using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TreasuryFlow.Application.PaymentOrders.Processing;
using TreasuryFlow.Contracts.PaymentOrders;
using TreasuryFlow.Domain.Common.Exceptions;
using TreasuryFlow.Domain.PaymentOrders;
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
    IPaymentProcessor paymentProcessor,
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
            ?? throw new NonRetryableIntegrationEventException(
                $"Payment order '{integrationEvent.PaymentOrderId}' " +
                "was not found.");

        if (paymentOrder.Status !=
            PaymentOrderStatus.Processing)
        {
            try
            {
                paymentOrder.StartProcessing();
            }
            catch (DomainException exception)
            {
                throw new NonRetryableIntegrationEventException(
                    $"Payment order '{integrationEvent.PaymentOrderId}' " +
                    "cannot process the submitted integration event.",
                    exception);
            }

            await dbContext.SaveChangesAsync(
                cancellationToken);
        }

        var processingResult = await paymentProcessor.ProcessAsync(
            new PaymentProcessingRequest(
                integrationEvent.MessageId,
                paymentOrder.Id,
                paymentOrder.Amount.Value,
                paymentOrder.Amount.Currency,
                paymentOrder.Beneficiary),
            cancellationToken);

        if (processingResult.Outcome ==
            PaymentProcessingOutcome.Approved)
        {
            paymentOrder.Complete();
        }
        else
        {
            paymentOrder.Fail();
        }

        dbContext.InboxMessages.Add(
            new InboxMessage(
                integrationEvent.MessageId,
                typeof(PaymentOrderSubmittedIntegrationEvent)
                    .FullName!,
                timeProvider.GetUtcNow().UtcDateTime));

        await dbContext.SaveChangesAsync(
            cancellationToken);

        logger.LogInformation(
            "Integration event {MessageId} finished payment order " +
            "{PaymentOrderId} with status {PaymentOrderStatus}.",
            integrationEvent.MessageId,
            integrationEvent.PaymentOrderId,
            paymentOrder.Status);

        return IntegrationEventHandlingResult.Processed;
    }
}
