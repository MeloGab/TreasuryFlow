using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TreasuryFlow.Contracts.PaymentOrders;
using TreasuryFlow.Infrastructure.Persistence;
using TreasuryFlow.Infrastructure.Persistence.Inbox;

namespace TreasuryFlow.Infrastructure.Messaging.RabbitMq;

public enum IntegrationEventHandlingResult
{
    Processed,
    AlreadyProcessed
}

public sealed class PaymentOrderSubmittedIntegrationEventHandler(
    TreasuryFlowDbContext dbContext,
    TimeProvider timeProvider,
    ILogger<PaymentOrderSubmittedIntegrationEventHandler> logger)
{
    public async Task<IntegrationEventHandlingResult> HandleAsync(
        PaymentOrderSubmittedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        var wasAlreadyProcessed = await dbContext.InboxMessages
            .AnyAsync(
                message => message.Id == integrationEvent.MessageId,
                cancellationToken);

        if (wasAlreadyProcessed)
        {
            logger.LogInformation(
                "Integration event {MessageId} was already processed.",
                integrationEvent.MessageId);

            return IntegrationEventHandlingResult.AlreadyProcessed;
        }

        var paymentOrder = await dbContext.PaymentOrders
            .SingleOrDefaultAsync(
                order =>
                    order.Id == integrationEvent.PaymentOrderId,
                cancellationToken)
            ?? throw new InvalidOperationException(
                $"Payment order '{integrationEvent.PaymentOrderId}' " +
                "was not found.");

        paymentOrder.StartProcessing();

        dbContext.InboxMessages.Add(
            new InboxMessage(
                integrationEvent.MessageId,
                typeof(PaymentOrderSubmittedIntegrationEvent)
                    .FullName!,
                timeProvider.GetUtcNow().UtcDateTime));

        await dbContext.SaveChangesAsync(
            cancellationToken);

        logger.LogInformation(
            "Integration event {MessageId} moved payment order " +
            "{PaymentOrderId} to processing.",
            integrationEvent.MessageId,
            integrationEvent.PaymentOrderId);

        return IntegrationEventHandlingResult.Processed;
    }
}
