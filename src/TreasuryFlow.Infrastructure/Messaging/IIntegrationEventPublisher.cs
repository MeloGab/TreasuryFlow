namespace TreasuryFlow.Infrastructure.Messaging;

public interface IIntegrationEventPublisher
{
    Task PublishAsync(
        string routingKey,
        object integrationEvent,
        Guid messageId,
        CancellationToken cancellationToken = default);
}
