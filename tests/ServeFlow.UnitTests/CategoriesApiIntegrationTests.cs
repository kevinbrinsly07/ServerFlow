
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace ServeFlow.UnitTests;

public class CategoriesApiIntegrationTests : IAsyncLifetime
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
    public async Task GetCategories_ReturnsEmptyListInitially()
    {
        var response = await _client.GetAsync("/api/categories");

        response.EnsureSuccessStatusCode();

        var categories = await response.Content
            .ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(JsonValueKind.Array, categories.ValueKind);
        Assert.Empty(categories.EnumerateArray());
    }

    [Fact]
    public async Task CreateCategory_ReturnsCreatedCategory()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/categories",
            new
            {
                name = "Electronics",
                description = "Electronic devices"
            });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var category = await response.Content
            .ReadFromJsonAsync<JsonElement>();

        Assert.Equal(
            "Electronics",
            category.GetProperty("name").GetString());

        Assert.Equal(
            "Electronic devices",
            category.GetProperty("description").GetString());

        Assert.Equal(
            0,
            category.GetProperty("productCount").GetInt32());
    }

    [Fact]
    public async Task GetCategory_ReturnsCreatedCategory()
    {
        var createResponse = await _client.PostAsJsonAsync(
            "/api/categories",
            new
            {
                name = "Clothing",
                description = "Clothing and accessories"
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content
            .ReadFromJsonAsync<JsonElement>();

        var id = created.GetProperty("id").GetGuid();

        var response = await _client.GetAsync(
            $"/api/categories/{id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var category = await response.Content
            .ReadFromJsonAsync<JsonElement>();

        Assert.Equal(id, category.GetProperty("id").GetGuid());
        Assert.Equal(
            "Clothing",
            category.GetProperty("name").GetString());
    }

    [Fact]
    public async Task UpdateCategory_ChangesCategoryDetails()
    {
        var createResponse = await _client.PostAsJsonAsync(
            "/api/categories",
            new
            {
                name = "Food",
                description = "Food products"
            });

        var created = await createResponse.Content
            .ReadFromJsonAsync<JsonElement>();

        var id = created.GetProperty("id").GetGuid();

        var updateResponse = await _client.PutAsJsonAsync(
            $"/api/categories/{id}",
            new
            {
                name = "Fresh Food",
                description = "Fresh food products"
            });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updated = await updateResponse.Content
            .ReadFromJsonAsync<JsonElement>();

        Assert.Equal(
            "Fresh Food",
            updated.GetProperty("name").GetString());

        Assert.Equal(
            "Fresh food products",
            updated.GetProperty("description").GetString());
    }

    [Fact]
    public async Task DeleteCategory_ReturnsNoContent()
    {
        var createResponse = await _client.PostAsJsonAsync(
            "/api/categories",
            new
            {
                name = "Books",
                description = "Printed books"
            });

        var created = await createResponse.Content
            .ReadFromJsonAsync<JsonElement>();

        var id = created.GetProperty("id").GetGuid();

        var deleteResponse = await _client.DeleteAsync(
            $"/api/categories/{id}");

        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync(
            $"/api/categories/{id}");

        Assert.Equal(
            HttpStatusCode.NotFound,
            getResponse.StatusCode);
    }

    [Fact]
    public async Task CreateCategory_ReturnsConflictForDuplicateName()
    {
        var firstResponse = await _client.PostAsJsonAsync(
            "/api/categories",
            new { name = "Electronics", description = "Devices" });

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);

        var duplicateResponse = await _client.PostAsJsonAsync(
            "/api/categories",
            new { name = "Electronics", description = "Other devices" });

        Assert.Equal(
            HttpStatusCode.Conflict,
            duplicateResponse.StatusCode);
    }

    [Fact]
    public async Task GetCategory_ReturnsNotFoundForMissingCategory()
    {
        var response = await _client.GetAsync(
            $"/api/categories/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteCategory_PreservesProducts()
    {
        var categoryResponse = await _client.PostAsJsonAsync(
            "/api/categories",
            new { name = "Gadgets", description = "Small gadgets" });

        var category = await categoryResponse.Content
            .ReadFromJsonAsync<JsonElement>();

        var categoryId = category.GetProperty("id").GetGuid();

        var productResponse = await _client.PostAsJsonAsync(
            "/api/products",
            new
            {
                name = "Wireless Mouse",
                price = 2500m,
                description = "Wireless computer mouse",
                categoryId
            });

        Assert.Equal(HttpStatusCode.Created, productResponse.StatusCode);

        var product = await productResponse.Content
            .ReadFromJsonAsync<JsonElement>();

        var productId = product.GetProperty("id").GetGuid();

        var deleteResponse = await _client.DeleteAsync(
            $"/api/categories/{categoryId}");

        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteResponse.StatusCode);

        var getProductResponse = await _client.GetAsync(
            $"/api/products/{productId}");

        Assert.Equal(HttpStatusCode.OK, getProductResponse.StatusCode);

        var savedProduct = await getProductResponse.Content
            .ReadFromJsonAsync<JsonElement>();

        Assert.Equal(
            JsonValueKind.Null,
            savedProduct.GetProperty("categoryId").ValueKind);
    }


    [Fact]
    public async Task CreateCategory_AsStaff_ReturnsForbidden()
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/categories")
        {
            Content = JsonContent.Create(new
            {
                name = "Staff Restricted Category",
                description = "Should not be created by Staff"
            })
        };

        request.Headers.Add("X-Test-Role", "Staff");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

}
