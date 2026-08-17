using Shop.Api.Models;

namespace Shop.Api.Contracts;

public record CategoryDto(Guid Id, string Name)
{
    public static CategoryDto FromEntity(Category category) => new(category.Id, category.Name);
}

public record UpsertCategoryRequest(string Name);
