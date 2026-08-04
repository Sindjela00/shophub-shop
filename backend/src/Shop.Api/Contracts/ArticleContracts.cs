using Shop.Api.Models;

namespace Shop.Api.Contracts;

public record ArticleDto(Guid Id, string Name, string Description, decimal Price, string Category, int Stock)
{
    public static ArticleDto FromEntity(Article article) =>
        new(article.Id, article.Name, article.Description, article.Price, article.Category, article.Stock);
}

public record UpsertArticleRequest(string Name, string Description, decimal Price, string Category, int Stock);
