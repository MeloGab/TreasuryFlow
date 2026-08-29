namespace TreasuryFlow.Application.PaymentOrders.Processing;

public interface IPaymentProcessor
{
    Task<PaymentProcessingResult> ProcessAsync(
        PaymentProcessingRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record PaymentProcessingRequest(
    Guid IdempotencyKey,
    Guid PaymentOrderId,
    decimal Amount,
    string Currency,
    string Beneficiary);

public sealed record PaymentProcessingResult(
    PaymentProcessingOutcome Outcome);

public enum PaymentProcessingOutcome
{
    Approved,
    Rejected
}
