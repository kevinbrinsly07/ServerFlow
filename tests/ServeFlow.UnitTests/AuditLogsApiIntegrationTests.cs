using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace ServeFlow.UnitTests;

public class AuditLogsApiIntegrationTests : IAsyncLifetime
{
    private ProductsApiFactory _factory = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _factory = new ProductsApiFactory();
        _client = _factory.CreateClient();

        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task GetAuditLogs_AsAdmin_ReturnsPaginatedResults()
    {
        // Create a product. Successful creation should create an audit log.
        var createResponse = await _client.PostAsJsonAsync(
            "/api/products",
            new
            {
                name = "Audit Test Product",
                price = 1250m,
                description = "Product for audit integration test"
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var response = await _client.GetAsync("/api/auditlogs");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<JsonElement>();

        Assert.True(result.TryGetProperty("items", out var items));
        Assert.True(result.TryGetProperty("page", out var page));
        Assert.True(result.TryGetProperty("pageSize", out var pageSize));
        Assert.True(result.TryGetProperty("totalCount", out var totalCount));
        Assert.True(result.TryGetProperty("totalPages", out var totalPages));

        Assert.True(totalCount.GetInt32() >= 1);
        Assert.Equal(1, page.GetInt32());
        Assert.Equal(20, pageSize.GetInt32());
        Assert.True(totalPages.GetInt32() >= 1);
        Assert.Equal(JsonValueKind.Array, items.ValueKind);

        var matchingLog = items.EnumerateArray().FirstOrDefault(log =>
            log.GetProperty("entityType").GetString() == "Product" &&
            log.GetProperty("action").GetString() == "Created" &&
            log.GetProperty("description").GetString()!
                .Contains("Audit Test Product"));

        Assert.NotEqual(JsonValueKind.Undefined, matchingLog.ValueKind);
        Assert.Equal(
            "Integration Test User",
            matchingLog.GetProperty("actorEmail").GetString());
    }

    [Fact]
    public async Task GetAuditLogs_AsStaff_ReturnsForbidden()
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/auditlogs");

        request.Headers.Add("X-Test-Role", "Staff");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetAuditLogs_WithoutAuthentication_ReturnsUnauthorized()
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/auditlogs");

        request.Headers.Add("X-Test-Role", "none");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAuditLogs_FiltersByActionAndEntityType()
    {
        var productResponse = await _client.PostAsJsonAsync(
            "/api/products",
            new
            {
                name = "Filtered Audit Product",
                price = 750m,
                description = "Filtering test"
            });

        Assert.Equal(HttpStatusCode.Created, productResponse.StatusCode);

        var categoryResponse = await _client.PostAsJsonAsync(
            "/api/categories",
            new
            {
                name = "Audit Test Category",
                description = "Filtering test category"
            });

        Assert.Equal(HttpStatusCode.Created, categoryResponse.StatusCode);

        var response = await _client.GetAsync(
            "/api/auditlogs?action=Created&entityType=Product");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<JsonElement>();

        var items = result.GetProperty("items");

        Assert.True(items.GetArrayLength() >= 1);

        foreach (var item in items.EnumerateArray())
        {
            Assert.Equal(
                "Created",
                item.GetProperty("action").GetString());

            Assert.Equal(
                "Product",
                item.GetProperty("entityType").GetString());
        }
    }

    [Fact]
    public async Task GetAuditLogs_ClampsInvalidPaginationValues()
    {
        var response = await _client.GetAsync(
            "/api/auditlogs?page=0&pageSize=500");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<JsonElement>();

        Assert.Equal(1, result.GetProperty("page").GetInt32());
        Assert.Equal(100, result.GetProperty("pageSize").GetInt32());
    }
}
