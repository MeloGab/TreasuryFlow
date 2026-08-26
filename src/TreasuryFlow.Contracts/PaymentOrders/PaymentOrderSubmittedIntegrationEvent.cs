namespace TreasuryFlow.Contracts.PaymentOrders;

public sealed record PaymentOrderSubmittedIntegrationEvent(
    Guid MessageId,
    Guid PaymentOrderId,
    decimal Amount,
    string Currency,
    DateTime OccurredAt);
