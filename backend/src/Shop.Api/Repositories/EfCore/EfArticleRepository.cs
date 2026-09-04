using Microsoft.EntityFrameworkCore;
using Shop.Api.Data;
using Shop.Api.Models;

namespace Shop.Api.Repositories.EfCore;

public class EfArticleRepository(ShopDbContext db) : IArticleRepository
{
    public async Task<List<Article>> ListAsync(string? search, string? categoryName, CancellationToken ct = default)
    {
        var query = db.Articles.AsNoTracking().Include(a => a.Category).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(a => EF.Functions.ILike(a.Name, $"%{search}%"));
        }

        if (!string.IsNullOrWhiteSpace(categoryName))
        {
            query = query.Where(a => a.Category!.Name == categoryName);
        }

        return await query.OrderBy(a => a.Name).ToListAsync(ct);
    }

    public Task<Article?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Articles.AsNoTracking().Include(a => a.Category).FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task<Dictionary<Guid, Article>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var idList = ids.ToList();
        return db.Articles.Where(a => idList.Contains(a.Id)).ToDictionaryAsync(a => a.Id, ct);
    }

    public async Task<Article> CreateAsync(Article article, CancellationToken ct = default)
    {
        AttachExistingCategory(article);
        db.Articles.Add(article);
        await db.SaveChangesAsync(ct);
        return article;
    }

    public async Task UpdateAsync(Article article, CancellationToken ct = default)
    {
        AttachExistingCategory(article);
        // The tracked instance (loaded via FindAsync elsewhere in the same scoped DbContext) is
        // what actually gets its properties assigned by the caller before this runs — this just
        // commits. Attaching defensively covers a caller that passed in a detached instance.
        if (db.Entry(article).State == EntityState.Detached)
        {
            db.Articles.Update(article);
        }
        await db.SaveChangesAsync(ct);
    }

    // ArticlesController resolves the Category via ICategoryRepository.GetByIdAsync (AsNoTracking
    // — it's a read-only lookup everywhere else it's used) and hangs it off Article.Category so
    // ArticleDto.FromEntity has a name to read. Handing that untracked instance straight to
    // db.Articles.Add/Update would make EF's change tracker treat it as a brand-new row to
    // INSERT — it has no way to know this Category already exists — which collides with the
    // real one on its primary key (confirmed for real: 23505 duplicate key on PK_Categories,
    // breaking article creation/update entirely once this repository split introduced the
    // untracked read). Attach() marks it Unchanged instead, telling EF "this already exists,
    // don't touch it" — correct, since nothing here ever legitimately modifies a Category via
    // an Article's navigation.
    private void AttachExistingCategory(Article article)
    {
        if (article.Category is not null && db.Entry(article.Category).State == EntityState.Detached)
        {
            db.Attach(article.Category);
        }
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var article = await db.Articles.FindAsync([id], ct);
        if (article is null)
        {
            return false;
        }
        db.Articles.Remove(article);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public Task<int> CountByCategoryAsync(Guid categoryId, CancellationToken ct = default) =>
        db.Articles.CountAsync(a => a.CategoryId == categoryId, ct);
}
