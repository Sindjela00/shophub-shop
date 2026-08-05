using System.Numerics;

namespace Shop.Api.Services;

/// <summary>
/// Builds the calldata for an ERC-20 transfer(address,uint256) call, so the backend — not
/// the frontend — is the source of truth for what a customer needs to pay and to whom.
/// The frontend only ever needs to hand the result straight to eth_sendTransaction.
/// </summary>
public static class Erc20TransferEncoder
{
    // First 4 bytes of keccak256("transfer(address,uint256)") — the standard ERC-20 transfer selector.
    private const string TransferSelector = "a9059cbb";

    public static string EncodeTransferCallData(string recipientAddress, BigInteger amount)
    {
        var addressHex = recipientAddress[2..].ToLowerInvariant().PadLeft(64, '0');

        var amountHex = amount.ToString("x").TrimStart('0');
        if (amountHex.Length == 0)
        {
            amountHex = "0";
        }

        var amountPadded = amountHex.PadLeft(64, '0');

        return $"0x{TransferSelector}{addressHex}{amountPadded}";
    }
}
