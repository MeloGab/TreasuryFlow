namespace TreasuryFlow.Infrastructure.Messaging.RabbitMq;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    public bool Enabled { get; init; }

    public string HostName { get; init; } = "127.0.0.1";

    public int Port { get; init; } = 5672;

    public string UserName { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string ExchangeName { get; init; } =
        "treasuryflow.payment-orders";

    public string QueueName { get; init; } =
        "treasuryflow.payment-orders.processing";

    public string SubmittedRoutingKey { get; init; } =
        "payment-order.submitted";

    public int BatchSize { get; init; } = 20;

    public int PollingIntervalSeconds { get; init; } = 5;

    public int RetryDelaySeconds { get; init; } = 30;
}
