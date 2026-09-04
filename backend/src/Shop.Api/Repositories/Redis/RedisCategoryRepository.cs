using System.Text.Json;
using Shop.Api.Models;
using StackExchange.Redis;

namespace Shop.Api.Repositories.Redis;

public class RedisCategoryRepository(IConnectionMultiplexer redis) : ICategoryRepository
{
    private IDatabase Db => redis.GetDatabase();

    public async Task<List<Category>> ListAsync(CancellationToken ct = default)
    {
        var ids = await Db.SetMembersAsync(RedisKeys.CategoriesAll);
        var categories = new List<Category>();
        foreach (var idValue in ids)
        {
            var category = await GetByIdAsync(Guid.Parse((string)idValue!), ct);
            if (category is not null)
            {
                categories.Add(category);
            }
        }
        return categories.OrderBy(c => c.Name, StringComparer.Ordinal).ToList();
    }

    public async Task<Category?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var json = await Db.StringGetAsync(RedisKeys.Category(id));
        return json.IsNullOrEmpty ? null : JsonSerializer.Deserialize<Category>((string)json!);
    }

    public async Task<bool> NameExistsAsync(string name, Guid? excludingId = null, CancellationToken ct = default)
    {
        var existingId = await Db.HashGetAsync(RedisKeys.CategoryNameIndex, name);
        if (existingId.IsNullOrEmpty)
        {
            return false;
        }
        return excludingId is null || Guid.Parse((string)existingId!) != excludingId;
    }

    public async Task<Category> CreateAsync(Category category, CancellationToken ct = default)
    {
        var tx = Db.CreateTransaction();
        _ = tx.StringSetAsync(RedisKeys.Category(category.Id), JsonSerializer.Serialize(category));
        _ = tx.SetAddAsync(RedisKeys.CategoriesAll, category.Id.ToString());
        _ = tx.HashSetAsync(RedisKeys.CategoryNameIndex, category.Name, category.Id.ToString());
        await tx.ExecuteAsync();
        return category;
    }

    public async Task UpdateAsync(Category category, CancellationToken ct = default)
    {
        var previous = await GetByIdAsync(category.Id, ct);
        var tx = Db.CreateTransaction();
        _ = tx.StringSetAsync(RedisKeys.Category(category.Id), JsonSerializer.Serialize(category));
        if (previous is not null && previous.Name != category.Name)
        {
            // The name index is keyed by name, not id — renaming means the old name entry
            // would otherwise point at this category's id forever, blocking that old name
            // from ever being reused by a different category.
            _ = tx.HashDeleteAsync(RedisKeys.CategoryNameIndex, previous.Name);
        }
        _ = tx.HashSetAsync(RedisKeys.CategoryNameIndex, category.Name, category.Id.ToString());
        await tx.ExecuteAsync();
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var category = await GetByIdAsync(id, ct);
        if (category is null)
        {
            return false;
        }
        var tx = Db.CreateTransaction();
        _ = tx.KeyDeleteAsync(RedisKeys.Category(id));
        _ = tx.SetRemoveAsync(RedisKeys.CategoriesAll, id.ToString());
        _ = tx.HashDeleteAsync(RedisKeys.CategoryNameIndex, category.Name);
        await tx.ExecuteAsync();
        return true;
    }
}
