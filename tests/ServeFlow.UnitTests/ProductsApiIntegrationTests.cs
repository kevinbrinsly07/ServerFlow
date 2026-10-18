using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ServeFlow.Domain.Entities;
using ServeFlow.Infrastructure.Data;
using Xunit;

namespace ServeFlow.UnitTests;


public class ProductsApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = Guid.NewGuid().ToString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, configuration) =>
        {
            configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Jwt:Key"] =
                        "ServeFlow_Test_Only_Signing_Key_2026_0123456789",

                    ["Jwt:Issuer"] = "ServeFlow.Tests",
                    ["Jwt:Audience"] = "ServeFlow.Tests.Client"
                });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<
                IDbContextOptionsConfiguration<ServeFlowDbContext>>();

            services.RemoveAll<DbContextOptions<ServeFlowDbContext>>();
            services.RemoveAll<ServeFlowDbContext>();

            services.AddDbContext<ServeFlowDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    }
}


public class ProductsApiIntegrationTests : IAsyncLifetime
{
    private ProductsApiFactory _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        _factory = new ProductsApiFactory();
        _client = _factory.CreateClient();

        await Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task GetProducts_ReturnsSuccess()
    {
        var response = await _client.GetAsync("/api/products");

        response.EnsureSuccessStatusCode();

        var result =
            await response.Content.ReadFromJsonAsync<JsonElement>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(result.TryGetProperty("items", out var items));
        Assert.True(result.TryGetProperty("page", out var page));
        Assert.True(result.TryGetProperty("pageSize", out var pageSize));
        Assert.True(result.TryGetProperty("totalCount", out var totalCount));
        Assert.True(result.TryGetProperty("totalPages", out var totalPages));

        Assert.Equal(1, page.GetInt32());
        Assert.Equal(10, pageSize.GetInt32());
        Assert.Equal(0, totalCount.GetInt32());
        Assert.Equal(0, totalPages.GetInt32());
        Assert.Equal(JsonValueKind.Array, items.ValueKind);
    }

    [Fact]
    public async Task CreateProduct_ReturnsCreatedProduct()
    {
        var request = new
        {
            name = "Integration Test Burger",
            price = 1250m,
            description = "Created through HTTP"
        };

        var response = await _client.PostAsJsonAsync(
            "/api/products", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var product = await response.Content.ReadFromJsonAsync<Product>();

        Assert.NotNull(product);
        Assert.NotEqual(Guid.Empty, product.Id);
        Assert.Equal("Integration Test Burger", product.Name);
        Assert.Equal(1250m, product.Price);
    }

    [Fact]
    public async Task GetProduct_ReturnsProductAfterCreation()
    {
        var request = new
        {
            name = "Read Test Product",
            price = 900m,
            description = "Test description"
        };

        var createResponse = await _client.PostAsJsonAsync(
            "/api/products", request);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content
            .ReadFromJsonAsync<Product>();

        Assert.NotNull(created);

        var response = await _client.GetAsync(
            $"/api/products/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var product = await response.Content.ReadFromJsonAsync<Product>();

        Assert.NotNull(product);
        Assert.Equal(created.Id, product.Id);
        Assert.Equal("Read Test Product", product.Name);
    }

    [Fact]
    public async Task UpdateProduct_ChangesProduct()
    {
        var createResponse = await _client.PostAsJsonAsync(
            "/api/products",
            new
            {
                name = "Original Product",
                price = 1000m,
                description = "Original description"
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content
            .ReadFromJsonAsync<Product>();

        Assert.NotNull(created);

        var updateResponse = await _client.PutAsJsonAsync(
            $"/api/products/{created.Id}",
            new
            {
                name = "Updated Product",
                price = 1500m,
                description = "Updated description",
                isAvailable = false
            });

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updated = await updateResponse.Content
            .ReadFromJsonAsync<Product>();

        Assert.NotNull(updated);
        Assert.Equal("Updated Product", updated.Name);
        Assert.Equal(1500m, updated.Price);
        Assert.False(updated.IsAvailable);
    }

    [Fact]
    public async Task DeleteProduct_RemovesProduct()
    {
        var createResponse = await _client.PostAsJsonAsync(
            "/api/products",
            new
            {
                name = "Product To Delete",
                price = 500m,
                description = "Temporary product"
            });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content
            .ReadFromJsonAsync<Product>();

        Assert.NotNull(created);

        var deleteResponse = await _client.DeleteAsync(
            $"/api/products/{created.Id}");

        Assert.Equal(
            HttpStatusCode.NoContent,
            deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync(
            $"/api/products/{created.Id}");

        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task CreateProduct_ReturnsBadRequestForInvalidPrice()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/products",
            new
            {
                name = "Invalid Product",
                price = 0m,
                description = "Invalid price test"
            });

        Assert.Equal(
            HttpStatusCode.BadRequest,
            response.StatusCode);
    }

    [Fact]
    public async Task GetProducts_FiltersBySearchTerm()
    {
        await _client.PostAsJsonAsync(
            "/api/products",
            new
            {
                name = "Coffee",
                price = 1200m,
                description = "Ground coffee beans"
            });

        await _client.PostAsJsonAsync(
            "/api/products",
            new
            {
                name = "Tea",
                price = 800m,
                description = "Black tea leaves"
            });

        var response = await _client.GetAsync(
            "/api/products?search=coffee");

        response.EnsureSuccessStatusCode();

        var result = await response.Content
            .ReadFromJsonAsync<JsonElement>();

        var items = result.GetProperty("items");

        Assert.Equal(1, result.GetProperty("totalCount").GetInt32());
        Assert.Single(items.EnumerateArray());

        Assert.Equal(
            "Coffee",
            items[0].GetProperty("name").GetString());
    }

    [Fact]
    public async Task GetProducts_ReturnsRequestedPage()
    {
        await _client.PostAsJsonAsync(
            "/api/products",
            new
            {
                name = "Coffee",
                price = 1200m,
                description = "Coffee beans"
            });

        await _client.PostAsJsonAsync(
            "/api/products",
            new
            {
                name = "Milk",
                price = 600m,
                description = "Fresh milk"
            });

        await _client.PostAsJsonAsync(
            "/api/products",
            new
            {
                name = "Tea",
                price = 800m,
                description = "Tea leaves"
            });

        var response = await _client.GetAsync(
            "/api/products?page=2&pageSize=2");

        response.EnsureSuccessStatusCode();

        var result = await response.Content
            .ReadFromJsonAsync<JsonElement>();

        var items = result.GetProperty("items");

        Assert.Equal(2, result.GetProperty("page").GetInt32());
        Assert.Equal(2, result.GetProperty("pageSize").GetInt32());
        Assert.Equal(3, result.GetProperty("totalCount").GetInt32());
        Assert.Equal(2, result.GetProperty("totalPages").GetInt32());
        Assert.Single(items.EnumerateArray());

        Assert.Equal(
            "Tea",
            items[0].GetProperty("name").GetString());
    }
}
