
using System.ComponentModel.DataAnnotations;

namespace ServeFlow.Api.Contracts.Products;

public record CreateProductRequest(
    [Required]
    [StringLength(150, MinimumLength = 1)]
    string Name,

    [Range(typeof(decimal), "0.01", "9999999999")]
    decimal Price,

    [StringLength(1000)]
    string? Description
);
