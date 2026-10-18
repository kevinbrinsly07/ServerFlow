using System.Text.Json.Serialization;

namespace ServeFlow.Domain.Entities;

public class Category
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    [JsonIgnore]
    public ICollection<Product> Products { get; set; } = new List<Product>();
}