namespace Shop.Api.Services;

public enum PaymentVerificationStatus
{
    /// <summary>The payment is confirmed on-chain and matches what was expected.</summary>
    Verified,

    /// <summary>
    /// Not resolved yet — the transaction isn't mined, or the RPC couldn't be reached. Worth
    /// retrying: a transaction sent via eth_sendTransaction is almost never mined by the time
    /// the caller gets the hash back, so callers should poll rather than treat this as final.
    /// </summary>
    Pending,

    /// <summary>
    /// Resolved and rejected — the transaction is mined but doesn't match (wrong sender,
    /// wrong recipient, insufficient amount, reverted). This can't change by retrying.
    /// </summary>
    Failed,
}

public record PaymentVerificationResult(PaymentVerificationStatus Status, string? Error)
{
    public static PaymentVerificationResult Verified() => new(PaymentVerificationStatus.Verified, null);
    public static PaymentVerificationResult Pending(string reason) => new(PaymentVerificationStatus.Pending, reason);
    public static PaymentVerificationResult Failed(string error) => new(PaymentVerificationStatus.Failed, error);
}

public interface IPaymentVerificationService
{
    /// <summary>
    /// Checks whether <paramref name="txHash"/> is a confirmed on-chain transfer of at least
    /// <paramref name="expectedAmount"/> of the configured payment token, from
    /// <paramref name="expectedFromAddress"/> to the configured receiving wallet. Safe to call
    /// repeatedly for the same transaction while it's still Pending.
    /// </summary>
    Task<PaymentVerificationResult> VerifyAsync(
        string txHash,
        string expectedFromAddress,
        decimal expectedAmount,
        CancellationToken cancellationToken = default);
}
