using TreasuryFlow.Domain.Common.Exceptions;

namespace TreasuryFlow.Domain.PaymentOrders.ValueObjects;

public sealed record Money
{
    private static readonly HashSet<string> SupportedCurrencies =
    [
        "BRL",
        "USD",
        "EUR"
    ];

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

        if (decimal.Round(value, 2) != value)
        {
            throw new DomainException(
                "Money value cannot have more than two decimal places.");
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new DomainException(
                "Money currency is required.");
        }

        var normalizedCurrency = currency
            .Trim()
            .ToUpperInvariant();

        if (normalizedCurrency.Length != 3 ||
            !normalizedCurrency.All(char.IsLetter))
        {
            throw new DomainException(
                "Money currency must be a valid three-letter code.");
        }

        if (!SupportedCurrencies.Contains(normalizedCurrency))
        {
            throw new DomainException(
                $"Currency '{normalizedCurrency}' is not supported.");
        }

        return new Money(
            value,
            normalizedCurrency);
    }
}