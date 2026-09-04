using Microsoft.EntityFrameworkCore;
using Shop.Api.Data;
using Shop.Api.Models;

namespace Shop.Api.Repositories.EfCore;

public class EfOrderRepository(ShopDbContext db) : IOrderRepository
{
    public Task<bool> TxHashExistsAsync(string txHash, CancellationToken ct = default) =>
        db.Orders.AnyAsync(o => o.TxHash == txHash, ct);

    public Task<List<Order>> ListAsync(CancellationToken ct = default) =>
        db.Orders.AsNoTracking().Include(o => o.Items).OrderByDescending(o => o.CreatedAt).ToListAsync(ct);

    public async Task CreateAsync(Order order, IReadOnlyDictionary<Guid, int> stockDecrements, CancellationToken ct = default)
    {
        foreach (var (articleId, quantity) in stockDecrements)
        {
            // FindAsync checks the DbContext's own change tracker first — the same Article
            // instances OrderService already loaded via EfArticleRepository.GetByIdsAsync
            // (same scoped ShopDbContext) are returned here without a second round-trip, so
            // decrementing Stock on them participates in the one SaveChangesAsync below
            // alongside the new order, exactly like the pre-repository-refactor code did.
            var article = await db.Articles.FindAsync([articleId], ct);
            if (article is not null)
            {
                article.Stock -= quantity;
            }
        }

        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);
    }
}
