namespace TreasuryFlow.Api.Contracts.PaymentOrders;

public sealed record GetPaymentOrderByIdResponse(
    Guid Id,
    string Description,
    decimal Amount,
    string Currency,
    string Beneficiary,
    string Status,
    DateTime CreatedAt,
    DateTime? ProcessedAt);
