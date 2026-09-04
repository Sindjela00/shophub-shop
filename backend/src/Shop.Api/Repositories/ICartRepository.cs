using Shop.Api.Models;

namespace Shop.Api.Repositories;

public interface ICartRepository
{
    Task<Cart> CreateAsync(CancellationToken ct = default);

    /// <summary>Null if not found. Items populated.</summary>
    Task<Cart?> GetAsync(Guid id, CancellationToken ct = default);

    /// <summary>Adds <paramref name="quantity"/> to the existing line for this article, or
    /// creates a new one — mirrors CartsController.AddItem's "add to whatever's already there"
    /// semantics (as opposed to SetItemQuantityAsync's absolute set). Caller has already
    /// validated stock/article existence against the *resulting* total quantity.</summary>
    Task AddItemAsync(Guid cartId, Guid articleId, int quantity, CancellationToken ct = default);

    /// <summary>Absolute set, not additive. quantity &lt;= 0 removes the line entirely —
    /// mirrors CartsController.SetItemQuantity exactly.</summary>
    Task SetItemQuantityAsync(Guid cartId, Guid articleId, int quantity, CancellationToken ct = default);

    Task RemoveItemAsync(Guid cartId, Guid articleId, CancellationToken ct = default);

    /// <summary>Used after a successful checkout turns the cart into an order.</summary>
    Task DeleteAsync(Guid cartId, CancellationToken ct = default);
}
