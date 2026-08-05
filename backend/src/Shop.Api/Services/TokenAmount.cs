using System.Numerics;

namespace Shop.Api.Services;

/// <summary>
/// Shared decimal-to-smallest-unit scaling so the verification service (checking an amount
/// that was paid) and the checkout preparation endpoint (encoding an amount to be paid)
/// can never drift from each other.
/// </summary>
public static class TokenAmount
{
    public static BigInteger ToSmallestUnit(decimal amount, int decimals)
    {
        var multiplier = 1m;
        for (var i = 0; i < decimals; i++)
        {
            multiplier *= 10m;
        }

        return new BigInteger(amount * multiplier);
    }
}
