using Microsoft.Extensions.Options;
using TreasuryFlow.Application.PaymentOrders.Processing;

namespace TreasuryFlow.Infrastructure.PaymentProcessing;

public sealed class SimulatedPaymentProcessor(
    IOptions<PaymentProcessorOptions> options)
    : IPaymentProcessor
{
    private readonly PaymentProcessingOutcome _outcome =
        Enum.Parse<PaymentProcessingOutcome>(
            options.Value.SimulatedOutcome,
            ignoreCase: true);

    public Task<PaymentProcessingResult> ProcessAsync(
        PaymentProcessingRequest request,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            new PaymentProcessingResult(
                _outcome));
    }
}
