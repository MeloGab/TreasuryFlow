namespace TreasuryFlow.Infrastructure.Messaging.RabbitMq;

public sealed class NonRetryableIntegrationEventException
    : Exception
{
    public NonRetryableIntegrationEventException(
        string message)
        : base(message)
    {
    }

    public NonRetryableIntegrationEventException(
        string message,
        Exception innerException)
        : base(message, innerException)
    {
    }
}
