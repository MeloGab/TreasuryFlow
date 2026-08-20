using TreasuryFlow.Domain.Common.Exceptions;
using TreasuryFlow.Domain.PaymentOrders.ValueObjects;

namespace TreasuryFlow.Domain.PaymentOrders;

public class PaymentOrder
{
    public Guid Id { get; private set; }

    public string Description { get; private set; }

    public Money Amount { get; private set; }

    public string Beneficiary { get; private set; }

    public PaymentOrderStatus Status { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? ProcessedAt { get; private set; }

    private PaymentOrder(
        string description,
        Money amount,
        string beneficiary)
    {
        Id = Guid.NewGuid();
        Description = description;
        Amount = amount;
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
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new DomainException(
                "Payment order description is required.");
        }

        if (string.IsNullOrWhiteSpace(beneficiary))
        {
            throw new DomainException(
                "Payment order beneficiary is required.");
        }

        var money = Money.Create(
            amount,
            currency);

        return new PaymentOrder(
            description,
            money,
            beneficiary);
    }
}