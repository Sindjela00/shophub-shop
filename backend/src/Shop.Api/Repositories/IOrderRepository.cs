using Shop.Api.Models;

namespace Shop.Api.Repositories;

public interface IOrderRepository
{
    /// <summary>A given on-chain payment can only ever back one order — mirrors the unique
    /// index on TxHash in the EF Core model.</summary>
    Task<bool> TxHashExistsAsync(string txHash, CancellationToken ct = default);

    /// <summary>Newest first, each with its Items populated.</summary>
    Task<List<Order>> ListAsync(CancellationToken ct = default);

    /// <summary>Persists <paramref name="order"/> (already fully built, Items included) and
    /// applies <paramref name="stockDecrements"/> (articleId -&gt; quantity to subtract) as one
    /// atomic unit — mirrors EF Core's single SaveChangesAsync committing both the new order and
    /// the stock changes together, so a failure partway through can't leave stock decremented
    /// with no matching order (or vice versa).</summary>
    Task CreateAsync(Order order, IReadOnlyDictionary<Guid, int> stockDecrements, CancellationToken ct = default);
}
