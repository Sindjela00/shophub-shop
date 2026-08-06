using System.Net;
using System.Net.Http.Json;
using Shop.Api.Contracts;
using Shop.Api.Services;
using Xunit;

namespace Shop.Api.IntegrationTests;

[Collection(ShopApiCollection.Name)]
public class OrdersAndCheckoutEndpointTests
{
    private readonly HttpClient _client;
    private readonly ShopApiFactory _factory;

    public OrdersAndCheckoutEndpointTests(ShopApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
        // Fresh default before every test — this fake is a shared singleton across the collection.
        _factory.PaymentVerification.Result = PaymentVerificationResult.Verified();
    }

    private async Task<ArticleDto> CreateArticleAsync(decimal price = 20m, int stock = 10)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/articles")
        {
            Content = JsonContent.Create(new UpsertArticleRequest($"Order Test Item {Guid.NewGuid()}", "desc", price, "OrderTest", stock), options: TestJson.Options),
        };
        req.Headers.Add("X-Admin-Key", ShopApiFactory.AdminApiKey);
        var response = await _client.SendAsync(req);
        return (await response.Content.ReadFromJsonAsync<ArticleDto>(TestJson.Options))!;
    }

    private async Task<Guid> CreateCartWithItemAsync(Guid articleId, int quantity = 1)
    {
        var cartResponse = await _client.PostAsync("/api/carts", content: null);
        var cart = await cartResponse.Content.ReadFromJsonAsync<CartDto>(TestJson.Options);
        await _client.PostAsJsonAsync($"/api/carts/{cart!.Id}/items", new AddCartItemRequest(articleId, quantity), TestJson.Options);
        return cart.Id;
    }

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

        var articleAfter = await (await _client.GetAsync($"/api/articles/{article.Id}")).Content.ReadFromJsonAsync<ArticleDto>(TestJson.Options);
        Assert.Equal(8, articleAfter!.Stock);

        var listReq = new HttpRequestMessage(HttpMethod.Get, "/api/orders");
        listReq.Headers.Add("X-Admin-Key", ShopApiFactory.AdminApiKey);
        var orders = await (await _client.SendAsync(listReq)).Content.ReadFromJsonAsync<List<OrderDto>>(TestJson.Options);
        Assert.Contains(orders!, o => o.Id == order.Id);
    }

    [Fact]
    public async Task Listing_orders_without_admin_key_returns_401()
    {
        var response = await _client.GetAsync("/api/orders");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Reusing_the_same_tx_hash_returns_409_on_the_second_attempt()
    {
        var article = await CreateArticleAsync(stock: 10);
        var txHash = $"0x{Guid.NewGuid():N}{Guid.NewGuid():N}";
        var request = new CreateOrderRequest("0xwallet", txHash, [new CreateOrderItemRequest(article.Id, 1)]);

        var first = await _client.PostAsJsonAsync("/api/orders", request, TestJson.Options);
        var second = await _client.PostAsJsonAsync("/api/orders", request, TestJson.Options);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Pending_verification_returns_202_with_status_and_reason_not_an_error_body()
    {
        _factory.PaymentVerification.Result = PaymentVerificationResult.Pending("not mined yet");
        var article = await CreateArticleAsync();

        var response = await _client.PostAsJsonAsync(
            "/api/orders",
            new CreateOrderRequest("0xwallet", "0xpending", [new CreateOrderItemRequest(article.Id, 1)]),
            TestJson.Options);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PendingResponse>(TestJson.Options);
        Assert.Equal("pending", body!.Status);
        Assert.Equal("not mined yet", body.Reason);
    }

    [Fact]
    public async Task Pending_verification_does_not_touch_stock()
    {
        _factory.PaymentVerification.Result = PaymentVerificationResult.Pending("not mined yet");
        var article = await CreateArticleAsync(stock: 5);

        await _client.PostAsJsonAsync(
            "/api/orders",
            new CreateOrderRequest("0xwallet", "0xpending2", [new CreateOrderItemRequest(article.Id, 1)]),
            TestJson.Options);

        var articleAfter = await (await _client.GetAsync($"/api/articles/{article.Id}")).Content.ReadFromJsonAsync<ArticleDto>(TestJson.Options);
        Assert.Equal(5, articleAfter!.Stock);
    }

    [Fact]
    public async Task Failed_verification_returns_402_with_an_error_body()
    {
        _factory.PaymentVerification.Result = PaymentVerificationResult.Failed("wrong sender");
        var article = await CreateArticleAsync();

        var response = await _client.PostAsJsonAsync(
            "/api/orders",
            new CreateOrderRequest("0xwallet", "0xfailed", [new CreateOrderItemRequest(article.Id, 1)]),
            TestJson.Options);

        Assert.Equal(HttpStatusCode.PaymentRequired, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ErrorResponse>(TestJson.Options);
        Assert.Equal("wrong sender", body!.Error);
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
        // 20 USDC at 6 decimals = 20_000_000 = 0x1312d00, right-padded into the trailing word.
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

        var cartAfter = await _client.GetAsync($"/api/carts/{cartId}");
        Assert.Equal(HttpStatusCode.NotFound, cartAfter.StatusCode);
    }

    [Fact]
    public async Task Checkout_on_an_empty_cart_returns_400()
    {
        var cartResponse = await _client.PostAsync("/api/carts", content: null);
        var cart = await cartResponse.Content.ReadFromJsonAsync<CartDto>(TestJson.Options);

        var response = await _client.PostAsJsonAsync($"/api/carts/{cart!.Id}/checkout", new CheckoutRequest("0xwallet", "0xtx"), TestJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Checkout_on_a_nonexistent_cart_returns_404()
    {
        var response = await _client.PostAsJsonAsync($"/api/carts/{Guid.NewGuid()}/checkout", new CheckoutRequest("0xwallet", "0xtx"), TestJson.Options);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Checkout_fails_when_stock_dropped_below_cart_quantity_since_it_was_added()
    {
        var article = await CreateArticleAsync(stock: 3);
        var cartId = await CreateCartWithItemAsync(article.Id, quantity: 2);

        // Admin sells out the stock after the item was added to the cart.
        var updateReq = new HttpRequestMessage(HttpMethod.Put, $"/api/articles/{article.Id}")
        {
            Content = JsonContent.Create(new UpsertArticleRequest(article.Name, article.Description, article.Price, article.Category, 0), options: TestJson.Options),
        };
        updateReq.Headers.Add("X-Admin-Key", ShopApiFactory.AdminApiKey);
        await _client.SendAsync(updateReq);

        var response = await _client.PostAsJsonAsync($"/api/carts/{cartId}/checkout", new CheckoutRequest("0xwallet", $"0x{Guid.NewGuid():N}"), TestJson.Options);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var cartAfter = await _client.GetAsync($"/api/carts/{cartId}");
        Assert.Equal(HttpStatusCode.OK, cartAfter.StatusCode); // cart survives a failed checkout
    }
}
