using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Shop.Api.Services;

namespace Shop.Api.Tests;

public class SepoliaTokenPaymentVerificationServiceTests
{
    private const string TokenContract = "0x1c7D4B196Cb0C7B01d743Fbc6116a902379C7238";
    private const string ReceivingWallet = "0x5e7d9291067925b49f3fbe6026894fb4300c1381";
    private const string Sender = "0xf192691256ad47ee5d4033d6979cdaea23180ad8";

    // Real receipt shape captured from Sepolia for a genuine 100 USDC transfer
    // (tx 0xf945e84a3f0e06cced1ead50b36d70571750f8a9561fab67fab4ec89e379b9df) during manual
    // testing, so these tests exercise the exact JSON shape the RPC actually returns.
    private const string TransferEventTopic = "0xddf252ad1be2c89b69c2b068fc378daa952ba7f163c4a11628f55a4df523b3ef";

    private static string BuildReceiptJson(string status, params (string Address, string From, string To, string DataHex)[] logs)
    {
        var logsJson = string.Join(",", logs.Select(l =>
            "{\"address\":\"" + l.Address + "\",\"topics\":[\"" + TransferEventTopic + "\",\"" +
            PadAddress(l.From) + "\",\"" + PadAddress(l.To) + "\"],\"data\":\"" + l.DataHex + "\"}"));

        return "{\"jsonrpc\":\"2.0\",\"id\":1,\"result\":{\"status\":\"" + status + "\",\"logs\":[" + logsJson + "]}}";
    }

    private static string PadAddress(string address) => "0x" + address[2..].PadLeft(64, '0');

    private static SepoliaTokenPaymentVerificationService CreateService(HttpMessageHandler handler, string? receivingWallet = ReceivingWallet)
    {
        var httpClient = new HttpClient(handler);
        var options = Options.Create(new PaymentOptions
        {
            RpcUrl = "https://fake-rpc.test",
            TokenContractAddress = TokenContract,
            TokenDecimals = 6,
            ReceivingWalletAddress = receivingWallet,
        });
        return new SepoliaTokenPaymentVerificationService(httpClient, options, NullLogger<SepoliaTokenPaymentVerificationService>.Instance);
    }

    [Fact]
    public async Task VerifyAsync_returns_verified_when_amount_matches_a_real_transfer_shape()
    {
        var receipt = BuildReceiptJson("0x1", (TokenContract, Sender, ReceivingWallet, "0x0000000000000000000000000000000000000000000000000000000005f5e100"));
        var service = CreateService(new FakeHttpMessageHandler(_ => JsonResponse(receipt)));

        var result = await service.VerifyAsync("0xf945e8...", Sender, 100m);

        Assert.Equal(PaymentVerificationStatus.Verified, result.Status);
    }

    [Fact]
    public async Task VerifyAsync_returns_verified_when_transferred_amount_exceeds_expected()
    {
        var receipt = BuildReceiptJson("0x1", (TokenContract, Sender, ReceivingWallet, "0x0000000000000000000000000000000000000000000000000000000005f5e100"));
        var service = CreateService(new FakeHttpMessageHandler(_ => JsonResponse(receipt)));

        var result = await service.VerifyAsync("0xf945e8...", Sender, 50m);

        Assert.Equal(PaymentVerificationStatus.Verified, result.Status);
    }

    [Fact]
    public async Task VerifyAsync_returns_pending_when_receipt_is_null_not_mined_yet()
    {
        var service = CreateService(new FakeHttpMessageHandler(_ => JsonResponse("""{"jsonrpc":"2.0","id":1,"result":null}""")));

        var result = await service.VerifyAsync("0xunmined", Sender, 100m);

        Assert.Equal(PaymentVerificationStatus.Pending, result.Status);
    }

    [Fact]
    public async Task VerifyAsync_returns_pending_when_rpc_is_unreachable()
    {
        var service = CreateService(new ThrowingHttpMessageHandler());

        var result = await service.VerifyAsync("0xanything", Sender, 100m);

        Assert.Equal(PaymentVerificationStatus.Pending, result.Status);
    }

    [Fact]
    public async Task VerifyAsync_returns_failed_when_transaction_reverted()
    {
        var receipt = BuildReceiptJson("0x0", (TokenContract, Sender, ReceivingWallet, "0x0000000000000000000000000000000000000000000000000000000005f5e100"));
        var service = CreateService(new FakeHttpMessageHandler(_ => JsonResponse(receipt)));

        var result = await service.VerifyAsync("0xreverted", Sender, 100m);

        Assert.Equal(PaymentVerificationStatus.Failed, result.Status);
    }

    [Fact]
    public async Task VerifyAsync_returns_failed_when_sender_does_not_match()
    {
        var receipt = BuildReceiptJson("0x1", (TokenContract, "0x000000000000000000000000000000000000dead", ReceivingWallet, "0x0000000000000000000000000000000000000000000000000000000005f5e100"));
        var service = CreateService(new FakeHttpMessageHandler(_ => JsonResponse(receipt)));

        var result = await service.VerifyAsync("0xwrongsender", Sender, 100m);

        Assert.Equal(PaymentVerificationStatus.Failed, result.Status);
    }

    [Fact]
    public async Task VerifyAsync_returns_failed_when_recipient_does_not_match_configured_wallet()
    {
        var receipt = BuildReceiptJson("0x1", (TokenContract, Sender, "0x000000000000000000000000000000000000dead", "0x0000000000000000000000000000000000000000000000000000000005f5e100"));
        var service = CreateService(new FakeHttpMessageHandler(_ => JsonResponse(receipt)));

        var result = await service.VerifyAsync("0xwrongrecipient", Sender, 100m);

        Assert.Equal(PaymentVerificationStatus.Failed, result.Status);
    }

    [Fact]
    public async Task VerifyAsync_returns_failed_when_amount_is_too_low()
    {
        // data encodes 100 USDC, but we ask to have received 101.
        var receipt = BuildReceiptJson("0x1", (TokenContract, Sender, ReceivingWallet, "0x0000000000000000000000000000000000000000000000000000000005f5e100"));
        var service = CreateService(new FakeHttpMessageHandler(_ => JsonResponse(receipt)));

        var result = await service.VerifyAsync("0xunderpaid", Sender, 101m);

        Assert.Equal(PaymentVerificationStatus.Failed, result.Status);
        Assert.Contains("too low", result.Error);
    }

    [Fact]
    public async Task VerifyAsync_returns_failed_when_no_log_matches_the_token_contract()
    {
        var receipt = BuildReceiptJson("0x1", ("0x000000000000000000000000000000000000dead", Sender, ReceivingWallet, "0x0000000000000000000000000000000000000000000000000000000005f5e100"));
        var service = CreateService(new FakeHttpMessageHandler(_ => JsonResponse(receipt)));

        var result = await service.VerifyAsync("0xwrongtoken", Sender, 100m);

        Assert.Equal(PaymentVerificationStatus.Failed, result.Status);
    }

    [Fact]
    public async Task VerifyAsync_returns_failed_when_receiving_wallet_is_not_configured()
    {
        var service = CreateService(new FakeHttpMessageHandler(_ => JsonResponse("""{"jsonrpc":"2.0","id":1,"result":null}""")), receivingWallet: null);

        var result = await service.VerifyAsync("0xanything", Sender, 100m);

        Assert.Equal(PaymentVerificationStatus.Failed, result.Status);
    }

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private sealed class FakeHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }

    private sealed class ThrowingHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new HttpRequestException("Simulated network failure.");
    }
}
