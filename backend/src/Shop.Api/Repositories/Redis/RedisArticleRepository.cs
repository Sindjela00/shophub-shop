using Shop.Api.Models;
using StackExchange.Redis;

namespace Shop.Api.Repositories.Redis;

/// <summary>
/// Stored as a Hash (Name/Description/Price/CategoryId/Stock as separate fields), not a single
/// JSON blob — RedisOrderRepository needs to decrement Stock atomically (HINCRBY) as part of the
/// same transaction that persists a new order, which isn't possible against an opaque JSON
/// string. Category isn't stored here at all: CategoryId is, and the current Category is always
/// resolved fresh from its own key at read time, the same live-data behavior EF's
/// Include(a => a.Category) has.
/// </summary>
public class RedisArticleRepository(IConnectionMultiplexer redis) : IArticleRepository
{
    private IDatabase Db => redis.GetDatabase();

    internal static class Field
    {
        public const string Name = "Name";
        public const string Description = "Description";
        public const string Price = "Price";
        public const string CategoryId = "CategoryId";
        public const string Stock = "Stock";
    }

    internal static HashEntry[] ToHashEntries(Article a) =>
    [
        new(Field.Name, a.Name),
        new(Field.Description, a.Description),
        new(Field.Price, a.Price.ToString(System.Globalization.CultureInfo.InvariantCulture)),
        new(Field.CategoryId, a.CategoryId.ToString()),
        new(Field.Stock, a.Stock),
    ];

    private static Article? FromHashEntries(Guid id, HashEntry[] entries, Category? category)
    {
        if (entries.Length == 0)
        {
            return null;
        }
        var dict = entries.ToDictionary(e => e.Name.ToString(), e => e.Value.ToString());
        return new Article
        {
            Id = id,
            Name = dict[Field.Name],
            Description = dict[Field.Description],
            Price = decimal.Parse(dict[Field.Price], System.Globalization.CultureInfo.InvariantCulture),
            CategoryId = Guid.Parse(dict[Field.CategoryId]),
            Category = category,
            Stock = int.Parse(dict[Field.Stock]),
        };
    }

    public async Task<List<Article>> ListAsync(string? search, string? categoryName, CancellationToken ct = default)
    {
        var ids = await Db.SetMembersAsync(RedisKeys.ArticlesAll);
        var articles = new List<Article>();

        // No secondary index for search/category-name (the catalog sizes this tier targets
        // don't warrant one) — fetch every article and filter/sort in-process, same result
        // shape ArticlesController expects regardless of which repository produced it.
        foreach (var idValue in ids)
        {
            var id = Guid.Parse((string)idValue!);
            var article = FromHashEntries(id, await Db.HashGetAllAsync(RedisKeys.Article(id)), null);
            if (article is null)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(search) &&
                !article.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var category = await LoadCategoryAsync(article.CategoryId);

            if (!string.IsNullOrWhiteSpace(categoryName) &&
                !string.Equals(category?.Name, categoryName, StringComparison.Ordinal))
            {
                continue;
            }

            article.Category = category;
            articles.Add(article);
        }

        return articles.OrderBy(a => a.Name, StringComparer.Ordinal).ToList();
    }

    public async Task<Article?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var article = FromHashEntries(id, await Db.HashGetAllAsync(RedisKeys.Article(id)), null);
        if (article is null)
        {
            return null;
        }
        article.Category = await LoadCategoryAsync(article.CategoryId);
        return article;
    }

    public async Task<Dictionary<Guid, Article>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var result = new Dictionary<Guid, Article>();
        foreach (var id in ids.Distinct())
        {
            // Category isn't needed by any caller of GetByIdsAsync (cart/order flows only need
            // Name/Price/Stock) — skip resolving it to avoid N extra round-trips.
            var article = FromHashEntries(id, await Db.HashGetAllAsync(RedisKeys.Article(id)), null);
            if (article is not null)
            {
                result[id] = article;
            }
        }
        return result;
    }

    public async Task<Article> CreateAsync(Article article, CancellationToken ct = default)
    {
        var tx = Db.CreateTransaction();
        _ = tx.HashSetAsync(RedisKeys.Article(article.Id), ToHashEntries(article));
        _ = tx.SetAddAsync(RedisKeys.ArticlesAll, article.Id.ToString());
        await tx.ExecuteAsync();
        return article;
    }

    public Task UpdateAsync(Article article, CancellationToken ct = default) =>
        Db.HashSetAsync(RedisKeys.Article(article.Id), ToHashEntries(article));

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        if (!await Db.KeyExistsAsync(RedisKeys.Article(id)))
        {
            return false;
        }
        var tx = Db.CreateTransaction();
        _ = tx.KeyDeleteAsync(RedisKeys.Article(id));
        _ = tx.SetRemoveAsync(RedisKeys.ArticlesAll, id.ToString());
        await tx.ExecuteAsync();
        return true;
    }

    public async Task<int> CountByCategoryAsync(Guid categoryId, CancellationToken ct = default)
    {
        var ids = await Db.SetMembersAsync(RedisKeys.ArticlesAll);
        var count = 0;
        foreach (var idValue in ids)
        {
            var categoryIdField = await Db.HashGetAsync(RedisKeys.Article(Guid.Parse((string)idValue!)), Field.CategoryId);
            if (!categoryIdField.IsNullOrEmpty && Guid.Parse((string)categoryIdField!) == categoryId)
            {
                count++;
            }
        }
        return count;
    }

    private async Task<Category?> LoadCategoryAsync(Guid categoryId)
    {
        var json = await Db.StringGetAsync(RedisKeys.Category(categoryId));
        return json.IsNullOrEmpty ? null : System.Text.Json.JsonSerializer.Deserialize<Category>((string)json!);
    }
}
