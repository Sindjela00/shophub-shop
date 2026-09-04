using System.Text.Json;
using Shop.Api.Models;
using StackExchange.Redis;

namespace Shop.Api.Repositories.Redis;

public class RedisCartRepository(IConnectionMultiplexer redis) : ICartRepository
{
    // A cart and its items are one JSON document — unlike Article/Category/Order, there's no
    // separate "all carts" index at all: nothing in the app ever lists carts, they're only
    // ever looked up by the id the client already has, so there's nothing that index would serve.
    private IDatabase Db => redis.GetDatabase();

    public async Task<Cart> CreateAsync(CancellationToken ct = default)
    {
        var cart = new Cart { Id = Guid.NewGuid() };
        await Db.StringSetAsync(RedisKeys.Cart(cart.Id), JsonSerializer.Serialize(cart));
        return cart;
    }

    public async Task<Cart?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var json = await Db.StringGetAsync(RedisKeys.Cart(id));
        return json.IsNullOrEmpty ? null : JsonSerializer.Deserialize<Cart>((string)json!);
    }

    public async Task AddItemAsync(Guid cartId, Guid articleId, int quantity, CancellationToken ct = default)
    {
        var cart = await GetAsync(cartId, ct) ?? throw new InvalidOperationException($"Cart {cartId} not found.");
        var existing = cart.Items.FirstOrDefault(i => i.ArticleId == articleId);
        if (existing is not null)
        {
            existing.Quantity += quantity;
        }
        else
        {
            cart.Items.Add(new CartItem { Id = Guid.NewGuid(), CartId = cartId, ArticleId = articleId, Quantity = quantity });
        }
        await Db.StringSetAsync(RedisKeys.Cart(cartId), JsonSerializer.Serialize(cart));
    }

    public async Task SetItemQuantityAsync(Guid cartId, Guid articleId, int quantity, CancellationToken ct = default)
    {
        var cart = await GetAsync(cartId, ct) ?? throw new InvalidOperationException($"Cart {cartId} not found.");
        var existing = cart.Items.FirstOrDefault(i => i.ArticleId == articleId);
        if (existing is null)
        {
            return;
        }
        if (quantity <= 0)
        {
            cart.Items.Remove(existing);
        }
        else
        {
            existing.Quantity = quantity;
        }
        await Db.StringSetAsync(RedisKeys.Cart(cartId), JsonSerializer.Serialize(cart));
    }

    public async Task RemoveItemAsync(Guid cartId, Guid articleId, CancellationToken ct = default)
    {
        var cart = await GetAsync(cartId, ct) ?? throw new InvalidOperationException($"Cart {cartId} not found.");
        cart.Items.RemoveAll(i => i.ArticleId == articleId);
        await Db.StringSetAsync(RedisKeys.Cart(cartId), JsonSerializer.Serialize(cart));
    }

    public Task DeleteAsync(Guid cartId, CancellationToken ct = default) =>
        Db.KeyDeleteAsync(RedisKeys.Cart(cartId));
}
