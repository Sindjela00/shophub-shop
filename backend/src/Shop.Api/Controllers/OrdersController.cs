using Microsoft.AspNetCore.Mvc;
using Shop.Api.Auth;
using Shop.Api.Contracts;
using Shop.Api.Repositories;
using Shop.Api.Services;

namespace Shop.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController(IOrderRepository orders, OrderService orderService) : ControllerBase
{
    // Admin-only: review placed orders.
    [HttpGet]
    [ServiceFilter(typeof(AdminApiKeyFilter))]
    public async Task<ActionResult<IEnumerable<OrderDto>>> List()
    {
        var result = await orders.ListAsync();
        return Ok(result.Select(OrderDto.FromEntity));
    }

    // Called once a crypto payment has been sent; verifies it on-chain before finalizing.
    [HttpPost]
    public async Task<ActionResult<OrderDto>> Create(CreateOrderRequest request)
    {
        var items = request.Items.Select(i => new CreateOrderItem(i.ArticleId, i.Quantity)).ToList();
        var result = await orderService.CreateAsync(request.WalletAddress, request.TxHash, items);

        if (!result.Success)
        {
            return result.IsPending
                ? StatusCode(result.StatusCode, new PendingResponse(result.Error!))
                : StatusCode(result.StatusCode, new ErrorResponse(result.Error!));
        }

        // No get-by-id endpoint is in scope yet, so there's no resource URI to point to.
        return StatusCode(StatusCodes.Status201Created, OrderDto.FromEntity(result.Order!));
    }
}
