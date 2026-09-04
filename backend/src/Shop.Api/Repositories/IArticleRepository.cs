using Shop.Api.Models;

namespace Shop.Api.Repositories;

/// <summary>
/// Backend-agnostic catalog storage — two implementations exist (EfCore, Redis), selected in
/// Program.cs based on which connection string is configured, so a "light" tier shop (Redis)
/// gets the exact same catalog behavior as a "standard" one (Postgres via EF Core).
/// </summary>
public interface IArticleRepository
{
    /// <summary>Ordered by Name, each with its Category populated. `search` matches a
    /// case-insensitive substring of Name; `categoryName` filters to that category exactly —
    /// both optional, both matching ArticlesController's existing query semantics.</summary>
    Task<List<Article>> ListAsync(string? search, string? categoryName, CancellationToken ct = default);

    /// <summary>Null if not found. Category is populated.</summary>
    Task<Article?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Only the articles that exist are present in the result — callers diff against
    /// the requested id list themselves to detect missing ones (matches the existing
    /// Except(articles.Keys) pattern in OrderService/CartsController).</summary>
    Task<Dictionary<Guid, Article>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);

    /// <summary>Category must already be a valid, existing category — that check is the
    /// caller's responsibility (ArticlesController does it before calling this).</summary>
    Task<Article> CreateAsync(Article article, CancellationToken ct = default);

    Task UpdateAsync(Article article, CancellationToken ct = default);

    /// <summary>False if it didn't exist.</summary>
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>Used by CategoriesController.Delete to refuse deleting a category still in use.</summary>
    Task<int> CountByCategoryAsync(Guid categoryId, CancellationToken ct = default);
}
