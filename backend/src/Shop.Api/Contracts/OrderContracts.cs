using Shop.Api.Models;

namespace Shop.Api.Contracts;

public record OrderItemDto(Guid ArticleId, string ArticleName, decimal UnitPrice, int Quantity)
{
    public static OrderItemDto FromEntity(OrderItem item) =>
        new(item.ArticleId, item.ArticleName, item.UnitPrice, item.Quantity);
}

public record OrderDto(Guid Id, string WalletAddress, string TxHash, decimal Total, DateTimeOffset CreatedAt, IEnumerable<OrderItemDto> Items)
{
    public static OrderDto FromEntity(Order order) =>
        new(order.Id, order.WalletAddress, order.TxHash, order.Total, order.CreatedAt, order.Items.Select(OrderItemDto.FromEntity));
}

public record CreateOrderItemRequest(Guid ArticleId, int Quantity);

public record CreateOrderRequest(string WalletAddress, string TxHash, List<CreateOrderItemRequest> Items);
