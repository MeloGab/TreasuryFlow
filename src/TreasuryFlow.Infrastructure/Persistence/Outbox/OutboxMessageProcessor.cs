using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TreasuryFlow.Contracts.PaymentOrders;
using TreasuryFlow.Domain.PaymentOrders.Events;
using TreasuryFlow.Infrastructure.Messaging;
using TreasuryFlow.Infrastructure.Messaging.RabbitMq;

namespace TreasuryFlow.Infrastructure.Persistence.Outbox;

public sealed class OutboxMessageProcessor(
    TreasuryFlowDbContext dbContext,
    IIntegrationEventPublisher integrationEventPublisher,
    IOptions<RabbitMqOptions> options,
    TimeProvider timeProvider,
    ILogger<OutboxMessageProcessor> logger)
{
    private const int MaximumErrorLength = 4000;
    private readonly RabbitMqOptions _options = options.Value;

    public async Task<int> ProcessBatchAsync(
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        var messages = await dbContext.OutboxMessages
            .Where(message =>
                message.ProcessedAt == null &&
                (message.NextAttemptAt == null ||
                    message.NextAttemptAt <= now))
            .OrderBy(message => message.OccurredAt)
            .Take(_options.BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            try
            {
                await PublishAsync(
                    message,
                    cancellationToken);

                message.MarkAsProcessed(
                    timeProvider.GetUtcNow().UtcDateTime);
            }
            catch (Exception exception)
                when (exception is not OperationCanceledException)
            {
                var error = exception.ToString();

                if (error.Length > MaximumErrorLength)
                {
                    error = error[..MaximumErrorLength];
                }

                var nextAttemptAt = timeProvider
                    .GetUtcNow()
                    .AddSeconds(_options.RetryDelaySeconds)
                    .UtcDateTime;

                message.MarkAsFailed(
                    error,
                    nextAttemptAt);

                logger.LogWarning(
                    exception,
                    "Failed to publish outbox message {MessageId}. " +
                    "Retry {RetryCount} is scheduled for {NextAttemptAt}.",
                    message.Id,
                    message.RetryCount,
                    message.NextAttemptAt);
            }

            await dbContext.SaveChangesAsync(
                cancellationToken);
        }

        return messages.Count;
    }

    private async Task PublishAsync(
        OutboxMessage message,
        CancellationToken cancellationToken)
    {
        if (message.Type !=
            typeof(PaymentOrderSubmittedDomainEvent).FullName)
        {
            throw new NotSupportedException(
                $"Outbox message type '{message.Type}' is not supported.");
        }

        var domainEvent = JsonSerializer.Deserialize<
            PaymentOrderSubmittedDomainEvent>(
                message.Content)
            ?? throw new InvalidOperationException(
                $"Outbox message '{message.Id}' has invalid content.");

        var integrationEvent =
            new PaymentOrderSubmittedIntegrationEvent(
                message.Id,
                domainEvent.PaymentOrderId,
                domainEvent.Amount,
                domainEvent.Currency,
                domainEvent.OccurredAt);

        await integrationEventPublisher.PublishAsync(
            _options.SubmittedRoutingKey,
            integrationEvent,
            message.Id,
            cancellationToken);
    }
}
