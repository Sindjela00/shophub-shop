using Microsoft.EntityFrameworkCore;
using Shop.Api.Auth;
using Shop.Api.Data;
using Shop.Api.Models;

namespace Shop.Api.Endpoints;

public record ArticleDto(Guid Id, string Name, string Description, decimal Price, string Category, int Stock)
{
    public static ArticleDto FromEntity(Article article) =>
        new(article.Id, article.Name, article.Description, article.Price, article.Category, article.Stock);
}

public record UpsertArticleRequest(string Name, string Description, decimal Price, string Category, int Stock);

public static class ArticleEndpoints
{
    public static RouteGroupBuilder MapArticleEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/articles").WithTags("Articles");

        // Customer-facing: browse/search the catalog.
        group.MapGet("/", async (string? search, string? category, ShopDbContext db) =>
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
            return Results.Ok(articles.Select(ArticleDto.FromEntity));
        })
        .WithName("ListArticles");

        group.MapGet("/{id:guid}", async (Guid id, ShopDbContext db) =>
        {
            var article = await db.Articles.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
            return article is null ? Results.NotFound() : Results.Ok(ArticleDto.FromEntity(article));
        })
        .WithName("GetArticle");

        // Admin-only: manage the catalog.
        group.MapPost("/", async (UpsertArticleRequest request, ShopDbContext db) =>
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
            return Results.Created($"/api/articles/{article.Id}", ArticleDto.FromEntity(article));
        })
        .AddEndpointFilter<AdminApiKeyFilter>()
        .WithName("CreateArticle");

        group.MapPut("/{id:guid}", async (Guid id, UpsertArticleRequest request, ShopDbContext db) =>
        {
            var article = await db.Articles.FindAsync(id);
            if (article is null)
            {
                return Results.NotFound();
            }

            article.Name = request.Name;
            article.Description = request.Description;
            article.Price = request.Price;
            article.Category = request.Category;
            article.Stock = request.Stock;
            await db.SaveChangesAsync();
            return Results.Ok(ArticleDto.FromEntity(article));
        })
        .AddEndpointFilter<AdminApiKeyFilter>()
        .WithName("UpdateArticle");

        group.MapDelete("/{id:guid}", async (Guid id, ShopDbContext db) =>
        {
            var article = await db.Articles.FindAsync(id);
            if (article is null)
            {
                return Results.NotFound();
            }

            db.Articles.Remove(article);
            await db.SaveChangesAsync();
            return Results.NoContent();
        })
        .AddEndpointFilter<AdminApiKeyFilter>()
        .WithName("DeleteArticle");

        return group;
    }
}
