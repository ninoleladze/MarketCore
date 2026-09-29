using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.MySql;
using Xunit;

namespace MarketCore.Tests.Integration;

/// <summary>
/// Boots the real MarketCore API in memory against a throw-away MySQL container
/// (the same database engine used in production on Railway), so tests run through
/// the full pipeline: HTTP -> middleware -> controllers -> MediatR -> EF Core -> MySQL.
/// </summary>
public sealed class MarketCoreApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private static readonly string[] MySqlEnvVars =
        ["MYSQLHOST", "MYSQLPORT", "MYSQLDATABASE", "MYSQLUSER", "MYSQLPASSWORD"];

    private readonly MySqlContainer _db = new MySqlBuilder()
        .WithImage("mysql:8.0")
        .WithDatabase("marketcore")
        .WithUsername("marketcore")
        .WithPassword("marketcore_test_pw")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // No Redis in tests -> the app falls back to its in-memory cache
        builder.UseSetting("ConnectionStrings:Redis", "localhost:6399,connectTimeout=500");
    }

    public async Task InitializeAsync()
    {
        await _db.StartAsync();

        // The app switches to MySQL when these variables are present (see AddInfrastructure)
        Environment.SetEnvironmentVariable("MYSQLHOST", _db.Hostname);
        Environment.SetEnvironmentVariable("MYSQLPORT", _db.GetMappedPublicPort(3306).ToString());
        Environment.SetEnvironmentVariable("MYSQLDATABASE", "marketcore");
        Environment.SetEnvironmentVariable("MYSQLUSER", "marketcore");
        Environment.SetEnvironmentVariable("MYSQLPASSWORD", "marketcore_test_pw");
    }

    public new async Task DisposeAsync()
    {
        foreach (var name in MySqlEnvVars)
            Environment.SetEnvironmentVariable(name, null);

        await _db.DisposeAsync();
        await base.DisposeAsync();
    }
}
