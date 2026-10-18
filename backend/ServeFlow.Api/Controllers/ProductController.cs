
using Microsoft.AspNetCore.Authorization;
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

    // GET: api/products
    // Public: anyone can view products.
    [HttpGet]
    public async Task<IActionResult> GetProducts(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        page = Math.Max(page, 1);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _dbContext.Products
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = search.Trim().ToLower();

            query = query.Where(product =>
                product.Name.ToLower().Contains(searchTerm) ||
                product.Description.ToLower().Contains(searchTerm));
        }

        var totalCount = await query.CountAsync();

        var products = await query
            .Include(product => product.Category)
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

    // GET: api/products/{id}
    // Public: anyone can view a product.
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Product>> GetProduct(Guid id)
    {
        var product = await _dbContext.Products
            .AsNoTracking()
            .Include(item => item.Category)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (product is null)
        {
            return NotFound();
        }

        return product;
    }

    // POST: api/products
    // Staff and Admin can create products.
    [Authorize(Roles = "Staff,Admin")]
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

        var name = request.Name.Trim();

        if (name.Length > 150)
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

        Category? category = null;

        if (request.CategoryId.HasValue)
        {
            category = await _dbContext.Categories
                .FirstOrDefaultAsync(
                    item => item.Id == request.CategoryId.Value);

            if (category is null)
            {
                return BadRequest(new
                {
                    message = "The selected category does not exist."
                });
            }
        }

        var product = new Product
        {
            Name = name,
            Description = request.Description?.Trim() ?? string.Empty,
            Price = request.Price,
            CategoryId = category?.Id,
            Category = category
        };

        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetProduct),
            new { id = product.Id },
            product);
    }

    // PUT: api/products/{id}
    // Staff and Admin can update products.
    [Authorize(Roles = "Staff,Admin")]
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<Product>> UpdateProduct(
        Guid id,
        UpdateProductRequest request)
    {
        var product = await _dbContext.Products
            .Include(item => item.Category)
            .FirstOrDefaultAsync(item => item.Id == id);

        if (product is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new
            {
                message = "Product name is required."
            });
        }

        var name = request.Name.Trim();

        if (name.Length > 150)
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

        Category? category = null;

        if (request.CategoryId.HasValue)
        {
            category = await _dbContext.Categories
                .FirstOrDefaultAsync(
                    item => item.Id == request.CategoryId.Value);

            if (category is null)
            {
                return BadRequest(new
                {
                    message = "The selected category does not exist."
                });
            }
        }

        product.Name = name;
        product.Description = request.Description?.Trim() ?? string.Empty;
        product.Price = request.Price;
        product.IsAvailable = request.IsAvailable;
        product.CategoryId = category?.Id;
        product.Category = category;

        await _dbContext.SaveChangesAsync();

        return Ok(product);
    }

    // DELETE: api/products/{id}
    // Only Admin can delete products.
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteProduct(Guid id)
    {
        var product = await _dbContext.Products
            .FirstOrDefaultAsync(item => item.Id == id);

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
