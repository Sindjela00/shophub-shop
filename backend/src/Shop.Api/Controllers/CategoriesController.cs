using Microsoft.AspNetCore.Mvc;
using Shop.Api.Auth;
using Shop.Api.Contracts;
using Shop.Api.Models;
using Shop.Api.Repositories;

namespace Shop.Api.Controllers;

[ApiController]
[Route("api/categories")]
public class CategoriesController(ICategoryRepository categories, IArticleRepository articles) : ControllerBase
{
    // Customer-facing: populate category filters/menus.
    [HttpGet]
    public async Task<ActionResult<IEnumerable<CategoryDto>>> List()
    {
        var result = await categories.ListAsync();
        return Ok(result.Select(CategoryDto.FromEntity));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CategoryDto>> GetById(Guid id)
    {
        var category = await categories.GetByIdAsync(id);
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

        if (await categories.NameExistsAsync(request.Name))
        {
            return Conflict(new ErrorResponse($"A category named '{request.Name}' already exists."));
        }

        var category = new Category { Id = Guid.NewGuid(), Name = request.Name };
        await categories.CreateAsync(category);
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

        var category = await categories.GetByIdAsync(id);
        if (category is null)
        {
            return NotFound();
        }

        if (await categories.NameExistsAsync(request.Name, excludingId: id))
        {
            return Conflict(new ErrorResponse($"A category named '{request.Name}' already exists."));
        }

        category.Name = request.Name;
        await categories.UpdateAsync(category);
        return Ok(CategoryDto.FromEntity(category));
    }

    [HttpDelete("{id:guid}")]
    [ServiceFilter(typeof(AdminApiKeyFilter))]
    public async Task<IActionResult> Delete(Guid id)
    {
        var category = await categories.GetByIdAsync(id);
        if (category is null)
        {
            return NotFound();
        }

        var articleCount = await articles.CountByCategoryAsync(id);
        if (articleCount > 0)
        {
            return Conflict(new ErrorResponse(
                $"'{category.Name}' is still used by {articleCount} article(s). Reassign or delete them first."));
        }

        await categories.DeleteAsync(id);
        return NoContent();
    }
}
