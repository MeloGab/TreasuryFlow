using TreasuryFlow.Domain.PaymentOrders;

namespace TreasuryFlow.Application.PaymentOrders.Queries.GetPaymentOrderById;

public sealed record GetPaymentOrderByIdResult(
    Guid Id,
    string Description,
    decimal Amount,
    string Currency,
    string Beneficiary,
    PaymentOrderStatus Status,
    DateTime CreatedAt,
    DateTime? ProcessedAt);
