using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Api.Auth;
using Shop.Api.Contracts;
using Shop.Api.Data;
using Shop.Api.Models;

namespace Shop.Api.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController(ShopDbContext db) : ControllerBase
{
    // Customer-facing: populate category filters/menus.
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> List()
    {
        var categories = await db.Categories.AsNoTracking().OrderBy(c => c.Name).ToListAsync();
        return Ok(categories.Select(CategoryDto.FromEntity));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CategoryDto>> GetById(Guid id)
    {
        var category = await db.Categories.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        return category is null ? NotFound() : Ok(CategoryDto.FromEntity(category));
    }

    // Admin-only: manage the category list articles are filed under.
    [HttpPost]
    [ServiceFilter(typeof(AdminApiKeyFilter))]
    public async Task<ActionResult<CategoryDto>> Create(UpsertCategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new ErrorResponse("Name is required."));
        }

        if (await db.Categories.AnyAsync(c => c.Name == request.Name))
        {
            return Conflict(new ErrorResponse($"A category named '{request.Name}' already exists."));
        }

        var category = new Category { Id = Guid.NewGuid(), Name = request.Name };
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = category.Id }, CategoryDto.FromEntity(category));
    }

    [HttpPut("{id:guid}")]
    [ServiceFilter(typeof(AdminApiKeyFilter))]
    public async Task<ActionResult<CategoryDto>> Update(Guid id, UpsertCategoryRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new ErrorResponse("Name is required."));
        }

        var category = await db.Categories.FindAsync(id);
        if (category is null)
        {
            return NotFound();
        }

        if (await db.Categories.AnyAsync(c => c.Id != id && c.Name == request.Name))
        {
            return Conflict(new ErrorResponse($"A category named '{request.Name}' already exists."));
        }

        category.Name = request.Name;
        await db.SaveChangesAsync();
        return Ok(CategoryDto.FromEntity(category));
    }

    [HttpDelete("{id:guid}")]
    [ServiceFilter(typeof(AdminApiKeyFilter))]
    public async Task<IActionResult> Delete(Guid id)
    {
        var category = await db.Categories.FindAsync(id);
        if (category is null)
        {
            return NotFound();
        }

        var articleCount = await db.Articles.CountAsync(a => a.CategoryId == id);
        if (articleCount > 0)
        {
            return Conflict(new ErrorResponse(
                $"'{category.Name}' is still used by {articleCount} article(s). Reassign or delete them first."));
        }

        db.Categories.Remove(category);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
