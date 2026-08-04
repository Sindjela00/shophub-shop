using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Api.Auth;
using Shop.Api.Contracts;
using Shop.Api.Data;
using Shop.Api.Models;

namespace Shop.Api.Controllers;

[ApiController]
[Route("api/articles")]
public class ArticlesController(ShopDbContext db) : ControllerBase
{
    // Customer-facing: browse/search the catalog.
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ArticleDto>>> List([FromQuery] string? search, [FromQuery] string? category)
    {
        var query = db.Articles.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(a => EF.Functions.ILike(a.Name, $"%{search}%"));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(a => a.Category == category);
        }

        var articles = await query.OrderBy(a => a.Name).ToListAsync();
        return Ok(articles.Select(ArticleDto.FromEntity));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ArticleDto>> GetById(Guid id)
    {
        var article = await db.Articles.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
        return article is null ? NotFound() : Ok(ArticleDto.FromEntity(article));
    }

    // Admin-only: manage the catalog.
    [HttpPost]
    [ServiceFilter(typeof(AdminApiKeyFilter))]
    public async Task<ActionResult<ArticleDto>> Create(UpsertArticleRequest request)
    {
        var article = new Article
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            Category = request.Category,
            Stock = request.Stock,
        };
        db.Articles.Add(article);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = article.Id }, ArticleDto.FromEntity(article));
    }

    [HttpPut("{id:guid}")]
    [ServiceFilter(typeof(AdminApiKeyFilter))]
    public async Task<ActionResult<ArticleDto>> Update(Guid id, UpsertArticleRequest request)
    {
        var article = await db.Articles.FindAsync(id);
        if (article is null)
        {
            return NotFound();
        }

        article.Name = request.Name;
        article.Description = request.Description;
        article.Price = request.Price;
        article.Category = request.Category;
        article.Stock = request.Stock;
        await db.SaveChangesAsync();
        return Ok(ArticleDto.FromEntity(article));
    }

    [HttpDelete("{id:guid}")]
    [ServiceFilter(typeof(AdminApiKeyFilter))]
    public async Task<IActionResult> Delete(Guid id)
    {
        var article = await db.Articles.FindAsync(id);
        if (article is null)
        {
            return NotFound();
        }

        db.Articles.Remove(article);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
