namespace Shop.Api.Contracts;

public record CartItemDto(Guid ArticleId, string ArticleName, decimal UnitPrice, int Quantity, decimal LineTotal);

public record CartDto(Guid Id, List<CartItemDto> Items, decimal Total);

public record AddCartItemRequest(Guid ArticleId, int Quantity);

public record SetCartItemQuantityRequest(int Quantity);

public record CheckoutRequest(string WalletAddress, string TxHash);

public record PrepareCheckoutRequest(string WalletAddress);

/// <summary>
/// An unsigned ERC-20 transfer call, ready to hand straight to eth_sendTransaction —
/// the frontend never needs to know the token contract or shop wallet address itself.
/// </summary>
public record PendingPaymentDto(string To, string Data, string Value, decimal Total);
