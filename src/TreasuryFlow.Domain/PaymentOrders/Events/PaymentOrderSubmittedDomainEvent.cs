using TreasuryFlow.Domain.Common.Events;

namespace TreasuryFlow.Domain.PaymentOrders.Events;

public sealed record PaymentOrderSubmittedDomainEvent( Guid PaymentOrderId, decimal Amount, string Currency, DateTime OccurredAt) : IDomainEvent;