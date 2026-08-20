using TreasuryFlow.Domain.Common.Exceptions;

namespace TreasuryFlow.Domain.PaymentOrders;

public class PaymentOrder
{
    public Guid Id { get; private set; }

    public string Description { get; private set; }

    public decimal Amount { get; private set; }

    public string Currency { get; private set; }

    public string Beneficiary { get; private set; }

    public PaymentOrderStatus Status { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? ProcessedAt { get; private set; }

    private PaymentOrder(
        string description,
        decimal amount,
        string currency,
        string beneficiary)
    {
        Id = Guid.NewGuid();
        Description = description;
        Amount = amount;
        Currency = currency;
        Beneficiary = beneficiary;
        Status = PaymentOrderStatus.Draft;
        CreatedAt = DateTime.UtcNow;
    }

    public static PaymentOrder Create(
        string description,
        decimal amount,
        string currency,
        string beneficiary)
    {
        if (amount <= 0)
        {
            throw new DomainException(
                "Payment order amount must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new DomainException(
                "Payment order description is required.");
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new DomainException(
                "Payment order currency is required.");
        }

        if (string.IsNullOrWhiteSpace(beneficiary))
        {
            throw new DomainException(
                "Payment order beneficiary is required.");
        }

        return new PaymentOrder(
            description,
            amount,
            currency,
            beneficiary);
    }
}