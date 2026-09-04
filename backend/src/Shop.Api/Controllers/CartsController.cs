using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Shop.Api.Contracts;
using Shop.Api.Repositories;
using Shop.Api.Services;

namespace Shop.Api.Controllers;

[ApiController]
[Route("api/carts")]
public class CartsController(
    ICartRepository carts,
    IArticleRepository articles,
    OrderService orderService,
    IOptions<PaymentOptions> paymentOptions) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CartDto>> Create()
    {
        var cart = await carts.CreateAsync();
        return CreatedAtAction(nameof(Get), new { cartId = cart.Id }, await BuildDto(cart.Id, cart.Items));
    }

    [HttpGet("{cartId:guid}")]
    public async Task<ActionResult<CartDto>> Get(Guid cartId)
    {
        var cart = await carts.GetAsync(cartId);
        return cart is null ? NotFound() : Ok(await BuildDto(cart.Id, cart.Items));
    }

    [HttpPost("{cartId:guid}/items")]
    public async Task<ActionResult<CartDto>> AddItem(Guid cartId, AddCartItemRequest request)
    {
        if (request.Quantity <= 0)
        {
            return BadRequest(new ErrorResponse("Quantity must be positive."));
        }

        var cart = await carts.GetAsync(cartId);
        if (cart is null)
        {
            return NotFound();
        }

        var article = await articles.GetByIdAsync(request.ArticleId);
        if (article is null)
        {
            return BadRequest(new ErrorResponse($"Article {request.ArticleId} not found."));
        }

        var existing = cart.Items.FirstOrDefault(i => i.ArticleId == request.ArticleId);
        var newQuantity = (existing?.Quantity ?? 0) + request.Quantity;
        if (newQuantity > article.Stock)
        {
            return BadRequest(new ErrorResponse($"Insufficient stock for '{article.Name}': {article.Stock} available."));
        }

        await carts.AddItemAsync(cartId, request.ArticleId, request.Quantity);
        var updated = await carts.GetAsync(cartId);
        return Ok(await BuildDto(cartId, updated!.Items));
    }

    [HttpPut("{cartId:guid}/items/{articleId:guid}")]
    public async Task<ActionResult<CartDto>> SetItemQuantity(Guid cartId, Guid articleId, SetCartItemQuantityRequest request)
    {
        var cart = await carts.GetAsync(cartId);
        if (cart is null)
        {
            return NotFound();
        }

        var existing = cart.Items.FirstOrDefault(i => i.ArticleId == articleId);
        if (existing is null)
        {
            return NotFound();
        }

        if (request.Quantity > 0)
        {
            var article = await articles.GetByIdAsync(articleId);
            if (article is null)
            {
                return BadRequest(new ErrorResponse($"Article {articleId} not found."));
            }

            if (request.Quantity > article.Stock)
            {
                return BadRequest(new ErrorResponse($"Insufficient stock for '{article.Name}': {article.Stock} available."));
            }
        }

        await carts.SetItemQuantityAsync(cartId, articleId, request.Quantity);
        var updated = await carts.GetAsync(cartId);
        return Ok(await BuildDto(cartId, updated!.Items));
    }

    [HttpDelete("{cartId:guid}/items/{articleId:guid}")]
    public async Task<ActionResult<CartDto>> RemoveItem(Guid cartId, Guid articleId)
    {
        var cart = await carts.GetAsync(cartId);
        if (cart is null)
        {
            return NotFound();
        }

        var existing = cart.Items.FirstOrDefault(i => i.ArticleId == articleId);
        if (existing is null)
        {
            return NotFound();
        }

        await carts.RemoveItemAsync(cartId, articleId);
        var updated = await carts.GetAsync(cartId);
        return Ok(await BuildDto(cartId, updated!.Items));
    }

    // Builds the unsigned ERC-20 transfer the customer needs to sign: the backend is the
    // source of truth for the amount/token/recipient, so the frontend just hands this
    // straight to eth_sendTransaction rather than constructing it itself.
    [HttpPost("{cartId:guid}/checkout/prepare")]
    public async Task<ActionResult<PendingPaymentDto>> PrepareCheckout(Guid cartId, PrepareCheckoutRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.WalletAddress))
        {
            return BadRequest(new ErrorResponse("WalletAddress is required."));
        }

        var cart = await carts.GetAsync(cartId);
        if (cart is null)
        {
            return NotFound();
        }

        if (cart.Items.Count == 0)
        {
            return BadRequest(new ErrorResponse("Cart is empty."));
        }

        var articleIds = cart.Items.Select(i => i.ArticleId).ToList();
        var resolved = await articles.GetByIdsAsync(articleIds);

        var missingIds = articleIds.Except(resolved.Keys).ToList();
        if (missingIds.Count > 0)
        {
            return BadRequest(new ErrorResponse($"Article(s) not found: {string.Join(", ", missingIds)}."));
        }

        foreach (var item in cart.Items)
        {
            var article = resolved[item.ArticleId];
            if (article.Stock < item.Quantity)
            {
                return BadRequest(new ErrorResponse($"Insufficient stock for '{article.Name}': {article.Stock} available, {item.Quantity} requested."));
            }
        }

        var opts = paymentOptions.Value;
        if (string.IsNullOrWhiteSpace(opts.ReceivingWalletAddress))
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse("Payment receiving wallet is not configured."));
        }

        var total = cart.Items.Sum(i => resolved[i.ArticleId].Price * i.Quantity);
        var amountSmallestUnit = TokenAmount.ToSmallestUnit(total, opts.TokenDecimals);
        var data = Erc20TransferEncoder.EncodeTransferCallData(opts.ReceivingWalletAddress, amountSmallestUnit);

        // Stock isn't reserved here — it's re-checked authoritatively when /checkout is
        // called with the resulting txHash, same as it would be for a stale prepared cart.
        return Ok(new PendingPaymentDto(To: opts.TokenContractAddress, Data: data, Value: "0x0", Total: total));
    }

    // Submits an already-sent crypto payment; verifies it on-chain and, only if valid,
    // turns the cart into an order and clears it.
    [HttpPost("{cartId:guid}/checkout")]
    public async Task<ActionResult<OrderDto>> Checkout(Guid cartId, CheckoutRequest request)
    {
        var cart = await carts.GetAsync(cartId);
        if (cart is null)
        {
            return NotFound();
        }

        if (cart.Items.Count == 0)
        {
            return BadRequest(new ErrorResponse("Cart is empty."));
        }

        var items = cart.Items.Select(i => new CreateOrderItem(i.ArticleId, i.Quantity)).ToList();
        var result = await orderService.CreateAsync(request.WalletAddress, request.TxHash, items);
        if (!result.Success)
        {
            return result.IsPending
                ? StatusCode(result.StatusCode, new PendingResponse(result.Error!))
                : StatusCode(result.StatusCode, new ErrorResponse(result.Error!));
        }

        await carts.DeleteAsync(cartId);

        return StatusCode(StatusCodes.Status201Created, OrderDto.FromEntity(result.Order!));
    }

    private async Task<CartDto> BuildDto(Guid cartId, List<Models.CartItem> items)
    {
        var articleIds = items.Select(i => i.ArticleId).ToList();
        var resolved = await articles.GetByIdsAsync(articleIds);

        var dtoItems = items
            // an article may have been deleted by an admin since it was added to the cart
            .Where(i => resolved.ContainsKey(i.ArticleId))
            .Select(i =>
            {
                var article = resolved[i.ArticleId];
                return new CartItemDto(article.Id, article.Name, article.Price, i.Quantity, article.Price * i.Quantity);
            })
            .ToList();

        return new CartDto(cartId, dtoItems, dtoItems.Sum(i => i.LineTotal));
    }
}
