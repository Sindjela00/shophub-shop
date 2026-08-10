namespace Shop.Api.Models;

/// <summary>
/// A line in a cart. Unlike OrderItem, this does not snapshot name/price — the cart is
/// pre-purchase and always reflects the article's current data.
/// </summary>
public class CartItem
{
    public Guid Id { get; set; }
    public Guid CartId { get; set; }
    public required Guid ArticleId { get; set; }
    public required int Quantity { get; set; }
}
