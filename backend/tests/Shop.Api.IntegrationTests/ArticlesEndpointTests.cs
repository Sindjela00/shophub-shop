using System.Net;
using System.Net.Http.Json;
using Shop.Api.Contracts;
using Xunit;

namespace Shop.Api.IntegrationTests;

[Collection(ShopApiCollection.Name)]
public class ArticlesEndpointTests(ShopApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    private static UpsertArticleRequest NewArticleRequest(string? name = null, string category = "IntegrationTest") =>
        new(name ?? $"Test Article {Guid.NewGuid()}", "desc", 20m, category, 10);

    private async Task<ArticleDto> CreateArticleAsync(UpsertArticleRequest? request = null)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/articles")
        {
            Content = JsonContent.Create(request ?? NewArticleRequest(), options: TestJson.Options),
        };
        req.Headers.Add("X-Admin-Key", ShopApiFactory.AdminApiKey);

        var response = await _client.SendAsync(req);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ArticleDto>(TestJson.Options))!;
    }

    [Fact]
    public async Task Create_with_admin_key_returns_201_with_the_created_article()
    {
        var request = NewArticleRequest("Aurora Windbreaker");

        var req = new HttpRequestMessage(HttpMethod.Post, "/api/articles") { Content = JsonContent.Create(request, options: TestJson.Options) };
        req.Headers.Add("X-Admin-Key", ShopApiFactory.AdminApiKey);
        var response = await _client.SendAsync(req);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var article = await response.Content.ReadFromJsonAsync<ArticleDto>(TestJson.Options);
        Assert.Equal("Aurora Windbreaker", article!.Name);
        Assert.Equal(20m, article.Price);
    }

    [Fact]
    public async Task Create_without_admin_key_returns_401()
    {
        var response = await _client.PostAsJsonAsync("/api/articles", NewArticleRequest(), TestJson.Options);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetById_returns_404_for_unknown_id()
    {
        var response = await _client.GetAsync($"/api/articles/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetById_returns_the_article_after_creation()
    {
        var created = await CreateArticleAsync();

        var response = await _client.GetAsync($"/api/articles/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var fetched = await response.Content.ReadFromJsonAsync<ArticleDto>(TestJson.Options);
        Assert.Equal(created.Id, fetched!.Id);
    }

    [Fact]
    public async Task Update_changes_the_articles_fields()
    {
        var created = await CreateArticleAsync();
        var updateReq = new HttpRequestMessage(HttpMethod.Put, $"/api/articles/{created.Id}")
        {
            Content = JsonContent.Create(new UpsertArticleRequest("Renamed", "new desc", 99m, "NewCat", 3), options: TestJson.Options),
        };
        updateReq.Headers.Add("X-Admin-Key", ShopApiFactory.AdminApiKey);

        var response = await _client.SendAsync(updateReq);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<ArticleDto>(TestJson.Options);
        Assert.Equal("Renamed", updated!.Name);
        Assert.Equal(99m, updated.Price);
        Assert.Equal(3, updated.Stock);
    }

    [Fact]
    public async Task Delete_removes_the_article()
    {
        var created = await CreateArticleAsync();
        var deleteReq = new HttpRequestMessage(HttpMethod.Delete, $"/api/articles/{created.Id}");
        deleteReq.Headers.Add("X-Admin-Key", ShopApiFactory.AdminApiKey);

        var deleteResponse = await _client.SendAsync(deleteReq);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/articles/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task List_with_search_finds_the_article_by_a_unique_name_fragment()
    {
        var uniqueName = $"Zzyzx-{Guid.NewGuid():N}";
        await CreateArticleAsync(NewArticleRequest(uniqueName));

        var response = await _client.GetAsync($"/api/articles?search={uniqueName}");

        var articles = await response.Content.ReadFromJsonAsync<List<ArticleDto>>(TestJson.Options);
        Assert.Single(articles!);
        Assert.Equal(uniqueName, articles![0].Name);
    }

    [Fact]
    public async Task List_with_category_filters_to_that_category_only()
    {
        var uniqueCategory = $"Cat-{Guid.NewGuid():N}";
        await CreateArticleAsync(NewArticleRequest(category: uniqueCategory));
        await CreateArticleAsync(NewArticleRequest(category: uniqueCategory));
        await CreateArticleAsync(NewArticleRequest(category: "SomeOtherCategory"));

        var response = await _client.GetAsync($"/api/articles?category={uniqueCategory}");

        var articles = await response.Content.ReadFromJsonAsync<List<ArticleDto>>(TestJson.Options);
        Assert.Equal(2, articles!.Count);
        Assert.All(articles, a => Assert.Equal(uniqueCategory, a.Category));
    }
}
