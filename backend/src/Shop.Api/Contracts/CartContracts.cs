namespace Shop.Api.Contracts;

public record CartItemDto(Guid ArticleId, string ArticleName, decimal UnitPrice, int Quantity, decimal LineTotal);

public record CartDto(Guid Id, List<CartItemDto> Items, decimal Total);

public record AddCartItemRequest(Guid ArticleId, int Quantity);

public record SetCartItemQuantityRequest(int Quantity);

public record CheckoutRequest(string WalletAddress, string TxHash);
