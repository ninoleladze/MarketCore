using Asp.Versioning;
using MarketCore.Api.HealthChecks;
using MarketCore.Infrastructure.Hubs;
using MarketCore.Api.Middleware;
using MarketCore.Application.Extensions;
using MarketCore.Infrastructure.Auth;
using MarketCore.Infrastructure.Extensions;
using MarketCore.Infrastructure.Persistence;
using MarketCore.Infrastructure.Persistence.Seeders;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;
using System.Text;
using System.Threading.RateLimiting;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("MachineName", Environment.MachineName)
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting MarketCore API");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, services, config) => config
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("MachineName", Environment.MachineName)
        .Enrich.WithProperty("Application", "MarketCore.Api"));

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    builder.Services.AddScoped<DataSeeder>();

    builder.Services.AddControllers()
        .AddJsonOptions(opts =>
        {
            opts.JsonSerializerOptions.PropertyNamingPolicy =
                System.Text.Json.JsonNamingPolicy.CamelCase;
        });

    builder.Services.AddApiVersioning(opts =>
    {
        opts.DefaultApiVersion = new ApiVersion(1, 0);
        opts.AssumeDefaultVersionWhenUnspecified = true;
        opts.ReportApiVersions = true;
        opts.ApiVersionReader = new UrlSegmentApiVersionReader();
    }).AddApiExplorer(opts =>
    {
        opts.GroupNameFormat = "'v'VVV";
        opts.SubstituteApiVersionInUrl = true;
    });

    var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
        ?? throw new InvalidOperationException("Jwt configuration section is missing.");

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(opts =>
        {
            opts.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = jwtSettings.Audience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSettings.Key)),
                ClockSkew = TimeSpan.FromSeconds(30)
            };

            opts.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];
                    var path = context.HttpContext.Request.Path;

                    if (!string.IsNullOrWhiteSpace(accessToken) &&
                        path.StartsWithSegments("/hubs"))
                    {
                        context.Token = accessToken;
                    }

                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddAuthorization(opts =>
    {
        opts.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
        opts.AddPolicy("AuthenticatedUser", policy => policy.RequireAuthenticatedUser());
    });

    builder.Services.AddCors(opts =>
    {
        opts.AddPolicy("AllowedOrigins", policy =>
        {
            policy.WithOrigins("http://localhost:4200", "http://localhost:3000")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });

    builder.Services.AddRateLimiter(opts =>
    {

        opts.AddFixedWindowLimiter("GlobalFixed", limiterOpts =>
        {
            limiterOpts.PermitLimit = 60;
            limiterOpts.Window = TimeSpan.FromMinutes(1);
            limiterOpts.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            limiterOpts.QueueLimit = 5;
        });

        opts.AddSlidingWindowLimiter("AuthSliding", limiterOpts =>
        {
            limiterOpts.PermitLimit = 10;
            limiterOpts.Window = TimeSpan.FromSeconds(5);
            limiterOpts.SegmentsPerWindow = 5;
            limiterOpts.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            limiterOpts.QueueLimit = 2;
        });

        opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        opts.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        {
            var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return RateLimitPartition.GetFixedWindowLimiter(ip, _ =>
                new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 60,
                    Window = TimeSpan.FromMinutes(1)
                });
        });
    });

    builder.Services.AddHealthChecks()
        .AddDbContextCheck<ApplicationDbContext>("database")
        .AddCheck<RedisHealthCheck>("redis");

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(opts =>
    {
        opts.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "MarketCore API",
            Version = "v1",
            Description = "E-commerce API built on Clean Architecture, DDD, and CQRS."
        });

        var securityScheme = new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Description = "Enter 'Bearer {token}'",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Reference = new OpenApiReference
            {
                Id = JwtBearerDefaults.AuthenticationScheme,
                Type = ReferenceType.SecurityScheme
            }
        };

        opts.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, securityScheme);
        opts.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            { securityScheme, Array.Empty<string>() }
        });
    });

    builder.Services.AddSignalR();

    var app = builder.Build();

    app.UseMiddleware<GlobalExceptionMiddleware>();
    app.UseMiddleware<RequestLoggingMiddleware>();

    app.UseHttpsRedirection();
    app.UseCors("AllowedOrigins");
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(opts =>
        {
            opts.SwaggerEndpoint("/swagger/v1/swagger.json", "MarketCore API v1");
            opts.RoutePrefix = "swagger";
        });

        using var seedScope = app.Services.CreateScope();
        var seeder = seedScope.ServiceProvider.GetRequiredService<DataSeeder>();
        await seeder.SeedAsync();
    }

    app.MapHealthChecks("/health");
    app.MapControllers();

    app.MapHub<OrderHub>("/hubs/orders");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "MarketCore API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
