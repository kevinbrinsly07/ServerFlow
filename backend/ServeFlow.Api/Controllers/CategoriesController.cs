
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServeFlow.Api.Contracts.Categories;
using ServeFlow.Domain.Entities;
using ServeFlow.Infrastructure.Data;

namespace ServeFlow.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
     private readonly ServeFlowDbContext _dbContext;

     public CategoriesController(ServeFlowDbContext dbContext)
     {
          _dbContext = dbContext;
     }

     // GET: api/categories
     [HttpGet]
     public async Task<IActionResult> GetCategories()
     {
          var categories = await _dbContext.Categories
              .AsNoTracking()
              .OrderBy(category => category.Name)
              .Select(category => new
              {
                   category.Id,
                   category.Name,
                   category.Description,
                   category.CreatedAtUtc,
                   ProductCount = category.Products.Count()
              })
              .ToListAsync();

          return Ok(categories);
     }

     // GET: api/categories/{id}
     [HttpGet("{id:guid}")]
     public async Task<IActionResult> GetCategory(Guid id)
     {
          var category = await _dbContext.Categories
              .AsNoTracking()
              .Where(category => category.Id == id)
              .Select(category => new
              {
                   category.Id,
                   category.Name,
                   category.Description,
                   category.CreatedAtUtc,
                   ProductCount = category.Products.Count()
              })
              .FirstOrDefaultAsync();

          if (category is null)
          {
               return NotFound(new { message = "Category not found." });
          }

          return Ok(category);
     }

     // POST: api/categories
     [HttpPost]
     public async Task<IActionResult> CreateCategory(
         [FromBody] CreateCategoryRequest request)
     {
          var name = request.Name.Trim();

          if (string.IsNullOrWhiteSpace(name))
          {
               return BadRequest(new
               {
                    message = "Category name is required."
               });
          }

          var normalizedName = name.ToLower();

          var alreadyExists = await _dbContext.Categories
              .AnyAsync(category =>
                  category.Name.ToLower() == normalizedName);

          if (alreadyExists)
          {
               return Conflict(new
               {
                    message = "A category with this name already exists."
               });
          }

          var category = new Category
          {
               Name = name,
               Description = request.Description?.Trim()
          };

          _dbContext.Categories.Add(category);
          await _dbContext.SaveChangesAsync();

          return CreatedAtAction(
              nameof(GetCategory),
              new { id = category.Id },
              new
              {
                   category.Id,
                   category.Name,
                   category.Description,
                   category.CreatedAtUtc,
                   ProductCount = 0
              });
     }

     // PUT: api/categories/{id}
     [HttpPut("{id:guid}")]
     public async Task<IActionResult> UpdateCategory(
         Guid id,
         [FromBody] UpdateCategoryRequest request)
     {
          var category = await _dbContext.Categories
              .FirstOrDefaultAsync(category => category.Id == id);

          if (category is null)
          {
               return NotFound(new { message = "Category not found." });
          }

          var name = request.Name.Trim();

          if (string.IsNullOrWhiteSpace(name))
          {
               return BadRequest(new
               {
                    message = "Category name is required."
               });
          }

          var normalizedName = name.ToLower();

          var alreadyExists = await _dbContext.Categories
              .AnyAsync(existing =>
                  existing.Id != id &&
                  existing.Name.ToLower() == normalizedName);

          if (alreadyExists)
          {
               return Conflict(new
               {
                    message = "A category with this name already exists."
               });
          }

          category.Name = name;
          category.Description = request.Description?.Trim();

          await _dbContext.SaveChangesAsync();

          return Ok(new
          {
               category.Id,
               category.Name,
               category.Description,
               category.CreatedAtUtc,
               ProductCount = await _dbContext.Products
                  .CountAsync(product => product.CategoryId == id)
          });
     }

     // DELETE: api/categories/{id}
     [HttpDelete("{id:guid}")]
     public async Task<IActionResult> DeleteCategory(Guid id)
     {
          var category = await _dbContext.Categories
              .FirstOrDefaultAsync(category => category.Id == id);

          if (category is null)
          {
               return NotFound(new
               {
                    message = "Category not found."
               });
          }

          // Remove the category association from its products first.
          var products = await _dbContext.Products
              .Where(product => product.CategoryId == id)
              .ToListAsync();

          foreach (var product in products)
          {
               product.CategoryId = null;
               product.Category = null;
          }

          _dbContext.Categories.Remove(category);

          await _dbContext.SaveChangesAsync();

          return NoContent();
     }
}
