using Microsoft.EntityFrameworkCore;
using Shop.Api.Data;
using Shop.Api.Models;

namespace Shop.Api.Repositories.EfCore;

public class EfCategoryRepository(ShopDbContext db) : ICategoryRepository
{
    public Task<List<Category>> ListAsync(CancellationToken ct = default) =>
        db.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync(ct);

    public Task<Category?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<bool> NameExistsAsync(string name, Guid? excludingId = null, CancellationToken ct = default) =>
        db.Categories.AnyAsync(c => c.Name == name && (excludingId == null || c.Id != excludingId), ct);

    public async Task<Category> CreateAsync(Category category, CancellationToken ct = default)
    {
        db.Categories.Add(category);
        await db.SaveChangesAsync(ct);
        return category;
    }

    public async Task UpdateAsync(Category category, CancellationToken ct = default)
    {
        if (db.Entry(category).State == EntityState.Detached)
        {
            db.Categories.Update(category);
        }
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var category = await db.Categories.FindAsync([id], ct);
        if (category is null)
        {
            return false;
        }
        db.Categories.Remove(category);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
