namespace TreasuryFlow.Api.Contracts.PaymentOrders;

public sealed record UpdatePaymentOrderRequest(
    string Description,
    decimal Amount,
    string Currency,
    string Beneficiary);
