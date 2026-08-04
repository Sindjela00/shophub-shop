using Microsoft.EntityFrameworkCore;
using Shop.Api.Models;

namespace Shop.Api.Data;

public class ShopDbContext(DbContextOptions<ShopDbContext> options) : DbContext(options)
{
    public DbSet<Article> Articles => Set<Article>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Article>(entity =>
        {
            entity.Property(a => a.Name).HasMaxLength(200);
            entity.Property(a => a.Category).HasMaxLength(100);
            entity.Property(a => a.Price).HasPrecision(18, 2);
            entity.HasIndex(a => a.Category);
        });
    }
}
