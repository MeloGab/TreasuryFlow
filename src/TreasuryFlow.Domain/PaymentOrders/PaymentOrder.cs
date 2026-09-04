using TreasuryFlow.Domain.Common.Events;
using TreasuryFlow.Domain.Common.Exceptions;
using TreasuryFlow.Domain.PaymentOrders.Events;
using TreasuryFlow.Domain.PaymentOrders.ValueObjects;

namespace TreasuryFlow.Domain.PaymentOrders;

public class PaymentOrder
{
    public const int MaxDescriptionLength = 200;

    public const int MaxBeneficiaryLength = 150;

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

    private PaymentOrder()
    {
        Description = null!;
        Amount = null!;
        Beneficiary = null!;
    }

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
        var details = ValidateDetails(
            description,
            amount,
            currency,
            beneficiary);

        return new PaymentOrder(
            details.Description,
            details.Amount,
            details.Beneficiary);
    }

    public void UpdateDetails(
        string description,
        decimal amount,
        string currency,
        string beneficiary)
    {
        if (Status != PaymentOrderStatus.Draft)
        {
            throw new DomainException(
                "Only draft payment orders can be updated.");
        }

        var details = ValidateDetails(
            description,
            amount,
            currency,
            beneficiary);

        Description = details.Description;
        Amount = details.Amount;
        Beneficiary = details.Beneficiary;
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

    private static (
        string Description,
        Money Amount,
        string Beneficiary) ValidateDetails(
        string description,
        decimal amount,
        string currency,
        string beneficiary)
    {
        var normalizedDescription = NormalizeRequiredText(
            description,
            "description",
            MaxDescriptionLength);

        var normalizedBeneficiary = NormalizeRequiredText(
            beneficiary,
            "beneficiary",
            MaxBeneficiaryLength);

        var money = Money.Create(
            amount,
            currency);

        return (
            normalizedDescription,
            money,
            normalizedBeneficiary);
    }

    private static string NormalizeRequiredText(
        string value,
        string fieldName,
        int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(
                $"Payment order {fieldName} is required.");
        }

        var normalizedValue = value.Trim();

        if (normalizedValue.Length > maxLength)
        {
            throw new DomainException(
                $"Payment order {fieldName} cannot exceed {maxLength} characters.");
        }

        return normalizedValue;
    }

    private void AddDomainEvent(
        IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
}