namespace Shop.Api.Models;

public class Order
{
    public Guid Id { get; set; }
    public required string WalletAddress { get; set; }
    public required string TxHash { get; set; }
    public required decimal Total { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<OrderItem> Items { get; set; } = [];
}
