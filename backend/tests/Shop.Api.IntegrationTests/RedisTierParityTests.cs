using System.Net;
using System.Net.Http.Json;
using Shop.Api.Contracts;
using Shop.Api.Services;
using Xunit;

namespace Shop.Api.IntegrationTests;

/// <summary>
/// The same behaviors the Postgres-backed suites cover, run against a real Redis container
/// instead — this is what "the light tier is at parity" actually means, verified through the
/// real HTTP endpoints rather than by inspecting the repositories directly. If a behavior
/// holds here and in the Postgres suites, the storage swap is genuinely transparent to callers.
/// </summary>
[Collection(ShopApiRedisCollection.Name)]
public class RedisTierParityTests
{
    private readonly HttpClient _client;
    private readonly ShopApiRedisFactory _factory;

    public RedisTierParityTests(ShopApiRedisFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        _factory.PaymentVerification.Result = PaymentVerificationResult.Verified();
    }

    private HttpRequestMessage Admin(HttpMethod method, string url, object? body = null)
    {
        var req = new HttpRequestMessage(method, url);
        if (body is not null)
        {
            req.Content = JsonContent.Create(body, body.GetType(), options: TestJson.Options);
        }
        req.Headers.Add("X-Admin-Key", ShopApiRedisFactory.AdminApiKey);
        return req;
    }

    private async Task<CategoryDto> CreateCategoryAsync(string? name = null)
    {
        var response = await _client.SendAsync(Admin(HttpMethod.Post, "/api/categories", new UpsertCategoryRequest(name ?? $"Category {Guid.NewGuid()}")));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CategoryDto>(TestJson.Options))!;
    }

    private async Task<ArticleDto> CreateArticleAsync(string? name = null, decimal price = 20m, int stock = 10, Guid? categoryId = null)
    {
        var category = categoryId ?? (await CreateCategoryAsync()).Id;
        var request = new UpsertArticleRequest(name ?? $"Item {Guid.NewGuid()}", "desc", price, category, stock);
        var response = await _client.SendAsync(Admin(HttpMethod.Post, "/api/articles", request));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ArticleDto>(TestJson.Options))!;
    }

    private async Task<Guid> CreateCartWithItemAsync(Guid articleId, int quantity = 1)
    {
        var cart = await (await _client.PostAsync("/api/carts", content: null)).Content.ReadFromJsonAsync<CartDto>(TestJson.Options);
        await _client.PostAsJsonAsync($"/api/carts/{cart!.Id}/items", new AddCartItemRequest(articleId, quantity), TestJson.Options);
        return cart.Id;
    }

    // --- catalog ---

    [Fact]
    public async Task Article_round_trips_with_its_category_name_resolved()
    {
        var category = await CreateCategoryAsync();
        var created = await CreateArticleAsync("Aurora Windbreaker", price: 42.50m, categoryId: category.Id);

        var fetched = await (await _client.GetAsync($"/api/articles/{created.Id}")).Content.ReadFromJsonAsync<ArticleDto>(TestJson.Options);

        Assert.Equal("Aurora Windbreaker", fetched!.Name);
        Assert.Equal(42.50m, fetched.Price);
        // Resolved from the category's own key at read time, the equivalent of EF's Include.
        Assert.Equal(category.Name, fetched.CategoryName);
    }

    [Fact]
    public async Task Updating_an_article_changes_its_fields_and_category()
    {
        var created = await CreateArticleAsync();
        var newCategory = await CreateCategoryAsync();

        var response = await _client.SendAsync(Admin(HttpMethod.Put, $"/api/articles/{created.Id}",
            new UpsertArticleRequest("Renamed", "new desc", 99m, newCategory.Id, 3)));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<ArticleDto>(TestJson.Options);
        Assert.Equal("Renamed", updated!.Name);
        Assert.Equal(99m, updated.Price);
        Assert.Equal(3, updated.Stock);
        Assert.Equal(newCategory.Id, updated.CategoryId);
    }

    [Fact]
    public async Task Deleting_an_article_removes_it_from_get_and_list()
    {
        var created = await CreateArticleAsync();

        Assert.Equal(HttpStatusCode.NoContent, (await _client.SendAsync(Admin(HttpMethod.Delete, $"/api/articles/{created.Id}"))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/articles/{created.Id}")).StatusCode);

        var all = await (await _client.GetAsync("/api/articles")).Content.ReadFromJsonAsync<List<ArticleDto>>(TestJson.Options);
        Assert.DoesNotContain(all!, a => a.Id == created.Id);
    }

    [Fact]
    public async Task Article_creation_with_an_unknown_category_returns_400()
    {
        var response = await _client.SendAsync(Admin(HttpMethod.Post, "/api/articles",
            new UpsertArticleRequest("Orphan", "desc", 10m, Guid.NewGuid(), 1)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task List_search_matches_a_name_fragment_case_insensitively()
    {
        var uniqueName = $"Zzyzx-{Guid.NewGuid():N}";
        await CreateArticleAsync(uniqueName);

        // Lowercased on purpose: the Postgres tier uses ILIKE, so the Redis tier has to match
        // case-insensitively too or the same request would behave differently per tier.
        var articles = await (await _client.GetAsync($"/api/articles?search={uniqueName.ToLowerInvariant()}"))
            .Content.ReadFromJsonAsync<List<ArticleDto>>(TestJson.Options);

        Assert.Single(articles!);
        Assert.Equal(uniqueName, articles![0].Name);
    }

    [Fact]
    public async Task List_filtered_by_category_returns_only_that_categorys_articles()
    {
        var category = await CreateCategoryAsync($"Cat-{Guid.NewGuid():N}");
        var other = await CreateCategoryAsync();
        await CreateArticleAsync(categoryId: category.Id);
        await CreateArticleAsync(categoryId: category.Id);
        await CreateArticleAsync(categoryId: other.Id);

        var articles = await (await _client.GetAsync($"/api/articles?category={category.Name}"))
            .Content.ReadFromJsonAsync<List<ArticleDto>>(TestJson.Options);

        Assert.Equal(2, articles!.Count);
        Assert.All(articles, a => Assert.Equal(category.Name, a.CategoryName));
    }

    [Fact]
    public async Task List_is_ordered_by_name()
    {
        var prefix = $"Sort-{Guid.NewGuid():N}";
        await CreateArticleAsync($"{prefix}-C");
        await CreateArticleAsync($"{prefix}-A");
        await CreateArticleAsync($"{prefix}-B");

        var articles = await (await _client.GetAsync($"/api/articles?search={prefix}"))
            .Content.ReadFromJsonAsync<List<ArticleDto>>(TestJson.Options);

        Assert.Equal([$"{prefix}-A", $"{prefix}-B", $"{prefix}-C"], articles!.Select(a => a.Name));
    }

    // --- categories ---

    [Fact]
    public async Task Duplicate_category_name_returns_409()
    {
        var name = $"Unique-{Guid.NewGuid():N}";
        await CreateCategoryAsync(name);

        var response = await _client.SendAsync(Admin(HttpMethod.Post, "/api/categories", new UpsertCategoryRequest(name)));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Renaming_a_category_frees_its_previous_name_for_reuse()
    {
        var original = $"Original-{Guid.NewGuid():N}";
        var category = await CreateCategoryAsync(original);

        var renamed = await _client.SendAsync(Admin(HttpMethod.Put, $"/api/categories/{category.Id}", new UpsertCategoryRequest($"Renamed-{Guid.NewGuid():N}")));
        Assert.Equal(HttpStatusCode.OK, renamed.StatusCode);

        // The name index is keyed by name — a stale entry here would wrongly report a conflict.
        var reuse = await _client.SendAsync(Admin(HttpMethod.Post, "/api/categories", new UpsertCategoryRequest(original)));
        Assert.Equal(HttpStatusCode.Created, reuse.StatusCode);
    }

    [Fact]
    public async Task Renaming_a_category_to_its_own_current_name_is_allowed()
    {
        var category = await CreateCategoryAsync();

        var response = await _client.SendAsync(Admin(HttpMethod.Put, $"/api/categories/{category.Id}", new UpsertCategoryRequest(category.Name)));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Deleting_a_category_still_used_by_an_article_returns_409()
    {
        var category = await CreateCategoryAsync();
        await CreateArticleAsync(categoryId: category.Id);

        var response = await _client.SendAsync(Admin(HttpMethod.Delete, $"/api/categories/{category.Id}"));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync($"/api/categories/{category.Id}")).StatusCode);
    }

    [Fact]
    public async Task Deleting_an_unused_category_succeeds()
    {
        var category = await CreateCategoryAsync();

        Assert.Equal(HttpStatusCode.NoContent, (await _client.SendAsync(Admin(HttpMethod.Delete, $"/api/categories/{category.Id}"))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/categories/{category.Id}")).StatusCode);
    }

    // --- cart ---

    [Fact]
    public async Task Adding_the_same_article_twice_merges_the_quantity()
    {
        var article = await CreateArticleAsync(stock: 10);
        var cartId = await CreateCartWithItemAsync(article.Id, quantity: 2);

        await _client.PostAsJsonAsync($"/api/carts/{cartId}/items", new AddCartItemRequest(article.Id, 3), TestJson.Options);

        var cart = await (await _client.GetAsync($"/api/carts/{cartId}")).Content.ReadFromJsonAsync<CartDto>(TestJson.Options);
        Assert.Single(cart!.Items);
        Assert.Equal(5, cart.Items[0].Quantity);
    }

    [Fact]
    public async Task Cart_reflects_current_article_price_and_computes_line_totals()
    {
        var article = await CreateArticleAsync(price: 12.50m, stock: 10);
        var cartId = await CreateCartWithItemAsync(article.Id, quantity: 4);

        var cart = await (await _client.GetAsync($"/api/carts/{cartId}")).Content.ReadFromJsonAsync<CartDto>(TestJson.Options);

        Assert.Equal(12.50m, cart!.Items[0].UnitPrice);
        Assert.Equal(50m, cart.Items[0].LineTotal);
        Assert.Equal(50m, cart.Total);
    }

    [Fact]
    public async Task Adding_more_than_available_stock_returns_400()
    {
        var article = await CreateArticleAsync(stock: 2);
        var cart = await (await _client.PostAsync("/api/carts", content: null)).Content.ReadFromJsonAsync<CartDto>(TestJson.Options);

        var response = await _client.PostAsJsonAsync($"/api/carts/{cart!.Id}/items", new AddCartItemRequest(article.Id, 5), TestJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Setting_quantity_to_zero_removes_the_item()
    {
        var article = await CreateArticleAsync(stock: 10);
        var cartId = await CreateCartWithItemAsync(article.Id, quantity: 2);

        await _client.PutAsJsonAsync($"/api/carts/{cartId}/items/{article.Id}", new SetCartItemQuantityRequest(0), TestJson.Options);

        var cart = await (await _client.GetAsync($"/api/carts/{cartId}")).Content.ReadFromJsonAsync<CartDto>(TestJson.Options);
        Assert.Empty(cart!.Items);
    }

    [Fact]
    public async Task Removing_an_item_takes_it_out_of_the_cart()
    {
        var article = await CreateArticleAsync(stock: 10);
        var cartId = await CreateCartWithItemAsync(article.Id, quantity: 2);

        await _client.DeleteAsync($"/api/carts/{cartId}/items/{article.Id}");

        var cart = await (await _client.GetAsync($"/api/carts/{cartId}")).Content.ReadFromJsonAsync<CartDto>(TestJson.Options);
        Assert.Empty(cart!.Items);
    }

    [Fact]
    public async Task Getting_an_unknown_cart_returns_404()
    {
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/carts/{Guid.NewGuid()}")).StatusCode);
    }

    // --- orders / checkout ---

    [Fact]
    public async Task Direct_order_creation_succeeds_decrements_stock_and_appears_in_admin_list()
    {
        var article = await CreateArticleAsync(price: 20m, stock: 10);
        var txHash = $"0x{Guid.NewGuid():N}{Guid.NewGuid():N}";

        var response = await _client.PostAsJsonAsync(
            "/api/orders",
            new CreateOrderRequest("0xwallet", txHash, [new CreateOrderItemRequest(article.Id, 2)]),
            TestJson.Options);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = await response.Content.ReadFromJsonAsync<OrderDto>(TestJson.Options);
        Assert.Equal(40m, order!.Total);

        // Stock decremented via HINCRBY inside the same transaction that wrote the order.
        var articleAfter = await (await _client.GetAsync($"/api/articles/{article.Id}")).Content.ReadFromJsonAsync<ArticleDto>(TestJson.Options);
        Assert.Equal(8, articleAfter!.Stock);

        var orders = await (await _client.SendAsync(Admin(HttpMethod.Get, "/api/orders"))).Content.ReadFromJsonAsync<List<OrderDto>>(TestJson.Options);
        Assert.Contains(orders!, o => o.Id == order.Id);
    }

    [Fact]
    public async Task Order_items_snapshot_the_article_name_and_price()
    {
        var article = await CreateArticleAsync("Snapshot Me", price: 15m, stock: 5);

        var order = await (await _client.PostAsJsonAsync(
            "/api/orders",
            new CreateOrderRequest("0xwallet", $"0x{Guid.NewGuid():N}", [new CreateOrderItemRequest(article.Id, 1)]),
            TestJson.Options)).Content.ReadFromJsonAsync<OrderDto>(TestJson.Options);

        // Renaming/repricing afterwards must not rewrite history on the placed order.
        await _client.SendAsync(Admin(HttpMethod.Put, $"/api/articles/{article.Id}",
            new UpsertArticleRequest("Renamed Later", "desc", 999m, article.CategoryId, 5)));

        var orders = await (await _client.SendAsync(Admin(HttpMethod.Get, "/api/orders"))).Content.ReadFromJsonAsync<List<OrderDto>>(TestJson.Options);
        var persisted = orders!.Single(o => o.Id == order!.Id);
        Assert.Equal("Snapshot Me", persisted.Items.Single().ArticleName);
        Assert.Equal(15m, persisted.Items.Single().UnitPrice);
    }

    [Fact]
    public async Task Orders_are_listed_newest_first()
    {
        var article = await CreateArticleAsync(stock: 10);
        var first = await (await _client.PostAsJsonAsync("/api/orders",
            new CreateOrderRequest("0xwallet", $"0x{Guid.NewGuid():N}", [new CreateOrderItemRequest(article.Id, 1)]), TestJson.Options))
            .Content.ReadFromJsonAsync<OrderDto>(TestJson.Options);
        var second = await (await _client.PostAsJsonAsync("/api/orders",
            new CreateOrderRequest("0xwallet", $"0x{Guid.NewGuid():N}", [new CreateOrderItemRequest(article.Id, 1)]), TestJson.Options))
            .Content.ReadFromJsonAsync<OrderDto>(TestJson.Options);

        var orders = await (await _client.SendAsync(Admin(HttpMethod.Get, "/api/orders"))).Content.ReadFromJsonAsync<List<OrderDto>>(TestJson.Options);

        var firstIndex = orders!.FindIndex(o => o.Id == first!.Id);
        var secondIndex = orders.FindIndex(o => o.Id == second!.Id);
        Assert.True(secondIndex < firstIndex, "the newer order should come first");
    }

    [Fact]
    public async Task Reusing_the_same_tx_hash_returns_409_on_the_second_attempt()
    {
        var article = await CreateArticleAsync(stock: 10);
        var request = new CreateOrderRequest("0xwallet", $"0x{Guid.NewGuid():N}{Guid.NewGuid():N}", [new CreateOrderItemRequest(article.Id, 1)]);

        var first = await _client.PostAsJsonAsync("/api/orders", request, TestJson.Options);
        var second = await _client.PostAsJsonAsync("/api/orders", request, TestJson.Options);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Pending_verification_returns_202_and_does_not_touch_stock()
    {
        _factory.PaymentVerification.Result = PaymentVerificationResult.Pending("not mined yet");
        var article = await CreateArticleAsync(stock: 5);

        var response = await _client.PostAsJsonAsync(
            "/api/orders",
            new CreateOrderRequest("0xwallet", $"0x{Guid.NewGuid():N}", [new CreateOrderItemRequest(article.Id, 1)]),
            TestJson.Options);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var articleAfter = await (await _client.GetAsync($"/api/articles/{article.Id}")).Content.ReadFromJsonAsync<ArticleDto>(TestJson.Options);
        Assert.Equal(5, articleAfter!.Stock);
    }

    [Fact]
    public async Task Failed_verification_returns_402()
    {
        _factory.PaymentVerification.Result = PaymentVerificationResult.Failed("wrong sender");
        var article = await CreateArticleAsync();

        var response = await _client.PostAsJsonAsync(
            "/api/orders",
            new CreateOrderRequest("0xwallet", $"0x{Guid.NewGuid():N}", [new CreateOrderItemRequest(article.Id, 1)]),
            TestJson.Options);

        Assert.Equal(HttpStatusCode.PaymentRequired, response.StatusCode);
    }

    [Fact]
    public async Task Insufficient_stock_returns_400()
    {
        var article = await CreateArticleAsync(stock: 1);

        var response = await _client.PostAsJsonAsync(
            "/api/orders",
            new CreateOrderRequest("0xwallet", $"0x{Guid.NewGuid():N}", [new CreateOrderItemRequest(article.Id, 5)]),
            TestJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Prepare_checkout_returns_the_token_contract_and_correctly_encoded_calldata()
    {
        var article = await CreateArticleAsync(price: 20m);
        var cartId = await CreateCartWithItemAsync(article.Id);

        var response = await _client.PostAsJsonAsync($"/api/carts/{cartId}/checkout/prepare", new PrepareCheckoutRequest("0xwallet"), TestJson.Options);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var pending = await response.Content.ReadFromJsonAsync<PendingPaymentDto>(TestJson.Options);
        Assert.Equal("0x1c7D4B196Cb0C7B01d743Fbc6116a902379C7238", pending!.To);
        Assert.Equal(20m, pending.Total);
        Assert.StartsWith("0xa9059cbb", pending.Data);
        Assert.EndsWith("1312d00", pending.Data);
    }

    [Fact]
    public async Task Full_cart_checkout_creates_an_order_and_clears_the_cart()
    {
        var article = await CreateArticleAsync(price: 20m, stock: 10);
        var cartId = await CreateCartWithItemAsync(article.Id, quantity: 2);

        var response = await _client.PostAsJsonAsync($"/api/carts/{cartId}/checkout", new CheckoutRequest("0xwallet", $"0x{Guid.NewGuid():N}"), TestJson.Options);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var order = await response.Content.ReadFromJsonAsync<OrderDto>(TestJson.Options);
        Assert.Equal(40m, order!.Total);

        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/carts/{cartId}")).StatusCode);
    }

    [Fact]
    public async Task Checkout_fails_when_stock_dropped_below_cart_quantity_and_the_cart_survives()
    {
        var article = await CreateArticleAsync(stock: 3);
        var cartId = await CreateCartWithItemAsync(article.Id, quantity: 2);

        await _client.SendAsync(Admin(HttpMethod.Put, $"/api/articles/{article.Id}",
            new UpsertArticleRequest(article.Name, article.Description, article.Price, article.CategoryId, 0)));

        var response = await _client.PostAsJsonAsync($"/api/carts/{cartId}/checkout", new CheckoutRequest("0xwallet", $"0x{Guid.NewGuid():N}"), TestJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync($"/api/carts/{cartId}")).StatusCode);
    }

    [Fact]
    public async Task Admin_endpoints_still_require_the_admin_key()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync("/api/orders")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized,
            (await _client.PostAsJsonAsync("/api/categories", new UpsertCategoryRequest("nope"), TestJson.Options)).StatusCode);
    }
}
