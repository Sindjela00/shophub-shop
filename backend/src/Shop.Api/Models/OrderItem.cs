namespace Shop.Api.Models;

/// <summary>
/// A line item on an order. Article name/price are snapshotted at purchase time so the
/// order stays accurate even if the article is later renamed, repriced, or deleted.
/// </summary>
public class OrderItem
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public required Guid ArticleId { get; set; }
    public required string ArticleName { get; set; }
    public required decimal UnitPrice { get; set; }
    public required int Quantity { get; set; }
}
