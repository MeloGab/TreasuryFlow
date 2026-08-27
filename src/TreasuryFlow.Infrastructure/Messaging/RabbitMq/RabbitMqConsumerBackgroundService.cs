using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TreasuryFlow.Contracts.PaymentOrders;

namespace TreasuryFlow.Infrastructure.Messaging.RabbitMq;

public sealed class RabbitMqConsumerBackgroundService(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqConsumerBackgroundService> logger)
    : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly RabbitMqOptions _options = options.Value;

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation(
                "RabbitMQ integration event consumer is disabled.");

            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeAsync(
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "RabbitMQ consumer stopped unexpectedly. " +
                    "A reconnection will be attempted.");

                await Task.Delay(
                    TimeSpan.FromSeconds(
                        _options.ConsumerRetryDelaySeconds),
                    stoppingToken);
            }
        }
    }

    private async Task ConsumeAsync(
        CancellationToken stoppingToken)
    {
        var connectionFactory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
            ClientProvidedName =
                "treasuryflow-payment-order-worker"
        };

        await using var connection =
            await connectionFactory.CreateConnectionAsync(
                stoppingToken);

        var channelOptions = new CreateChannelOptions(
            publisherConfirmationsEnabled: true,
            publisherConfirmationTrackingEnabled: true);

        await using var channel =
            await connection.CreateChannelAsync(
                channelOptions,
                stoppingToken);

        await DeclareTopologyAsync(
            channel,
            stoppingToken);

        await channel.BasicQosAsync(
            prefetchSize: 0,
            prefetchCount: 1,
            global: false,
            cancellationToken: stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(
            channel);

        consumer.ReceivedAsync += (_, eventArgs) =>
            HandleDeliveryAsync(
                channel,
                eventArgs,
                stoppingToken);

        await channel.BasicConsumeAsync(
            queue: _options.QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken);

        logger.LogInformation(
            "RabbitMQ consumer started for queue {QueueName}.",
            _options.QueueName);

        await Task.Delay(
            Timeout.InfiniteTimeSpan,
            stoppingToken);
    }

    private async Task HandleDeliveryAsync(
        IChannel channel,
        BasicDeliverEventArgs eventArgs,
        CancellationToken stoppingToken)
    {
        try
        {
            var integrationEvent = Deserialize(
                eventArgs);

            await using var scope =
                serviceScopeFactory.CreateAsyncScope();

            var handler = scope.ServiceProvider
                .GetRequiredService<
                    PaymentOrderSubmittedIntegrationEventHandler>();

            await handler.HandleAsync(
                integrationEvent,
                stoppingToken);

            await channel.BasicAckAsync(
                eventArgs.DeliveryTag,
                multiple: false,
                cancellationToken: stoppingToken);
        }
        catch (Exception exception)
            when (exception is JsonException or
                NonRetryableIntegrationEventException)
        {
            logger.LogError(
                exception,
                "RabbitMQ message {MessageId} cannot be processed and will " +
                "be moved to the failed queue.",
                eventArgs.BasicProperties.MessageId);

            await MoveToFailedQueueAsync(
                channel,
                eventArgs,
                stoppingToken);
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // The unacknowledged message is returned to the queue when
            // the channel closes during the graceful shutdown.
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Failed to process RabbitMQ message {MessageId}. " +
                "The message will be retried.",
                eventArgs.BasicProperties.MessageId);

            await Task.Delay(
                TimeSpan.FromSeconds(
                    _options.ConsumerRetryDelaySeconds),
                stoppingToken);

            await channel.BasicNackAsync(
                eventArgs.DeliveryTag,
                multiple: false,
                requeue: true,
                cancellationToken: stoppingToken);
        }
    }

    private PaymentOrderSubmittedIntegrationEvent Deserialize(
        BasicDeliverEventArgs eventArgs)
    {
        var expectedType =
            typeof(PaymentOrderSubmittedIntegrationEvent).FullName;

        if (!string.IsNullOrWhiteSpace(
                eventArgs.BasicProperties.Type) &&
            eventArgs.BasicProperties.Type != expectedType)
        {
            throw new JsonException(
                $"Integration event type " +
                $"'{eventArgs.BasicProperties.Type}' is not supported.");
        }

        var integrationEvent = JsonSerializer.Deserialize<
            PaymentOrderSubmittedIntegrationEvent>(
                eventArgs.Body.Span,
                JsonOptions)
            ?? throw new JsonException(
                "Integration event content is required.");

        if (integrationEvent.MessageId == Guid.Empty ||
            integrationEvent.PaymentOrderId == Guid.Empty)
        {
            throw new JsonException(
                "Integration event identifiers are required.");
        }

        return integrationEvent;
    }

    private async Task MoveToFailedQueueAsync(
        IChannel channel,
        BasicDeliverEventArgs eventArgs,
        CancellationToken cancellationToken)
    {
        var properties = new BasicProperties
        {
            ContentType = eventArgs.BasicProperties.ContentType ??
                "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            MessageId = eventArgs.BasicProperties.MessageId,
            Type = eventArgs.BasicProperties.Type
        };

        await channel.BasicPublishAsync(
            exchange: _options.FailedExchangeName,
            routingKey: _options.FailedRoutingKey,
            mandatory: true,
            basicProperties: properties,
            body: eventArgs.Body,
            cancellationToken: cancellationToken);

        await channel.BasicAckAsync(
            eventArgs.DeliveryTag,
            multiple: false,
            cancellationToken: cancellationToken);
    }

    private async Task DeclareTopologyAsync(
        IChannel channel,
        CancellationToken cancellationToken)
    {
        await channel.ExchangeDeclareAsync(
            exchange: _options.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: _options.QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            queue: _options.QueueName,
            exchange: _options.ExchangeName,
            routingKey: _options.SubmittedRoutingKey,
            arguments: null,
            cancellationToken: cancellationToken);

        await channel.ExchangeDeclareAsync(
            exchange: _options.FailedExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        await channel.QueueDeclareAsync(
            queue: _options.FailedQueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null,
            cancellationToken: cancellationToken);

        await channel.QueueBindAsync(
            queue: _options.FailedQueueName,
            exchange: _options.FailedExchangeName,
            routingKey: _options.FailedRoutingKey,
            arguments: null,
            cancellationToken: cancellationToken);
    }
}
