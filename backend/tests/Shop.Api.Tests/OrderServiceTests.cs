using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shop.Api.Data;
using Shop.Api.Models;
using Shop.Api.Services;

namespace Shop.Api.Tests;

public class OrderServiceTests
{
    private static ShopDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ShopDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ShopDbContext(options);
    }

    private static Article NewArticle(string name = "Test Article", decimal price = 20m, int stock = 10) => new()
    {
        Id = Guid.NewGuid(),
        Name = name,
        Description = "desc",
        Price = price,
        Category = "Test",
        Stock = stock,
    };

    [Fact]
    public async Task CreateAsync_fails_when_wallet_address_missing()
    {
        await using var db = CreateContext();
        var service = new OrderService(db, new FakePaymentVerificationService(PaymentVerificationResult.Verified()));

        var result = await service.CreateAsync("", "0xtx", [new CreateOrderItem(Guid.NewGuid(), 1)]);

        Assert.False(result.Success);
        Assert.False(result.IsPending);
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_fails_when_items_empty()
    {
        await using var db = CreateContext();
        var service = new OrderService(db, new FakePaymentVerificationService(PaymentVerificationResult.Verified()));

        var result = await service.CreateAsync("0xwallet", "0xtx", []);

        Assert.False(result.Success);
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_fails_when_quantity_not_positive()
    {
        await using var db = CreateContext();
        var service = new OrderService(db, new FakePaymentVerificationService(PaymentVerificationResult.Verified()));

        var result = await service.CreateAsync("0xwallet", "0xtx", [new CreateOrderItem(Guid.NewGuid(), 0)]);

        Assert.False(result.Success);
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_fails_with_409_when_tx_hash_already_used()
    {
        await using var db = CreateContext();
        var article = NewArticle();
        db.Articles.Add(article);
        db.Orders.Add(new Order { Id = Guid.NewGuid(), WalletAddress = "0xsomeone", TxHash = "0xused", Total = 1 });
        await db.SaveChangesAsync();

        var service = new OrderService(db, new FakePaymentVerificationService(PaymentVerificationResult.Verified()));
        var result = await service.CreateAsync("0xwallet", "0xused", [new CreateOrderItem(article.Id, 1)]);

        Assert.False(result.Success);
        Assert.Equal(StatusCodes.Status409Conflict, result.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_fails_when_article_not_found()
    {
        await using var db = CreateContext();
        var service = new OrderService(db, new FakePaymentVerificationService(PaymentVerificationResult.Verified()));

        var result = await service.CreateAsync("0xwallet", "0xtx", [new CreateOrderItem(Guid.NewGuid(), 1)]);

        Assert.False(result.Success);
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        Assert.Contains("not found", result.Error);
    }

    [Fact]
    public async Task CreateAsync_fails_when_stock_insufficient()
    {
        await using var db = CreateContext();
        var article = NewArticle(stock: 2);
        db.Articles.Add(article);
        await db.SaveChangesAsync();

        var service = new OrderService(db, new FakePaymentVerificationService(PaymentVerificationResult.Verified()));
        var result = await service.CreateAsync("0xwallet", "0xtx", [new CreateOrderItem(article.Id, 5)]);

        Assert.False(result.Success);
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        Assert.Contains("Insufficient stock", result.Error);
    }

    [Fact]
    public async Task CreateAsync_does_not_call_payment_verification_when_stock_check_fails()
    {
        await using var db = CreateContext();
        var article = NewArticle(stock: 1);
        db.Articles.Add(article);
        await db.SaveChangesAsync();

        var fakeVerification = new FakePaymentVerificationService(PaymentVerificationResult.Verified());
        var service = new OrderService(db, fakeVerification);

        await service.CreateAsync("0xwallet", "0xtx", [new CreateOrderItem(article.Id, 5)]);

        Assert.Equal(0, fakeVerification.CallCount);
    }

    [Fact]
    public async Task CreateAsync_returns_202_when_payment_is_pending()
    {
        await using var db = CreateContext();
        var article = NewArticle();
        db.Articles.Add(article);
        await db.SaveChangesAsync();

        var service = new OrderService(db, new FakePaymentVerificationService(PaymentVerificationResult.Pending("not mined yet")));
        var result = await service.CreateAsync("0xwallet", "0xtx", [new CreateOrderItem(article.Id, 1)]);

        Assert.False(result.Success);
        Assert.True(result.IsPending);
        Assert.Equal(StatusCodes.Status202Accepted, result.StatusCode);
        Assert.Null(result.Order);
    }

    [Fact]
    public async Task CreateAsync_does_not_touch_stock_or_persist_anything_when_payment_is_pending()
    {
        await using var db = CreateContext();
        var article = NewArticle(stock: 5);
        db.Articles.Add(article);
        await db.SaveChangesAsync();

        var service = new OrderService(db, new FakePaymentVerificationService(PaymentVerificationResult.Pending("not mined yet")));
        await service.CreateAsync("0xwallet", "0xtx", [new CreateOrderItem(article.Id, 1)]);

        Assert.Equal(5, (await db.Articles.FindAsync(article.Id))!.Stock);
        Assert.Empty(db.Orders);
    }

    [Fact]
    public async Task CreateAsync_returns_402_when_payment_verification_fails()
    {
        await using var db = CreateContext();
        var article = NewArticle();
        db.Articles.Add(article);
        await db.SaveChangesAsync();

        var service = new OrderService(db, new FakePaymentVerificationService(PaymentVerificationResult.Failed("wrong sender")));
        var result = await service.CreateAsync("0xwallet", "0xtx", [new CreateOrderItem(article.Id, 1)]);

        Assert.False(result.Success);
        Assert.False(result.IsPending);
        Assert.Equal(StatusCodes.Status402PaymentRequired, result.StatusCode);
    }

    [Fact]
    public async Task CreateAsync_creates_order_decrements_stock_and_snapshots_article_data_when_verified()
    {
        await using var db = CreateContext();
        var article = NewArticle(name: "Aurora Windbreaker", price: 20m, stock: 10);
        db.Articles.Add(article);
        await db.SaveChangesAsync();

        var service = new OrderService(db, new FakePaymentVerificationService(PaymentVerificationResult.Verified()));
        var result = await service.CreateAsync("0xwallet", "0xtx", [new CreateOrderItem(article.Id, 3)]);

        Assert.True(result.Success);
        Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
        Assert.NotNull(result.Order);
        Assert.Equal(60m, result.Order!.Total);
        Assert.Single(result.Order.Items);
        Assert.Equal("Aurora Windbreaker", result.Order.Items[0].ArticleName);
        Assert.Equal(20m, result.Order.Items[0].UnitPrice);

        Assert.Equal(7, (await db.Articles.FindAsync(article.Id))!.Stock);
        Assert.Single(db.Orders);
    }

    [Fact]
    public async Task CreateAsync_computes_total_across_multiple_items()
    {
        await using var db = CreateContext();
        var a = NewArticle(price: 10m, stock: 5);
        var b = NewArticle(price: 7.5m, stock: 5);
        db.Articles.AddRange(a, b);
        await db.SaveChangesAsync();

        var service = new OrderService(db, new FakePaymentVerificationService(PaymentVerificationResult.Verified()));
        var result = await service.CreateAsync("0xwallet", "0xtx", [new CreateOrderItem(a.Id, 2), new CreateOrderItem(b.Id, 1)]);

        Assert.True(result.Success);
        Assert.Equal(27.5m, result.Order!.Total);
    }

    [Fact]
    public async Task CreateAsync_verifies_payment_using_the_computed_total_not_a_client_supplied_amount()
    {
        await using var db = CreateContext();
        var article = NewArticle(price: 20m, stock: 5);
        db.Articles.Add(article);
        await db.SaveChangesAsync();

        var fakeVerification = new FakePaymentVerificationService(PaymentVerificationResult.Verified());
        var service = new OrderService(db, fakeVerification);

        await service.CreateAsync("0xwallet", "0xtx", [new CreateOrderItem(article.Id, 2)]);

        Assert.Equal(40m, fakeVerification.LastExpectedAmount);
        Assert.Equal("0xwallet", fakeVerification.LastExpectedFromAddress);
    }

    private sealed class FakePaymentVerificationService(PaymentVerificationResult result) : IPaymentVerificationService
    {
        public int CallCount { get; private set; }
        public decimal LastExpectedAmount { get; private set; }
        public string? LastExpectedFromAddress { get; private set; }

        public Task<PaymentVerificationResult> VerifyAsync(string txHash, string expectedFromAddress, decimal expectedAmount, CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastExpectedAmount = expectedAmount;
            LastExpectedFromAddress = expectedFromAddress;
            return Task.FromResult(result);
        }
    }
}
