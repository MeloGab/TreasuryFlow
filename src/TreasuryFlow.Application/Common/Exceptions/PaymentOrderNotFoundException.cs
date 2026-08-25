namespace TreasuryFlow.Application.Common.Exceptions;

public sealed class PaymentOrderNotFoundException(
    Guid paymentOrderId)
    : Exception(
        $"Payment order '{paymentOrderId}' was not found.")
{
    public Guid PaymentOrderId { get; } =
        paymentOrderId;
}
