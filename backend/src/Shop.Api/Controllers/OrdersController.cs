using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Api.Auth;
using Shop.Api.Contracts;
using Shop.Api.Data;
using Shop.Api.Models;

namespace Shop.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController(ShopDbContext db) : ControllerBase
{
    // Admin-only: review placed orders.
    [HttpGet]
    [ServiceFilter(typeof(AdminApiKeyFilter))]
    public async Task<ActionResult<IEnumerable<OrderDto>>> List()
    {
        var orders = await db.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
        return Ok(orders.Select(OrderDto.FromEntity));
    }

    // Called by the storefront once a crypto payment has succeeded.
    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create(CreateOrderRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.WalletAddress) || string.IsNullOrWhiteSpace(request.TxHash))
        {
            return BadRequest("WalletAddress and TxHash are required.");
        }

        if (request.Items is not { Count: > 0 } || request.Items.Any(i => i.Quantity <= 0))
        {
            return BadRequest("Order must contain at least one item with a positive quantity.");
        }

        var articleIds = request.Items.Select(i => i.ArticleId).ToList();
        var articles = await db.Articles.Where(a => articleIds.Contains(a.Id)).ToDictionaryAsync(a => a.Id);

        var missingIds = articleIds.Except(articles.Keys).ToList();
        if (missingIds.Count > 0)
        {
            return BadRequest($"Article(s) not found: {string.Join(", ", missingIds)}.");
        }

        foreach (var item in request.Items)
        {
            var article = articles[item.ArticleId];
            if (article.Stock < item.Quantity)
            {
                return BadRequest($"Insufficient stock for '{article.Name}': {article.Stock} available, {item.Quantity} requested.");
            }
        }

        var order = new Order
        {
            Id = Guid.NewGuid(),
            WalletAddress = request.WalletAddress,
            TxHash = request.TxHash,
            Total = 0,
        };

        foreach (var item in request.Items)
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

        order.Total = order.Items.Sum(i => i.UnitPrice * i.Quantity);

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        // No get-by-id endpoint is in scope yet, so there's no resource URI to point to.
        return StatusCode(StatusCodes.Status201Created, OrderDto.FromEntity(order));
    }
}
