
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServeFlow.Api.Contracts.Products;
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
public async Task<IActionResult> GetProducts(
    [FromQuery] string? search,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10)
{
    page = Math.Max(page, 1);
    pageSize = Math.Clamp(pageSize, 1, 100);

    var query = _dbContext.Products.AsNoTracking();

    if (!string.IsNullOrWhiteSpace(search))
    {
        var searchTerm = search.Trim().ToLower();

        query = query.Where(product =>
            product.Name.ToLower().Contains(searchTerm) ||
            product.Description.ToLower().Contains(searchTerm));
    }

    var totalCount = await query.CountAsync();

    var products = await query
        .OrderBy(product => product.Name)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    var totalPages = (int)Math.Ceiling(
        totalCount / (double)pageSize);

    return Ok(new
    {
        items = products,
        page,
        pageSize,
        totalCount,
        totalPages
    });
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
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new
            {
                message = "Product name is required."
            });
        }

        if (request.Name.Trim().Length > 150)
        {
            return BadRequest(new
            {
                message = "Product name cannot exceed 150 characters."
            });
        }

        if (request.Price <= 0)
        {
            return BadRequest(new
            {
                message = "Price must be greater than zero."
            });
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
            return NotFound(new
            {
                message = "Product not found."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new
            {
                message = "Product name is required."
            });
        }

        if (request.Name.Trim().Length > 150)
        {
            return BadRequest(new
            {
                message = "Product name cannot exceed 150 characters."
            });
        }

        if (request.Price <= 0)
        {
            return BadRequest(new
            {
                message = "Price must be greater than zero."
            });
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
            return NotFound(new
            {
                message = "Product not found."
            });
        }

        _dbContext.Products.Remove(product);
        await _dbContext.SaveChangesAsync();

        return NoContent();
    }
}
