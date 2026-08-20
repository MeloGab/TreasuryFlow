using TreasuryFlow.Domain.Common.Exceptions;
using TreasuryFlow.Domain.PaymentOrders.ValueObjects;

namespace TreasuryFlow.Domain.UnitTests.PaymentOrders.ValueObjects;

public class MoneyTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateMoney()
    {
        var money = Money.Create(
            value: 15000m,
            currency: "BRL");

        Assert.Equal(15000m, money.Value);
        Assert.Equal("BRL", money.Currency);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-1000)]
    public void Create_WithNonPositiveValue_ShouldThrowDomainException(
        decimal value)
    {
        var action = () => Money.Create(
            value,
            "BRL");

        Assert.Throws<DomainException>(action);
    }

    [Fact]
    public void Create_WithMoreThanTwoDecimalPlaces_ShouldThrowDomainException()
    {
        var action = () => Money.Create(
            10.123m,
            "BRL");

        Assert.Throws<DomainException>(action);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_WithoutCurrency_ShouldThrowDomainException(
        string? currency)
    {
        var action = () => Money.Create(
            15000m,
            currency!);

        Assert.Throws<DomainException>(action);
    }

    [Theory]
    [InlineData("B")]
    [InlineData("BR")]
    [InlineData("BRLL")]
    [InlineData("123")]
    [InlineData("@@@")]
    [InlineData("R$X")]
    public void Create_WithInvalidCurrencyFormat_ShouldThrowDomainException(
        string currency)
    {
        var action = () => Money.Create(
            15000m,
            currency);

        Assert.Throws<DomainException>(action);
    }

    [Fact]
    public void Create_WithUnsupportedCurrency_ShouldThrowDomainException()
    {
        var action = () => Money.Create(
            15000m,
            "JPY");

        Assert.Throws<DomainException>(action);
    }

    [Theory]
    [InlineData("brl")]
    [InlineData(" BRL ")]
    [InlineData(" brl ")]
    public void Create_ShouldNormalizeCurrency(
        string currency)
    {
        var money = Money.Create(
            15000m,
            currency);

        Assert.Equal("BRL", money.Currency);
    }

    [Fact]
    public void TwoMoneyInstances_WithSameValues_ShouldBeEqual()
    {
        var first = Money.Create(
            15000m,
            "BRL");

        var second = Money.Create(
            15000m,
            " brl ");

        Assert.Equal(first, second);
    }

    [Fact]
    public void TwoMoneyInstances_WithDifferentValues_ShouldNotBeEqual()
    {
        var first = Money.Create(
            15000m,
            "BRL");

        var second = Money.Create(
            16000m,
            "BRL");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void TwoMoneyInstances_WithDifferentCurrencies_ShouldNotBeEqual()
    {
        var first = Money.Create(
            15000m,
            "BRL");

        var second = Money.Create(
            15000m,
            "USD");

        Assert.NotEqual(first, second);
    }
}