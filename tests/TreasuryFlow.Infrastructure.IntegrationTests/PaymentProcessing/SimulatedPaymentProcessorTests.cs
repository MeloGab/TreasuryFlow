using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TreasuryFlow.Application.PaymentOrders.Processing;
using TreasuryFlow.Infrastructure.PaymentProcessing;

namespace TreasuryFlow.Infrastructure.IntegrationTests.PaymentProcessing;

public sealed class SimulatedPaymentProcessorTests
{
    [Theory]
    [InlineData("Approved", PaymentProcessingOutcome.Approved)]
    [InlineData("rejected", PaymentProcessingOutcome.Rejected)]
    public async Task ProcessAsync_ShouldReturnConfiguredOutcome(
        string configuredOutcome,
        PaymentProcessingOutcome expectedOutcome)
    {
        var options = Options.Create(
            new PaymentProcessorOptions
            {
                SimulatedOutcome = configuredOutcome
            });
        var processor = new SimulatedPaymentProcessor(options);

        var result = await processor.ProcessAsync(
            new PaymentProcessingRequest(
                Guid.NewGuid(),
                Guid.NewGuid(),
                100m,
                "BRL",
                "Beneficiary"));

        Assert.Equal(expectedOutcome, result.Outcome);
    }

    [Fact]
    public void AddWorkerInfrastructure_WithInvalidOutcome_ShouldRejectConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["PaymentProcessor:SimulatedOutcome"] = "Unknown"
                })
            .Build();
        var services = new ServiceCollection();

        services.AddWorkerInfrastructure(
            "Server=localhost;Database=TreasuryFlow;" +
            "Trusted_Connection=True;TrustServerCertificate=True",
            configuration);

        using var serviceProvider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(
            () => serviceProvider
                .GetRequiredService<IOptions<PaymentProcessorOptions>>()
                .Value);
    }
}
