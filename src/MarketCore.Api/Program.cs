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
using Microsoft.EntityFrameworkCore;
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
            policy.SetIsOriginAllowed(_ => true)
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

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var isMySql = Environment.GetEnvironmentVariable("DATABASE_URL")
                   ?? Environment.GetEnvironmentVariable("MYSQL_URL")
                   ?? Environment.GetEnvironmentVariable("MYSQL_PRIVATE_URL")
                   ?? Environment.GetEnvironmentVariable("MYSQLHOST");

        if (isMySql is not null)
        {
            try
            {
                await db.Database.MigrateAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "EF Core migration failed — applying manual column fixes.");
            }

            // Fallback: ensure PasswordReset columns exist regardless of migration outcome.
            // Each operation is isolated in its own try/catch so one failure cannot block
            // the rest. "Duplicate column/key" errors simply mean the object already exists.

            try
            {
                await db.Database.ExecuteSqlRawAsync(
                    "CREATE UNIQUE INDEX `IX_Users_EmailVerificationToken` ON `Users` (`EmailVerificationToken`)");
                Log.Information("[Fallback] Recreated IX_Users_EmailVerificationToken index.");
            }
            catch (Exception ex) { Log.Debug(ex, "[Fallback] EmailVerificationToken index already exists or skipped."); }

            try
            {
                await db.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE `Users` ADD COLUMN `PasswordResetToken` VARCHAR(128) NULL");
                Log.Information("[Fallback] Added PasswordResetToken column.");
            }
            catch (Exception ex) { Log.Debug(ex, "[Fallback] PasswordResetToken column already exists or skipped."); }

            try
            {
                await db.Database.ExecuteSqlRawAsync(
                    "ALTER TABLE `Users` ADD COLUMN `PasswordResetTokenExpiresAt` DATETIME(6) NULL");
                Log.Information("[Fallback] Added PasswordResetTokenExpiresAt column.");
            }
            catch (Exception ex) { Log.Debug(ex, "[Fallback] PasswordResetTokenExpiresAt column already exists or skipped."); }

            try
            {
                await db.Database.ExecuteSqlRawAsync(
                    "CREATE UNIQUE INDEX `IX_Users_PasswordResetToken` ON `Users` (`PasswordResetToken`)");
                Log.Information("[Fallback] Created IX_Users_PasswordResetToken index.");
            }
            catch (Exception ex) { Log.Debug(ex, "[Fallback] PasswordResetToken index already exists or skipped."); }

            try
            {
                await db.Database.ExecuteSqlRawAsync(
                    "INSERT IGNORE INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`) " +
                    "VALUES ('20260317203943_AddPasswordReset', '8.0.0')");
                Log.Information("[Fallback] Migration history entry ensured for AddPasswordReset.");
            }
            catch (Exception ex) { Log.Warning(ex, "[Fallback] Could not insert migration history entry."); }
        }

        try
        {
            if (isMySql is null)
                await db.Database.MigrateAsync();

            var seeder = scope.ServiceProvider.GetRequiredService<DataSeeder>();
            await seeder.SeedAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Database migration or seeding failed. App will continue but data may be incomplete.");
        }
    }

    // Manual CORS — runs before everything, adds headers to every response
    app.Use(async (context, next) =>
    {
        var origin = context.Request.Headers.Origin.FirstOrDefault();
        if (!string.IsNullOrEmpty(origin))
        {
            context.Response.Headers["Access-Control-Allow-Origin"]      = origin;
            context.Response.Headers["Access-Control-Allow-Credentials"] = "true";
            context.Response.Headers["Access-Control-Allow-Methods"]     = "GET, POST, PUT, DELETE, PATCH, OPTIONS";
            context.Response.Headers["Access-Control-Allow-Headers"]     = "Authorization, Content-Type, Accept, Origin, X-Requested-With";
            context.Response.Headers["Access-Control-Max-Age"]           = "86400";
        }

        if (context.Request.Method == "OPTIONS")
        {
            context.Response.StatusCode = 204;
            return;
        }

        await next();
    });

    app.UseCors("AllowedOrigins");

    app.UseMiddleware<GlobalExceptionMiddleware>();
    app.UseMiddleware<RequestLoggingMiddleware>();

    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();

    app.UseSwagger();
    app.UseSwaggerUI(opts =>
    {
        opts.SwaggerEndpoint("/swagger/v1/swagger.json", "MarketCore API v1");
        opts.RoutePrefix = "swagger";
    });

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
