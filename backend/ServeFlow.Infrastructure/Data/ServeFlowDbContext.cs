
using Microsoft.EntityFrameworkCore;
using ServeFlow.Domain.Entities;

namespace ServeFlow.Infrastructure.Data;

public class ServeFlowDbContext : DbContext
{
    public ServeFlowDbContext(
        DbContextOptions<ServeFlowDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(product => product.Id);

            entity.Property(product => product.Name)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(product => product.Description)
                .HasMaxLength(1000);

            entity.Property(product => product.Price)
                .HasPrecision(12, 2);

            entity.Property(product => product.CreatedAtUtc)
                .IsRequired();
        });
    }
}
