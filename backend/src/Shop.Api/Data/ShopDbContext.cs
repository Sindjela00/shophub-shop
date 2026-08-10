using Microsoft.EntityFrameworkCore;
using Shop.Api.Models;

namespace Shop.Api.Data;

public class ShopDbContext(DbContextOptions<ShopDbContext> options) : DbContext(options)
{
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Article>(entity =>
        {
            entity.Property(a => a.Name).HasMaxLength(200);
            entity.Property(a => a.Category).HasMaxLength(100);
            entity.Property(a => a.Price).HasPrecision(18, 2);
            entity.HasIndex(a => a.Category);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.Property(o => o.WalletAddress).HasMaxLength(100);
            entity.Property(o => o.TxHash).HasMaxLength(150);
            entity.Property(o => o.Total).HasPrecision(18, 2);
            // A given on-chain payment can only ever back one order.
            entity.HasIndex(o => o.TxHash).IsUnique();
            entity.HasMany(o => o.Items)
                .WithOne()
                .HasForeignKey(i => i.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.Property(i => i.ArticleName).HasMaxLength(200);
            entity.Property(i => i.UnitPrice).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasMany(c => c.Items)
                .WithOne()
                .HasForeignKey(i => i.CartId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
