using Shop.Api.Models;
using Shop.Api.Repositories;

namespace Shop.Api.Services;

public record CreateOrderItem(Guid ArticleId, int Quantity);

public record OrderCreationResult(bool Success, bool IsPending, Order? Order, string? Error, int StatusCode)
{
    public static OrderCreationResult Ok(Order order) => new(true, false, order, null, StatusCodes.Status201Created);

    public static OrderCreationResult Pending(string reason) =>
        new(false, true, null, reason, StatusCodes.Status202Accepted);

    public static OrderCreationResult Fail(string error, int statusCode) => new(false, false, null, error, statusCode);
}

/// <summary>
/// Shared order-creation path used by both the direct order endpoint and cart checkout:
/// validates the requested items, checks stock, verifies the on-chain payment, then
/// decrements stock and persists the order — all before anything is considered final.
/// </summary>
public class OrderService(IArticleRepository articles, IOrderRepository orders, IPaymentVerificationService paymentVerification)
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

        if (await orders.TxHashExistsAsync(txHash, cancellationToken))
        {
            return OrderCreationResult.Fail("This transaction has already been used for an order.", StatusCodes.Status409Conflict);
        }

        var articleIds = items.Select(i => i.ArticleId).ToList();
        var resolved = await articles.GetByIdsAsync(articleIds, cancellationToken);

        var missingIds = articleIds.Except(resolved.Keys).ToList();
        if (missingIds.Count > 0)
        {
            return OrderCreationResult.Fail($"Article(s) not found: {string.Join(", ", missingIds)}.", StatusCodes.Status400BadRequest);
        }

        foreach (var item in items)
        {
            var article = resolved[item.ArticleId];
            if (article.Stock < item.Quantity)
            {
                return OrderCreationResult.Fail(
                    $"Insufficient stock for '{article.Name}': {article.Stock} available, {item.Quantity} requested.",
                    StatusCodes.Status400BadRequest);
            }
        }

        var total = items.Sum(i => resolved[i.ArticleId].Price * i.Quantity);

        var verification = await paymentVerification.VerifyAsync(txHash, walletAddress, total, cancellationToken);
        switch (verification.Status)
        {
            case PaymentVerificationStatus.Pending:
                // Not resolved yet, not rejected either — the caller should poll again rather
                // than treat this as a dead end. A freshly-broadcast transaction is essentially
                // never mined by the time eth_sendTransaction returns its hash.
                return OrderCreationResult.Pending(verification.Error!);
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

        var stockDecrements = new Dictionary<Guid, int>();
        foreach (var item in items)
        {
            var article = resolved[item.ArticleId];
            stockDecrements[article.Id] = stockDecrements.GetValueOrDefault(article.Id) + item.Quantity;

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

        await orders.CreateAsync(order, stockDecrements, cancellationToken);

        return OrderCreationResult.Ok(order);
    }
}
