namespace TreasuryFlow.Api.Contracts.PaymentOrders;

public sealed record CreatePaymentOrderRequest(
    string Description,
    decimal Amount,
    string Currency,
    string Beneficiary);
