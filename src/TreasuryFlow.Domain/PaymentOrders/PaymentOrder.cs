using TreasuryFlow.Domain.Common.Events;
using TreasuryFlow.Domain.Common.Exceptions;
using TreasuryFlow.Domain.PaymentOrders.Events;
using TreasuryFlow.Domain.PaymentOrders.ValueObjects;

namespace TreasuryFlow.Domain.PaymentOrders;

public class PaymentOrder
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public Guid Id { get; private set; }

    public string Description { get; private set; }

    public Money Amount { get; private set; }

    public string Beneficiary { get; private set; }

    public PaymentOrderStatus Status { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public DateTime? ProcessedAt { get; private set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents =>
        _domainEvents.AsReadOnly();

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

    public void Submit()
    {
        if (Status != PaymentOrderStatus.Draft)
        {
            throw new DomainException(
                "Only draft payment orders can be submitted.");
        }

        Status = PaymentOrderStatus.Pending;

        AddDomainEvent(
            new PaymentOrderSubmittedDomainEvent(
                Id,
                Amount.Value,
                Amount.Currency,
                DateTime.UtcNow));
    }

    public void StartProcessing()
    {
        if (Status != PaymentOrderStatus.Pending)
        {
            throw new DomainException(
                "Only pending payment orders can start processing.");
        }

        Status = PaymentOrderStatus.Processing;
    }

    public void Complete()
    {
        if (Status != PaymentOrderStatus.Processing)
        {
            throw new DomainException(
                "Only processing payment orders can be completed.");
        }

        Status = PaymentOrderStatus.Completed;
        ProcessedAt = DateTime.UtcNow;
    }

    public void Fail()
    {
        if (Status != PaymentOrderStatus.Processing)
        {
            throw new DomainException(
                "Only processing payment orders can fail.");
        }

        Status = PaymentOrderStatus.Failed;
        ProcessedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        if (Status != PaymentOrderStatus.Draft &&
            Status != PaymentOrderStatus.Pending)
        {
            throw new DomainException(
                "Only draft or pending payment orders can be cancelled.");
        }

        Status = PaymentOrderStatus.Cancelled;
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    private void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
}