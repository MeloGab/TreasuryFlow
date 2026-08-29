namespace TreasuryFlow.Infrastructure.PaymentProcessing;

public sealed class PaymentProcessorOptions
{
    public const string SectionName = "PaymentProcessor";

    public string SimulatedOutcome { get; init; } = "Approved";
}
