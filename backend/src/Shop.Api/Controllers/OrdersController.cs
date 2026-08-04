using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Api.Auth;
using Shop.Api.Contracts;
using Shop.Api.Data;
using Shop.Api.Services;

namespace Shop.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController(ShopDbContext db, OrderService orderService) : ControllerBase
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

    // Called once a crypto payment has been sent; verifies it on-chain before finalizing.
    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create(CreateOrderRequest request)
    {
        var items = request.Items.Select(i => new CreateOrderItem(i.ArticleId, i.Quantity)).ToList();
        var result = await orderService.CreateAsync(request.WalletAddress, request.TxHash, items);

        if (!result.Success)
        {
            return StatusCode(result.StatusCode, result.Error);
        }

        // No get-by-id endpoint is in scope yet, so there's no resource URI to point to.
        return StatusCode(StatusCodes.Status201Created, OrderDto.FromEntity(result.Order!));
    }
}
