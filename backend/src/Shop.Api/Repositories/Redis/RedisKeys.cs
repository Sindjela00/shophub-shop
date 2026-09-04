namespace Shop.Api.Repositories.Redis;

/// <summary>
/// Central key scheme for the "light" tier's Redis-backed storage. Each aggregate (Article,
/// Category, Cart, Order) is stored as one JSON document per instance — a natural fit for
/// Redis, unlike the relational EF Core model these mirror — plus small index structures
/// (sets/hashes/sorted sets) for the handful of lookups the app actually needs (listing,
/// uniqueness checks, "newest first" ordering) without requiring a full SCAN for each of them.
/// </summary>
internal static class RedisKeys
{
    public static string Article(Guid id) => $"article:{id}";
    public const string ArticlesAll = "articles:all"; // Set<articleId> — every article, for listing

    public static string Category(Guid id) => $"category:{id}";
    public const string CategoriesAll = "categories:all"; // Set<categoryId>
    public const string CategoryNameIndex = "categories:name-index"; // Hash<name, categoryId> — uniqueness + name->id lookup

    public static string Cart(Guid id) => $"cart:{id}";

    public static string Order(Guid id) => $"order:{id}";
    public const string OrdersByCreated = "orders:by-created"; // SortedSet<orderId> scored by CreatedAt ticks, for newest-first listing
    public const string OrdersByTxHash = "orders:by-txhash"; // Hash<txHash, orderId>
}
