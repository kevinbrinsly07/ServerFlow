
using System.ComponentModel.DataAnnotations;

namespace ServeFlow.Api.Contracts.Categories;

public record CreateCategoryRequest(
    [Required]
    [StringLength(100, MinimumLength = 1)]
    string Name,

    [StringLength(500)]
    string? Description
);
