using System.Numerics;
using Shop.Api.Services;

namespace Shop.Api.Tests;

public class TokenAmountTests
{
    [Theory]
    [InlineData(20, 6, "20000000")]
    [InlineData(721.86, 6, "721860000")]
    [InlineData(0.01, 6, "10000")]
    [InlineData(100, 6, "100000000")]
    [InlineData(1, 0, "1")]
    [InlineData(1.5, 2, "150")]
    public void ToSmallestUnit_scales_decimal_amount_correctly(decimal amount, int decimals, string expected)
    {
        var result = TokenAmount.ToSmallestUnit(amount, decimals);

        Assert.Equal(BigInteger.Parse(expected), result);
    }

    [Fact]
    public void ToSmallestUnit_with_zero_decimals_returns_amount_unchanged()
    {
        Assert.Equal(new BigInteger(42), TokenAmount.ToSmallestUnit(42m, 0));
    }
}
