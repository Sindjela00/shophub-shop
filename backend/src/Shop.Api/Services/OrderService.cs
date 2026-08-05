using Microsoft.EntityFrameworkCore;
using Shop.Api.Data;
using Shop.Api.Models;

namespace Shop.Api.Services;

public record CreateOrderItem(Guid ArticleId, int Quantity);

public record OrderCreationResult(bool Success, Order? Order, string? Error, int StatusCode)
{
    public static OrderCreationResult Ok(Order order) => new(true, order, null, StatusCodes.Status201Created);
    public static OrderCreationResult Fail(string error, int statusCode) => new(false, null, error, statusCode);
}

/// <summary>
/// Shared order-creation path used by both the direct order endpoint and cart checkout:
/// validates the requested items, checks stock, verifies the on-chain payment, then
/// decrements stock and persists the order — all before anything is considered final.
/// </summary>
public class OrderService(ShopDbContext db, IPaymentVerificationService paymentVerification)
{
    public async Task<OrderCreationResult> CreateAsync(
        string walletAddress,
        string txHash,
        List<CreateOrderItem> items,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(walletAddress) || string.IsNullOrWhiteSpace(txHash))
        {
            return OrderCreationResult.Fail("WalletAddress and TxHash are required.", StatusCodes.Status400BadRequest);
        }

        if (items is not { Count: > 0 } || items.Any(i => i.Quantity <= 0))
        {
            return OrderCreationResult.Fail("Order must contain at least one item with a positive quantity.", StatusCodes.Status400BadRequest);
        }

        if (await db.Orders.AnyAsync(o => o.TxHash == txHash, cancellationToken))
        {
            return OrderCreationResult.Fail("This transaction has already been used for an order.", StatusCodes.Status409Conflict);
        }

        var articleIds = items.Select(i => i.ArticleId).ToList();
        var articles = await db.Articles.Where(a => articleIds.Contains(a.Id)).ToDictionaryAsync(a => a.Id, cancellationToken);

        var missingIds = articleIds.Except(articles.Keys).ToList();
        if (missingIds.Count > 0)
        {
            return OrderCreationResult.Fail($"Article(s) not found: {string.Join(", ", missingIds)}.", StatusCodes.Status400BadRequest);
        }

        foreach (var item in items)
        {
            var article = articles[item.ArticleId];
            if (article.Stock < item.Quantity)
            {
                return OrderCreationResult.Fail(
                    $"Insufficient stock for '{article.Name}': {article.Stock} available, {item.Quantity} requested.",
                    StatusCodes.Status400BadRequest);
            }
        }

        var total = items.Sum(i => articles[i.ArticleId].Price * i.Quantity);

        var verification = await paymentVerification.VerifyAsync(txHash, walletAddress, total, cancellationToken);
        switch (verification.Status)
        {
            case PaymentVerificationStatus.Pending:
                // Not resolved yet, not rejected either — the caller should poll again rather
                // than treat this as a dead end. A freshly-broadcast transaction is essentially
                // never mined by the time eth_sendTransaction returns its hash.
                return OrderCreationResult.Fail(verification.Error!, StatusCodes.Status202Accepted);
            case PaymentVerificationStatus.Failed:
                return OrderCreationResult.Fail(verification.Error!, StatusCodes.Status402PaymentRequired);
        }

        var order = new Order
        {
            Id = Guid.NewGuid(),
            WalletAddress = walletAddress,
            TxHash = txHash,
            Total = total,
        };

        foreach (var item in items)
        {
            var article = articles[item.ArticleId];
            article.Stock -= item.Quantity;

            order.Items.Add(new OrderItem
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                ArticleId = article.Id,
                ArticleName = article.Name,
                UnitPrice = article.Price,
                Quantity = item.Quantity,
            });
        }

        db.Orders.Add(order);
        await db.SaveChangesAsync(cancellationToken);

        return OrderCreationResult.Ok(order);
    }
}
