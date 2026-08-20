using TreasuryFlow.Domain.Common.Exceptions;

namespace TreasuryFlow.Domain.PaymentOrders.ValueObjects;

public sealed record Money
{
    public decimal Value { get; }

    public string Currency { get; }

    private Money(
        decimal value,
        string currency)
    {
        Value = value;
        Currency = currency;
    }

    public static Money Create(
        decimal value,
        string currency)
    {
        if (value <= 0)
        {
            throw new DomainException(
                "Money value must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new DomainException(
                "Money currency is required.");
        }

        var normalizedCurrency = currency
            .Trim()
            .ToUpperInvariant();

        if (normalizedCurrency.Length != 3)
        {
            throw new DomainException(
                "Money currency must contain exactly three characters.");
        }

        return new Money(
            value,
            normalizedCurrency);
    }
}