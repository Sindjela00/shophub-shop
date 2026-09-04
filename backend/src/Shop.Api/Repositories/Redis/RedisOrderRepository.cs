using System.Text.Json;
using StackExchange.Redis;
using ShopOrder = Shop.Api.Models.Order;

namespace Shop.Api.Repositories.Redis;

// Aliased to ShopOrder throughout: StackExchange.Redis has its own Order enum
// (Ascending/Descending, used below for sorted-set range queries) that collides with
// Shop.Api.Models.Order otherwise.
public class RedisOrderRepository(IConnectionMultiplexer redis) : IOrderRepository
{
    private IDatabase Db => redis.GetDatabase();

    public async Task<bool> TxHashExistsAsync(string txHash, CancellationToken ct = default) =>
        await Db.HashExistsAsync(RedisKeys.OrdersByTxHash, txHash);

    public async Task<List<ShopOrder>> ListAsync(CancellationToken ct = default)
    {
        // Newest first, without a full scan+sort: orders:by-created is a sorted set scored by
        // CreatedAt ticks, so ZREVRANGE hands them back in the right order directly.
        var ids = await Db.SortedSetRangeByScoreAsync(RedisKeys.OrdersByCreated, order: Order.Descending);
        var orders = new List<ShopOrder>();
        foreach (var idValue in ids)
        {
            var json = await Db.StringGetAsync(RedisKeys.Order(Guid.Parse((string)idValue!)));
            if (!json.IsNullOrEmpty)
            {
                orders.Add(JsonSerializer.Deserialize<ShopOrder>((string)json!)!);
            }
        }
        return orders;
    }

    public async Task CreateAsync(ShopOrder order, IReadOnlyDictionary<Guid, int> stockDecrements, CancellationToken ct = default)
    {
        var tx = Db.CreateTransaction();
        _ = tx.StringSetAsync(RedisKeys.Order(order.Id), JsonSerializer.Serialize(order));
        _ = tx.SortedSetAddAsync(RedisKeys.OrdersByCreated, order.Id.ToString(), order.CreatedAt.UtcTicks);
        _ = tx.HashSetAsync(RedisKeys.OrdersByTxHash, order.TxHash, order.Id.ToString());

        // HashDecrementAsync (HINCRBY with a negative value) is atomic per field, so this
        // commits alongside the order write in the same MULTI/EXEC — the Redis equivalent of
        // EF Core's single SaveChangesAsync covering both the new order and the stock changes.
        foreach (var (articleId, quantity) in stockDecrements)
        {
            _ = tx.HashDecrementAsync(RedisKeys.Article(articleId), RedisArticleRepository.Field.Stock, quantity);
        }

        await tx.ExecuteAsync();
    }
}
