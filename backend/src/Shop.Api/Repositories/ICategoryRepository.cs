using Shop.Api.Models;

namespace Shop.Api.Repositories;

public interface ICategoryRepository
{
    /// <summary>Ordered by Name.</summary>
    Task<List<Category>> ListAsync(CancellationToken ct = default);

    Task<Category?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Case-sensitive exact match, mirroring the Postgres unique index this mirrors.
    /// Pass <paramref name="excludingId"/> when checking during an update, so a category isn't
    /// reported as colliding with its own current name.</summary>
    Task<bool> NameExistsAsync(string name, Guid? excludingId = null, CancellationToken ct = default);

    Task<Category> CreateAsync(Category category, CancellationToken ct = default);

    Task UpdateAsync(Category category, CancellationToken ct = default);

    /// <summary>False if it didn't exist. Caller (CategoriesController) is responsible for
    /// checking CountByCategoryAsync first — this doesn't itself refuse an in-use category.</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}
