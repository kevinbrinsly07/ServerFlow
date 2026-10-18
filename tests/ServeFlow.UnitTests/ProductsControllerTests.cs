using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServeFlow.Api.Controllers;
using ServeFlow.Api.Contracts.Products;
using ServeFlow.Domain.Entities;
using ServeFlow.Infrastructure.Data;
using ServeFlow.Infrastructure.Services;
using Xunit;

namespace ServeFlow.UnitTests;

public class ProductsControllerTests
{
    private static ServeFlowDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ServeFlowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ServeFlowDbContext(options);
    }

    private static ProductsController CreateController(
        ServeFlowDbContext dbContext)
    {
        var auditLogService = new AuditLogService(dbContext);

        return new ProductsController(dbContext, auditLogService);
    }

    [Fact]
    public async Task GetProducts_ReturnsEmptyList_WhenNoProductsExist()
    {
        using var dbContext = CreateDbContext();
        var controller = CreateController(dbContext);

        var result = await controller.GetProducts(
            search: null,
            page: 1,
            pageSize: 10);

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);

        var response = okResult.Value;
        var itemsProperty = response.GetType().GetProperty("items");

        Assert.NotNull(itemsProperty);

        var products = Assert.IsAssignableFrom<IEnumerable<Product>>(
            itemsProperty.GetValue(response));

        Assert.Empty(products);
    }

    [Fact]
    public async Task CreateProduct_SavesProduct_WhenRequestIsValid()
    {
        using var dbContext = CreateDbContext();
        var controller = CreateController(dbContext);

        var request = new CreateProductRequest(
            "Chicken Burger",
            1250m,
            "Grilled chicken burger");

        var result = await controller.CreateProduct(request);

        var response = Assert.IsType<CreatedAtActionResult>(result.Result);
        var product = Assert.IsType<Product>(response.Value);

        Assert.Equal("Chicken Burger", product.Name);
        Assert.Equal(1250m, product.Price);
        Assert.Equal("Grilled chicken burger", product.Description);
        Assert.Equal(1, await dbContext.Products.CountAsync());

        var auditLog = await dbContext.AuditLogs.SingleAsync();
        Assert.Equal("Created", auditLog.Action);
        Assert.Equal("Product", auditLog.EntityType);
        Assert.Equal(product.Id.ToString(), auditLog.EntityId);
    }

    [Fact]
    public async Task CreateProduct_ReturnsBadRequest_WhenNameIsEmpty()
    {
        using var dbContext = CreateDbContext();
        var controller = CreateController(dbContext);

        var request = new CreateProductRequest(
            " ",
            1250m,
            "Test description");

        var result = await controller.CreateProduct(request);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(0, await dbContext.Products.CountAsync());
        Assert.Empty(await dbContext.AuditLogs.ToListAsync());
    }

    [Fact]
    public async Task CreateProduct_ReturnsBadRequest_WhenPriceIsZero()
    {
        using var dbContext = CreateDbContext();
        var controller = CreateController(dbContext);

        var request = new CreateProductRequest(
            "Chicken Burger",
            0m,
            "Test description");

        var result = await controller.CreateProduct(request);

        Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal(0, await dbContext.Products.CountAsync());
        Assert.Empty(await dbContext.AuditLogs.ToListAsync());
    }

    [Fact]
    public async Task GetProduct_ReturnsNotFound_WhenProductDoesNotExist()
    {
        using var dbContext = CreateDbContext();
        var controller = CreateController(dbContext);

        var result = await controller.GetProduct(Guid.NewGuid());

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task UpdateProduct_ChangesProductDetails()
    {
        using var dbContext = CreateDbContext();

        var product = new Product
        {
            Name = "Old Burger",
            Description = "Old description",
            Price = 1000m,
            IsAvailable = true
        };

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();

        var controller = CreateController(dbContext);

        var request = new UpdateProductRequest(
            "New Burger",
            1500m,
            "Updated description",
            false);

        var result = await controller.UpdateProduct(product.Id, request);

        var response = Assert.IsType<OkObjectResult>(result.Result);
        var updatedProduct = Assert.IsType<Product>(response.Value);

        Assert.Equal("New Burger", updatedProduct.Name);
        Assert.Equal(1500m, updatedProduct.Price);
        Assert.Equal("Updated description", updatedProduct.Description);
        Assert.False(updatedProduct.IsAvailable);

        var auditLog = await dbContext.AuditLogs.SingleAsync();
        Assert.Equal("Updated", auditLog.Action);
        Assert.Equal("Product", auditLog.EntityType);
        Assert.Equal(product.Id.ToString(), auditLog.EntityId);
    }

    [Fact]
    public async Task DeleteProduct_RemovesProduct_WhenItExists()
    {
        using var dbContext = CreateDbContext();

        var product = new Product
        {
            Name = "Burger",
            Description = "Test product",
            Price = 1000m
        };

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync();

        var controller = CreateController(dbContext);

        var result = await controller.DeleteProduct(product.Id);

        Assert.IsType<NoContentResult>(result);
        Assert.Null(await dbContext.Products.FindAsync(product.Id));

        var auditLog = await dbContext.AuditLogs.SingleAsync();
        Assert.Equal("Deleted", auditLog.Action);
        Assert.Equal("Product", auditLog.EntityType);
        Assert.Equal(product.Id.ToString(), auditLog.EntityId);
    }

    [Fact]
    public async Task DeleteProduct_ReturnsNotFound_WhenProductDoesNotExist()
    {
        using var dbContext = CreateDbContext();
        var controller = CreateController(dbContext);

        var result = await controller.DeleteProduct(Guid.NewGuid());

        var response = Assert.IsType<NotFoundObjectResult>(result);
        Assert.NotNull(response.Value);

        Assert.Empty(await dbContext.AuditLogs.ToListAsync());
    }
}
