
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServeFlow.Domain.Entities;
using ServeFlow.Infrastructure.Data;

namespace ServeFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ServeFlowDbContext _dbContext;

    public ProductsController(ServeFlowDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
    {
        var products = await _dbContext.Products
            .AsNoTracking()
            .OrderBy(product => product.Name)
            .ToListAsync();

        return Ok(products);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Product>> GetProduct(Guid id)
    {
        var product = await _dbContext.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(product => product.Id == id);

        if (product is null)
        {
            return NotFound();
        }

        return Ok(product);
    }

    [HttpPost]
    public async Task<ActionResult<Product>> CreateProduct(
        CreateProductRequest request)
    {
        var validationError = ValidateProduct(
            request.Name, request.Price);

        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        var product = new Product
        {
            Name = request.Name.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            Price = request.Price
        };

        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetProduct),
            new { id = product.Id },
            product);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<Product>> UpdateProduct(
        Guid id,
        UpdateProductRequest request)
    {
        var product = await _dbContext.Products
            .FirstOrDefaultAsync(product => product.Id == id);

        if (product is null)
        {
            return NotFound(new { message = "Product not found." });
        }

        var validationError = ValidateProduct(
            request.Name, request.Price);

        if (validationError is not null)
        {
            return BadRequest(new { message = validationError });
        }

        product.Name = request.Name.Trim();
        product.Description = request.Description?.Trim() ?? string.Empty;
        product.Price = request.Price;
        product.IsAvailable = request.IsAvailable;

        await _dbContext.SaveChangesAsync();

        return Ok(product);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteProduct(Guid id)
    {
        var product = await _dbContext.Products
            .FirstOrDefaultAsync(product => product.Id == id);

        if (product is null)
        {
            return NotFound(new { message = "Product not found." });
        }

        _dbContext.Products.Remove(product);
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }

    private static string? ValidateProduct(string? name, decimal price)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "Product name is required.";
        }

        if (name.Trim().Length > 150)
        {
            return "Product name cannot exceed 150 characters.";
        }

        if (price <= 0)
        {
            return "Price must be greater than zero.";
        }

        return null;
    }
}

public record CreateProductRequest(
    string Name,
    decimal Price,
    string? Description
);

public record UpdateProductRequest(
    string Name,
    decimal Price,
    string? Description,
    bool IsAvailable
);
