using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shop.Api.Contracts;
using Shop.Api.Data;
using Shop.Api.Models;
using Shop.Api.Services;

namespace Shop.Api.Controllers;

[ApiController]
[Route("api/carts")]
public class CartsController(ShopDbContext db, OrderService orderService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CartDto>> Create()
    {
        var cart = new Cart { Id = Guid.NewGuid() };
        db.Carts.Add(cart);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { cartId = cart.Id }, await BuildDto(cart));
    }

    [HttpGet("{cartId:guid}")]
    public async Task<ActionResult<CartDto>> Get(Guid cartId)
    {
        var cart = await db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.Id == cartId);
        return cart is null ? NotFound() : Ok(await BuildDto(cart));
    }

    [HttpPost("{cartId:guid}/items")]
    public async Task<ActionResult<CartDto>> AddItem(Guid cartId, AddCartItemRequest request)
    {
        if (request.Quantity <= 0)
        {
            return BadRequest("Quantity must be positive.");
        }

        var cart = await db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.Id == cartId);
        if (cart is null)
        {
            return NotFound();
        }

        var article = await db.Articles.FindAsync(request.ArticleId);
        if (article is null)
        {
            return BadRequest($"Article {request.ArticleId} not found.");
        }

        var existing = cart.Items.FirstOrDefault(i => i.ArticleId == request.ArticleId);
        var newQuantity = (existing?.Quantity ?? 0) + request.Quantity;
        if (newQuantity > article.Stock)
        {
            return BadRequest($"Insufficient stock for '{article.Name}': {article.Stock} available.");
        }

        if (existing is not null)
        {
            existing.Quantity = newQuantity;
        }
        else
        {
            // Adding to an already-tracked Cart's collection navigation isn't enough on its
            // own: EF sees the pre-assigned Guid key and infers Modified instead of Added.
            // Adding it to the DbSet directly forces Added state; EF's relationship fixup
            // then wires it into cart.Items automatically (adding it here too would double it).
            db.CartItems.Add(new CartItem { Id = Guid.NewGuid(), CartId = cart.Id, ArticleId = article.Id, Quantity = request.Quantity });
        }

        await db.SaveChangesAsync();
        return Ok(await BuildDto(cart));
    }

    [HttpPut("{cartId:guid}/items/{articleId:guid}")]
    public async Task<ActionResult<CartDto>> SetItemQuantity(Guid cartId, Guid articleId, SetCartItemQuantityRequest request)
    {
        var cart = await db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.Id == cartId);
        if (cart is null)
        {
            return NotFound();
        }

        var existing = cart.Items.FirstOrDefault(i => i.ArticleId == articleId);
        if (existing is null)
        {
            return NotFound();
        }

        if (request.Quantity <= 0)
        {
            db.CartItems.Remove(existing);
        }
        else
        {
            var article = await db.Articles.FindAsync(articleId);
            if (article is null)
            {
                return BadRequest($"Article {articleId} not found.");
            }

            if (request.Quantity > article.Stock)
            {
                return BadRequest($"Insufficient stock for '{article.Name}': {article.Stock} available.");
            }

            existing.Quantity = request.Quantity;
        }

        await db.SaveChangesAsync();
        return Ok(await BuildDto(cart));
    }

    [HttpDelete("{cartId:guid}/items/{articleId:guid}")]
    public async Task<ActionResult<CartDto>> RemoveItem(Guid cartId, Guid articleId)
    {
        var cart = await db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.Id == cartId);
        if (cart is null)
        {
            return NotFound();
        }

        var existing = cart.Items.FirstOrDefault(i => i.ArticleId == articleId);
        if (existing is null)
        {
            return NotFound();
        }

        db.CartItems.Remove(existing);
        await db.SaveChangesAsync();
        return Ok(await BuildDto(cart));
    }

    // Submits an already-sent crypto payment; verifies it on-chain and, only if valid,
    // turns the cart into an order and clears it.
    [HttpPost("{cartId:guid}/checkout")]
    public async Task<ActionResult<OrderDto>> Checkout(Guid cartId, CheckoutRequest request)
    {
        var cart = await db.Carts.Include(c => c.Items).FirstOrDefaultAsync(c => c.Id == cartId);
        if (cart is null)
        {
            return NotFound();
        }

        if (cart.Items.Count == 0)
        {
            return BadRequest("Cart is empty.");
        }

        var items = cart.Items.Select(i => new CreateOrderItem(i.ArticleId, i.Quantity)).ToList();
        var result = await orderService.CreateAsync(request.WalletAddress, request.TxHash, items);
        if (!result.Success)
        {
            return StatusCode(result.StatusCode, result.Error);
        }

        db.CartItems.RemoveRange(cart.Items);
        db.Carts.Remove(cart);
        await db.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, OrderDto.FromEntity(result.Order!));
    }

    private async Task<CartDto> BuildDto(Cart cart)
    {
        var articleIds = cart.Items.Select(i => i.ArticleId).ToList();
        var articles = await db.Articles.Where(a => articleIds.Contains(a.Id)).ToDictionaryAsync(a => a.Id);

        var items = cart.Items
            // an article may have been deleted by an admin since it was added to the cart
            .Where(i => articles.ContainsKey(i.ArticleId))
            .Select(i =>
            {
                var article = articles[i.ArticleId];
                return new CartItemDto(article.Id, article.Name, article.Price, i.Quantity, article.Price * i.Quantity);
            })
            .ToList();

        return new CartDto(cart.Id, items, items.Sum(i => i.LineTotal));
    }
}
