namespace Shop.Api.Services;

public record PaymentVerificationResult(bool Success, string? Error)
{
    public static PaymentVerificationResult Ok() => new(true, null);
    public static PaymentVerificationResult Fail(string error) => new(false, error);
}

public interface IPaymentVerificationService
{
    /// <summary>
    /// Verifies that <paramref name="txHash"/> is a confirmed on-chain transfer of at least
    /// <paramref name="expectedAmount"/> of the configured payment token, from
    /// <paramref name="expectedFromAddress"/> to the configured receiving wallet.
    /// </summary>
    Task<PaymentVerificationResult> VerifyAsync(
        string txHash,
        string expectedFromAddress,
        decimal expectedAmount,
        CancellationToken cancellationToken = default);
}
