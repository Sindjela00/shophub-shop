using System.Net.Http.Json;
using System.Numerics;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Shop.Api.Services;

/// <summary>
/// Verifies ERC-20 token payments on Sepolia by reading the transaction receipt directly
/// from a JSON-RPC endpoint and checking for a matching Transfer event log. No wallet
/// SDK/private key is involved — this only ever reads public chain state.
/// </summary>
public class SepoliaTokenPaymentVerificationService(
    HttpClient httpClient,
    IOptions<PaymentOptions> options,
    ILogger<SepoliaTokenPaymentVerificationService> logger) : IPaymentVerificationService
{
    // keccak256("Transfer(address,address,uint256)") — the standard ERC-20 Transfer event topic.
    private const string TransferEventTopic = "0xddf252ad1be2c89b69c2b068fc378daa952ba7f163c4a11628f55a4df523b3ef";

    public async Task<PaymentVerificationResult> VerifyAsync(
        string txHash,
        string expectedFromAddress,
        decimal expectedAmount,
        CancellationToken cancellationToken = default)
    {
        var opts = options.Value;
        if (string.IsNullOrWhiteSpace(opts.ReceivingWalletAddress))
        {
            return PaymentVerificationResult.Fail("Payment receiving wallet is not configured.");
        }

        JsonElement receipt;
        try
        {
            receipt = await CallRpcAsync(opts.RpcUrl, "eth_getTransactionReceipt", [txHash], cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch transaction receipt for {TxHash}", txHash);
            return PaymentVerificationResult.Fail("Could not reach the blockchain RPC to verify the transaction.");
        }

        if (receipt.ValueKind != JsonValueKind.Object)
        {
            return PaymentVerificationResult.Fail("Transaction not found (it may not be mined yet).");
        }

        if (receipt.GetProperty("status").GetString() != "0x1")
        {
            return PaymentVerificationResult.Fail("Transaction failed on-chain.");
        }

        var expectedSmallestUnit = ToSmallestUnit(expectedAmount, opts.TokenDecimals);

        foreach (var log in receipt.GetProperty("logs").EnumerateArray())
        {
            if (!string.Equals(log.GetProperty("address").GetString(), opts.TokenContractAddress, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var topics = log.GetProperty("topics").EnumerateArray().Select(t => t.GetString()!).ToList();
            if (topics.Count != 3 || topics[0] != TransferEventTopic)
            {
                continue;
            }

            var from = AddressFromTopic(topics[1]);
            var to = AddressFromTopic(topics[2]);

            if (!string.Equals(from, expectedFromAddress, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!string.Equals(to, opts.ReceivingWalletAddress, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var amount = ParseUint256(log.GetProperty("data").GetString()!);
            if (amount >= expectedSmallestUnit)
            {
                return PaymentVerificationResult.Ok();
            }

            return PaymentVerificationResult.Fail(
                $"Payment amount too low: transferred {amount}, expected at least {expectedSmallestUnit} (smallest token unit).");
        }

        return PaymentVerificationResult.Fail(
            "No matching token transfer found in this transaction (wrong token, sender, or recipient).");
    }

    private async Task<JsonElement> CallRpcAsync(string rpcUrl, string method, object?[] rpcParams, CancellationToken cancellationToken)
    {
        var payload = new { jsonrpc = "2.0", id = 1, method, @params = rpcParams };
        using var response = await httpClient.PostAsJsonAsync(rpcUrl, payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);
        if (doc.RootElement.TryGetProperty("error", out var error))
        {
            throw new InvalidOperationException($"RPC error calling {method}: {error}");
        }

        return doc.RootElement.TryGetProperty("result", out var result) ? result.Clone() : default;
    }

    private static string AddressFromTopic(string topic) => "0x" + topic[2..][^40..];

    private static BigInteger ParseUint256(string hexData) =>
        new(Convert.FromHexString(hexData[2..]), isUnsigned: true, isBigEndian: true);

    private static BigInteger ToSmallestUnit(decimal amount, int decimals)
    {
        var multiplier = 1m;
        for (var i = 0; i < decimals; i++)
        {
            multiplier *= 10m;
        }

        return new BigInteger(amount * multiplier);
    }
}
