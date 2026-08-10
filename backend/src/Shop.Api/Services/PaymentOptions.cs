namespace Shop.Api.Services;

public class PaymentOptions
{
    public const string SectionName = "Payments";

    public required string RpcUrl { get; set; }
    public required string TokenContractAddress { get; set; }
    public int TokenDecimals { get; set; } = 6;

    /// <summary>
    /// The shop's payout wallet. Stands in for what would eventually come from the
    /// Shop CRD's wallet address (see the shop-operator repo) once ShopHub provisions it.
    /// </summary>
    public string? ReceivingWalletAddress { get; set; }
}
