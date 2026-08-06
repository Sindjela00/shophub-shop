using System.Numerics;
using Shop.Api.Services;

namespace Shop.Api.Tests;

public class Erc20TransferEncoderTests
{
    // Expected value cross-checked two ways during manual testing (see PR #16/#17 history):
    // decoded byte-for-byte, and simulated via eth_call against the live Sepolia USDC
    // contract with a funded holder address, which returned ABI-encoded `true`.
    [Fact]
    public void EncodeTransferCallData_matches_known_good_encoding_verified_on_chain()
    {
        var data = Erc20TransferEncoder.EncodeTransferCallData(
            "0x0A283497D67bE06E0f7caB043621F7eFefFf7Cb0", new BigInteger(20_000_000));

        Assert.Equal(
            "0xa9059cbb0000000000000000000000000a283497d67be06e0f7cab043621f7efefff7cb00000000000000000000000000000000000000000000000000000000001312d00",
            data);
    }

    [Fact]
    public void EncodeTransferCallData_starts_with_the_erc20_transfer_selector()
    {
        var data = Erc20TransferEncoder.EncodeTransferCallData("0x0000000000000000000000000000000000dEaD", BigInteger.One);

        Assert.StartsWith("0xa9059cbb", data);
    }

    [Fact]
    public void EncodeTransferCallData_pads_address_to_32_bytes()
    {
        var data = Erc20TransferEncoder.EncodeTransferCallData("0x0000000000000000000000000000000000dEaD", BigInteger.Zero);

        // "0x" + 8 hex chars (selector) + 64 hex chars (address word) + 64 hex chars (amount word)
        Assert.Equal(2 + 8 + 64 + 64, data.Length);
        Assert.Equal("000000000000000000000000000000000000000000000000000000000000dead", data[10..74]);
    }

    [Fact]
    public void EncodeTransferCallData_encodes_zero_amount_as_all_zero_word()
    {
        var data = Erc20TransferEncoder.EncodeTransferCallData("0x0000000000000000000000000000000000dEaD", BigInteger.Zero);

        Assert.Equal(new string('0', 64), data[74..]);
    }
}
