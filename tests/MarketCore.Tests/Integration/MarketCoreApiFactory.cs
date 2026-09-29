using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.MsSql;
using Xunit;

namespace MarketCore.Tests.Integration;

/// <summary>
/// Boots the real MarketCore API in memory against a throw-away SQL Server
/// container, so tests run through the full pipeline:
/// HTTP -> middleware -> controllers -> MediatR -> EF Core -> SQL Server.
/// </summary>
public sealed class MarketCoreApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly MsSqlContainer _db = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Real database from the container
        builder.UseSetting("ConnectionStrings:DefaultConnection", _db.GetConnectionString());

        // No Redis in tests -> the app falls back to its in-memory cache
        builder.UseSetting("ConnectionStrings:Redis", "localhost:6399,connectTimeout=500");
    }

    public Task InitializeAsync() => _db.StartAsync();

    public new async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await base.DisposeAsync();
    }
}
