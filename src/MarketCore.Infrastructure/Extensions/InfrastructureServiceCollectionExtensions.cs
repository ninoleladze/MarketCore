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
            var mysqlUrl = Environment.GetEnvironmentVariable("DATABASE_URL")
                        ?? Environment.GetEnvironmentVariable("MYSQL_URL")
                        ?? Environment.GetEnvironmentVariable("MYSQL_PRIVATE_URL");

            var mysqlHost = Environment.GetEnvironmentVariable("MYSQLHOST");

            if (mysqlUrl is not null)
            {
                var connectionString = ParseMySqlUrl(mysqlUrl);
                options.UseMySql(
                    connectionString,
                    new MySqlServerVersion(new Version(8, 0, 36)),
                    o => o.CommandTimeout(30));
            }
            else if (mysqlHost is not null)
            {
                var port = Environment.GetEnvironmentVariable("MYSQLPORT") ?? "3306";
                var db = Environment.GetEnvironmentVariable("MYSQLDATABASE") ?? "railway";
                var user = Environment.GetEnvironmentVariable("MYSQLUSER") ?? "root";
                var pass = Environment.GetEnvironmentVariable("MYSQLPASSWORD") ?? string.Empty;
                var connectionString = $"Server={mysqlHost};Port={port};Database={db};User={user};Password={pass};";
                options.UseMySql(
                    connectionString,
                    new MySqlServerVersion(new Version(8, 0, 36)),
                    o => o.CommandTimeout(30));
            }
            else
            {
                var connectionString = configuration.GetConnectionString("DefaultConnection");
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

    private static string ParseMySqlUrl(string url)
    {
        var uri = new Uri(url);
        var userInfo = uri.UserInfo.Split(':');
        var user = userInfo[0];
        var password = userInfo.Length > 1 ? userInfo[1] : string.Empty;
        var host = uri.Host;
        var port = uri.Port > 0 ? uri.Port : 3306;
        var database = uri.AbsolutePath.TrimStart('/');
        return $"Server={host};Port={port};Database={database};User={user};Password={password};";
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
