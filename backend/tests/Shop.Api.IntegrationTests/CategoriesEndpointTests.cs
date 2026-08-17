using System.Net;
using System.Net.Http.Json;
using Shop.Api.Contracts;
using Xunit;

namespace Shop.Api.IntegrationTests;

[Collection(ShopApiCollection.Name)]
public class CategoriesEndpointTests(ShopApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<CategoryDto> CreateCategoryAsync(string? name = null)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/categories")
        {
            Content = JsonContent.Create(new UpsertCategoryRequest(name ?? $"Category {Guid.NewGuid()}"), options: TestJson.Options),
        };
        req.Headers.Add("X-Admin-Key", ShopApiFactory.AdminApiKey);

        var response = await _client.SendAsync(req);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CategoryDto>(TestJson.Options))!;
    }

    [Fact]
    public async Task Create_with_admin_key_returns_201_with_the_created_category()
    {
        var name = $"Category {Guid.NewGuid()}";
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/categories")
        {
            Content = JsonContent.Create(new UpsertCategoryRequest(name), options: TestJson.Options),
        };
        req.Headers.Add("X-Admin-Key", ShopApiFactory.AdminApiKey);

        var response = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var category = await response.Content.ReadFromJsonAsync<CategoryDto>(TestJson.Options);
        Assert.Equal(name, category!.Name);
    }

    [Fact]
    public async Task Create_without_admin_key_returns_401()
    {
        var response = await _client.PostAsJsonAsync("/api/categories", new UpsertCategoryRequest("Nope"), TestJson.Options);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_with_blank_name_returns_400()
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/categories")
        {
            Content = JsonContent.Create(new UpsertCategoryRequest("   "), options: TestJson.Options),
        };
        req.Headers.Add("X-Admin-Key", ShopApiFactory.AdminApiKey);

        var response = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_with_a_name_already_in_use_returns_409()
    {
        var existing = await CreateCategoryAsync();
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/categories")
        {
            Content = JsonContent.Create(new UpsertCategoryRequest(existing.Name), options: TestJson.Options),
        };
        req.Headers.Add("X-Admin-Key", ShopApiFactory.AdminApiKey);

        var response = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetById_returns_404_for_unknown_id()
    {
        var response = await _client.GetAsync($"/api/categories/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task List_includes_a_newly_created_category()
    {
        var created = await CreateCategoryAsync();

        var response = await _client.GetAsync("/api/categories");

        var categories = await response.Content.ReadFromJsonAsync<List<CategoryDto>>(TestJson.Options);
        Assert.Contains(categories!, c => c.Id == created.Id);
    }

    [Fact]
    public async Task Update_renames_the_category()
    {
        var created = await CreateCategoryAsync();
        var newName = $"Renamed {Guid.NewGuid()}";
        var req = new HttpRequestMessage(HttpMethod.Put, $"/api/categories/{created.Id}")
        {
            Content = JsonContent.Create(new UpsertCategoryRequest(newName), options: TestJson.Options),
        };
        req.Headers.Add("X-Admin-Key", ShopApiFactory.AdminApiKey);

        var response = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<CategoryDto>(TestJson.Options);
        Assert.Equal(newName, updated!.Name);
    }

    [Fact]
    public async Task Update_to_a_name_already_used_by_another_category_returns_409()
    {
        var first = await CreateCategoryAsync();
        var second = await CreateCategoryAsync();
        var req = new HttpRequestMessage(HttpMethod.Put, $"/api/categories/{second.Id}")
        {
            Content = JsonContent.Create(new UpsertCategoryRequest(first.Name), options: TestJson.Options),
        };
        req.Headers.Add("X-Admin-Key", ShopApiFactory.AdminApiKey);

        var response = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Delete_removes_an_unused_category()
    {
        var created = await CreateCategoryAsync();
        var deleteReq = new HttpRequestMessage(HttpMethod.Delete, $"/api/categories/{created.Id}");
        deleteReq.Headers.Add("X-Admin-Key", ShopApiFactory.AdminApiKey);

        var deleteResponse = await _client.SendAsync(deleteReq);

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        var getResponse = await _client.GetAsync($"/api/categories/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_of_a_category_still_used_by_an_article_returns_409_and_does_not_delete_it()
    {
        var category = await CreateCategoryAsync();
        var articleReq = new HttpRequestMessage(HttpMethod.Post, "/api/articles")
        {
            Content = JsonContent.Create(
                new UpsertArticleRequest($"Article {Guid.NewGuid()}", "desc", 10m, category.Id, 5),
                options: TestJson.Options),
        };
        articleReq.Headers.Add("X-Admin-Key", ShopApiFactory.AdminApiKey);
        await _client.SendAsync(articleReq);

        var deleteReq = new HttpRequestMessage(HttpMethod.Delete, $"/api/categories/{category.Id}");
        deleteReq.Headers.Add("X-Admin-Key", ShopApiFactory.AdminApiKey);
        var deleteResponse = await _client.SendAsync(deleteReq);

        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);
        var getResponse = await _client.GetAsync($"/api/categories/{category.Id}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
    }
}
