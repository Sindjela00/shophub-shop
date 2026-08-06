using Shop.Api.Services;

namespace Shop.Api.IntegrationTests;

/// <summary>
/// Swaps out the real Sepolia RPC calls in integration tests — those are already covered by
/// SepoliaTokenPaymentVerificationService's own unit tests, and hitting a live chain from CI
/// would be slow, flaky, and non-deterministic. Defaults to Verified; tests that need a
/// specific outcome (Pending/Failed) set Result before making the request.
/// </summary>
public class FakePaymentVerificationService : IPaymentVerificationService
{
    public PaymentVerificationResult Result { get; set; } = PaymentVerificationResult.Verified();

    public Task<PaymentVerificationResult> VerifyAsync(
        string txHash, string expectedFromAddress, decimal expectedAmount, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result);
}
