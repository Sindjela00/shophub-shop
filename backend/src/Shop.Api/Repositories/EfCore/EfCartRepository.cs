using Microsoft.EntityFrameworkCore;
using Shop.Api.Data;
using Shop.Api.Models;

namespace Shop.Api.Repositories.EfCore;

public class EfCartRepository(ShopDbContext db) : ICartRepository
{
    public async Task<Cart> CreateAsync(CancellationToken ct = default)
    {
        var cart = new Cart { Id = Guid.NewGuid() };
        db.Carts.Add(cart);
        await db.SaveChangesAsync(ct);
        return cart;
    }

    public Task<Cart?> GetAsync(Guid id, CancellationToken ct = default) =>
        db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task AddItemAsync(Guid cartId, Guid articleId, int quantity, CancellationToken ct = default)
    {
        var cart = await db.Carts.Include(c => c.Items).FirstAsync(c => c.Id == cartId, ct);
        var existing = cart.Items.FirstOrDefault(i => i.ArticleId == articleId);
        if (existing is not null)
        {
            existing.Quantity += quantity;
        }
        else
        {
            // Adding to an already-tracked Cart's collection navigation isn't enough on its
            // own: EF sees the pre-assigned Guid key and infers Modified instead of Added.
            // Adding it to the DbSet directly forces Added state; EF's relationship fixup then
            // wires it into cart.Items automatically (adding it here too would double it).
            db.CartItems.Add(new CartItem { Id = Guid.NewGuid(), CartId = cart.Id, ArticleId = articleId, Quantity = quantity });
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task SetItemQuantityAsync(Guid cartId, Guid articleId, int quantity, CancellationToken ct = default)
    {
        var cart = await db.Carts.Include(c => c.Items).FirstAsync(c => c.Id == cartId, ct);
        var existing = cart.Items.FirstOrDefault(i => i.ArticleId == articleId);
        if (existing is null)
        {
            return;
        }
        if (quantity <= 0)
        {
            db.CartItems.Remove(existing);
        }
        else
        {
            existing.Quantity = quantity;
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task RemoveItemAsync(Guid cartId, Guid articleId, CancellationToken ct = default)
    {
        var cart = await db.Carts.Include(c => c.Items).FirstAsync(c => c.Id == cartId, ct);
        var existing = cart.Items.FirstOrDefault(i => i.ArticleId == articleId);
        if (existing is null)
        {
            return;
        }
        db.CartItems.Remove(existing);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid cartId, CancellationToken ct = default)
    {
        var cart = await db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.Id == cartId, ct);
        if (cart is null)
        {
            return;
        }
        db.CartItems.RemoveRange(cart.Items);
        db.Carts.Remove(cart);
        await db.SaveChangesAsync(ct);
    }
}
