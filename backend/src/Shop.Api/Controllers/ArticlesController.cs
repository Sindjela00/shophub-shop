using Microsoft.AspNetCore.Mvc;
using Shop.Api.Auth;
using Shop.Api.Contracts;
using Shop.Api.Models;
using Shop.Api.Repositories;

namespace Shop.Api.Controllers;

[ApiController]
[Route("api/articles")]
public class ArticlesController(IArticleRepository articles, ICategoryRepository categories) : ControllerBase
{
    // Customer-facing: browse/search the catalog. `category` filters by name (not id) to
    // keep the querystring human-readable/shareable, e.g. ?category=Jackets.
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ArticleDto>>> List([FromQuery] string? search, [FromQuery] string? category)
    {
        var result = await articles.ListAsync(search, category);
        return Ok(result.Select(ArticleDto.FromEntity));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ArticleDto>> GetById(Guid id)
    {
        var article = await articles.GetByIdAsync(id);
        return article is null ? NotFound() : Ok(ArticleDto.FromEntity(article));
    }

    // Admin-only: manage the catalog.
    [HttpPost]
    [ServiceFilter(typeof(AdminApiKeyFilter))]
    public async Task<ActionResult<ArticleDto>> Create(UpsertArticleRequest request)
    {
        var category = await categories.GetByIdAsync(request.CategoryId);
        if (category is null)
        {
            return BadRequest(new ErrorResponse($"Category {request.CategoryId} not found."));
        }

        var article = new Article
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            CategoryId = request.CategoryId,
            Category = category,
            Stock = request.Stock,
        };
        await articles.CreateAsync(article);
        return CreatedAtAction(nameof(GetById), new { id = article.Id }, ArticleDto.FromEntity(article));
    }

    [HttpPut("{id:guid}")]
    [ServiceFilter(typeof(AdminApiKeyFilter))]
    public async Task<ActionResult<ArticleDto>> Update(Guid id, UpsertArticleRequest request)
    {
        var article = await articles.GetByIdAsync(id);
        if (article is null)
        {
            return NotFound();
        }

        var category = await categories.GetByIdAsync(request.CategoryId);
        if (category is null)
        {
            return BadRequest(new ErrorResponse($"Category {request.CategoryId} not found."));
        }

        article.Name = request.Name;
        article.Description = request.Description;
        article.Price = request.Price;
        article.CategoryId = request.CategoryId;
        article.Category = category;
        article.Stock = request.Stock;
        await articles.UpdateAsync(article);
        return Ok(ArticleDto.FromEntity(article));
    }

    [HttpDelete("{id:guid}")]
    [ServiceFilter(typeof(AdminApiKeyFilter))]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await articles.DeleteAsync(id);
        return deleted ? NoContent() : NotFound();
    }
}
