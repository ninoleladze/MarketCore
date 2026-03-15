using MarketCore.Application.Interfaces;
using MarketCore.Domain.Repositories;
using MarketCore.Infrastructure.Auth;
using MarketCore.Infrastructure.Caching;
using MarketCore.Infrastructure.Interceptors;
using MarketCore.Infrastructure.Persistence;
using MarketCore.Infrastructure.Repositories;
using MarketCore.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace MarketCore.Infrastructure.Extensions;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {

        services.AddHttpContextAccessor();

        services.AddSingleton<AuditInterceptor>();
        services.AddSingleton<DomainEventInterceptor>();

        services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
        {
            var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
            var connectionString = databaseUrl ?? configuration.GetConnectionString("DefaultConnection");

            if (databaseUrl is not null)
            {
                options.UseMySql(
                    connectionString,
                    new MySqlServerVersion(new Version(8, 0, 36)),
                    o => o.CommandTimeout(30));
            }
            else
            {
                options.UseSqlServer(
                    connectionString,
                    o => o.CommandTimeout(30));
            }

            var auditInterceptor = serviceProvider.GetRequiredService<AuditInterceptor>();
            var domainEventInterceptor = serviceProvider.GetRequiredService<DomainEventInterceptor>();
            options.AddInterceptors(auditInterceptor, domainEventInterceptor);
        });

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        var redisConnectionString = configuration.GetConnectionString("Redis")
            ?? configuration["Redis:ConnectionString"]
            ?? "localhost:6379";

        var redisAvailable = TryConnectRedis(redisConnectionString, services);

        if (redisAvailable)
        {

            services.AddScoped<ICacheService, RedisCacheService>();
        }
        else
        {

            services.AddMemoryCache();
            services.AddScoped<ICacheService, InMemoryCacheService>();
        }

        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection("Jwt"))
            .Validate(s => !string.IsNullOrWhiteSpace(s.Key) && s.Key.Length >= 32,
                "Jwt:Key must be at least 32 characters.")
            .Validate(s => !string.IsNullOrWhiteSpace(s.Issuer),
                "Jwt:Issuer is required.")
            .Validate(s => !string.IsNullOrWhiteSpace(s.Audience),
                "Jwt:Audience is required.")
            .ValidateOnStart();

        services.AddScoped<ITokenProvider, JwtTokenProvider>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        var smtpUsername = configuration["Email:Smtp:Username"];
        if (!string.IsNullOrWhiteSpace(smtpUsername))
        {
            services.AddOptions<SmtpSettings>()
                .Bind(configuration.GetSection("Email:Smtp"))
                .Validate(s => !string.IsNullOrWhiteSpace(s.Username), "Email:Smtp:Username is required.")
                .Validate(s => !string.IsNullOrWhiteSpace(s.Password), "Email:Smtp:Password is required.")
                .ValidateOnStart();

            services.AddScoped<IEmailService, GmailEmailService>();
        }
        else
        {
            services.AddScoped<IEmailService, EmailService>();
        }

        services.AddScoped<IOrderHubService, OrderHubService>();

        return services;
    }

    private static bool TryConnectRedis(string connectionString, IServiceCollection services)
    {
        try
        {
            var configurationOptions = ConfigurationOptions.Parse(connectionString);
            configurationOptions.ConnectTimeout = 2000;
            configurationOptions.AbortOnConnectFail = false;

            var multiplexer = ConnectionMultiplexer.Connect(configurationOptions);

            if (!multiplexer.IsConnected)
            {

                multiplexer.Dispose();
                return false;
            }

            services.AddSingleton<IConnectionMultiplexer>(multiplexer);
            return true;
        }
        catch (Exception)
        {

            return false;
        }
    }
}
