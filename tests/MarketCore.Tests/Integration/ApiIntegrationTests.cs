using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace MarketCore.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class ApiIntegrationTests : IClassFixture<MarketCoreApiFactory>
{
    private readonly HttpClient _client;

    public ApiIntegrationTests(MarketCoreApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetProducts_ReturnsOk_FromRealDatabase()
    {
        var response = await _client.GetAsync("/api/v1/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetCategories_ReturnsOk_FromRealDatabase()
    {
        var response = await _client.GetAsync("/api/v1/categories");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateProduct_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/products", new { Name = "Test" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetCart_WithoutToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/cart");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_WithUnknownUser_IsRejected()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login",
            new { Email = "nobody@example.com", Password = "WrongPassword1!" });

        response.IsSuccessStatusCode.Should().BeFalse();
    }
}
