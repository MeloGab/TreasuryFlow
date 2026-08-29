namespace TreasuryFlow.Application.PaymentOrders.Receipts;

public interface IPaymentReceiptStorage
{
    Task StoreAsync(
        PaymentReceipt receipt,
        CancellationToken cancellationToken = default);
}

public sealed record PaymentReceipt(
    Guid PaymentOrderId,
    string Description,
    decimal Amount,
    string Currency,
    string Beneficiary,
    DateTime ProcessedAt);
