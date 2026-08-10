using System.Net;
using System.Net.Http.Json;
using Shop.Api.Contracts;
using Xunit;

namespace Shop.Api.IntegrationTests;

[Collection(ShopApiCollection.Name)]
public class CartsEndpointTests(ShopApiFactory factory)
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<ArticleDto> CreateArticleAsync(decimal price = 10m, int stock = 5)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/articles")
        {
            Content = JsonContent.Create(new UpsertArticleRequest($"Cart Test Item {Guid.NewGuid()}", "desc", price, "CartTest", stock), options: TestJson.Options),
        };
        req.Headers.Add("X-Admin-Key", ShopApiFactory.AdminApiKey);
        var response = await _client.SendAsync(req);
        return (await response.Content.ReadFromJsonAsync<ArticleDto>(TestJson.Options))!;
    }

    private async Task<Guid> CreateCartAsync()
    {
        var response = await _client.PostAsync("/api/carts", content: null);
        var cart = await response.Content.ReadFromJsonAsync<CartDto>(TestJson.Options);
        return cart!.Id;
    }

    [Fact]
    public async Task Get_nonexistent_cart_returns_404()
    {
        var response = await _client.GetAsync($"/api/carts/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Adding_the_same_article_twice_merges_the_quantity()
    {
        var article = await CreateArticleAsync(price: 10m, stock: 10);
        var cartId = await CreateCartAsync();

        await _client.PostAsJsonAsync($"/api/carts/{cartId}/items", new AddCartItemRequest(article.Id, 2), TestJson.Options);
        var response = await _client.PostAsJsonAsync($"/api/carts/{cartId}/items", new AddCartItemRequest(article.Id, 1), TestJson.Options);

        var cart = await response.Content.ReadFromJsonAsync<CartDto>(TestJson.Options);
        var item = Assert.Single(cart!.Items);
        Assert.Equal(3, item.Quantity);
        Assert.Equal(30m, cart.Total);
    }

    [Fact]
    public async Task Adding_more_than_available_stock_returns_400()
    {
        var article = await CreateArticleAsync(stock: 2);
        var cartId = await CreateCartAsync();

        var response = await _client.PostAsJsonAsync($"/api/carts/{cartId}/items", new AddCartItemRequest(article.Id, 5), TestJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>(TestJson.Options);
        Assert.Contains("Insufficient stock", body!.Error);
    }

    [Fact]
    public async Task Setting_quantity_to_zero_removes_the_item()
    {
        var article = await CreateArticleAsync();
        var cartId = await CreateCartAsync();
        await _client.PostAsJsonAsync($"/api/carts/{cartId}/items", new AddCartItemRequest(article.Id, 1), TestJson.Options);

        var response = await _client.PutAsJsonAsync($"/api/carts/{cartId}/items/{article.Id}", new SetCartItemQuantityRequest(0), TestJson.Options);

        var cart = await response.Content.ReadFromJsonAsync<CartDto>(TestJson.Options);
        Assert.Empty(cart!.Items);
    }

    [Fact]
    public async Task Removing_an_item_takes_it_out_of_the_cart()
    {
        var article = await CreateArticleAsync();
        var cartId = await CreateCartAsync();
        await _client.PostAsJsonAsync($"/api/carts/{cartId}/items", new AddCartItemRequest(article.Id, 1), TestJson.Options);

        var response = await _client.DeleteAsync($"/api/carts/{cartId}/items/{article.Id}");

        var cart = await response.Content.ReadFromJsonAsync<CartDto>(TestJson.Options);
        Assert.Empty(cart!.Items);
    }

    [Fact]
    public async Task Get_reflects_current_article_price_and_computes_line_totals()
    {
        var article = await CreateArticleAsync(price: 7.5m);
        var cartId = await CreateCartAsync();
        await _client.PostAsJsonAsync($"/api/carts/{cartId}/items", new AddCartItemRequest(article.Id, 3), TestJson.Options);

        var response = await _client.GetAsync($"/api/carts/{cartId}");

        var cart = await response.Content.ReadFromJsonAsync<CartDto>(TestJson.Options);
        Assert.Equal(22.5m, cart!.Items[0].LineTotal);
        Assert.Equal(22.5m, cart.Total);
    }
}
