
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ServeFlow.Domain.Entities;
using ServeFlow.Infrastructure.Identity;

namespace ServeFlow.Infrastructure.Data;

public class ServeFlowDbContext
    : IdentityDbContext<ApplicationUser>
{
    public ServeFlowDbContext(
        DbContextOptions<ServeFlowDbContext> options)
        : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
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

            entity.HasOne(product => product.Category)
                .WithMany(category => category.Products)
                .HasForeignKey(product => product.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(category => category.Id);

            entity.Property(category => category.Name)
                .HasMaxLength(100)
                .IsRequired();

            entity.HasIndex(category => category.Name)
                .IsUnique();

            entity.Property(category => category.Description)
                .HasMaxLength(500);

            entity.Property(category => category.CreatedAtUtc)
                .IsRequired();
        });

        modelBuilder.Entity<ApplicationUser>(entity =>
        {
            entity.Property(user => user.FullName)
                .HasMaxLength(150)
                .IsRequired();
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(log => log.Id);

            entity.Property(log => log.UserId)
                .HasMaxLength(450);

            entity.Property(log => log.ActorEmail)
                .HasMaxLength(256)
                .IsRequired();

            entity.Property(log => log.Action)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(log => log.EntityType)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(log => log.EntityId)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(log => log.Description)
                .HasMaxLength(1000)
                .IsRequired();

            entity.Property(log => log.CreatedAtUtc)
                .IsRequired();

            entity.HasIndex(log => log.CreatedAtUtc);
            entity.HasIndex(log => log.EntityType);
            entity.HasIndex(log => log.Action);
        });
    }
}
