using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace TreasuryFlow.Infrastructure.Messaging.RabbitMq;

public sealed class RabbitMqIntegrationEventPublisher(
    IOptions<RabbitMqOptions> options)
    : IIntegrationEventPublisher,
        IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    private readonly RabbitMqOptions _options = options.Value;
    private readonly SemaphoreSlim _initializationLock = new(1, 1);
    private readonly SemaphoreSlim _publishingLock = new(1, 1);

    private IConnection? _connection;
    private IChannel? _channel;

    public async Task PublishAsync(
        string routingKey,
        object integrationEvent,
        Guid messageId,
        CancellationToken cancellationToken = default)
    {
        await _publishingLock.WaitAsync(
            cancellationToken);

        try
        {
            var channel = await GetChannelAsync(
                cancellationToken);

            var body = JsonSerializer.SerializeToUtf8Bytes(
                integrationEvent,
                integrationEvent.GetType(),
                JsonOptions);

            var properties = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent,
                MessageId = messageId.ToString(),
                Type = integrationEvent.GetType().FullName
            };

            await channel.BasicPublishAsync(
                exchange: _options.ExchangeName,
                routingKey: routingKey,
                mandatory: true,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken);
        }
        finally
        {
            _publishingLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            await _channel.DisposeAsync();
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        _initializationLock.Dispose();
        _publishingLock.Dispose();
    }

    private async Task<IChannel> GetChannelAsync(
        CancellationToken cancellationToken)
    {
        if (_channel is { IsOpen: true })
        {
            return _channel;
        }

        await _initializationLock.WaitAsync(
            cancellationToken);

        try
        {
            if (_channel is { IsOpen: true })
            {
                return _channel;
            }

            await DisposeConnectionResourcesAsync();

            var connectionFactory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
                ClientProvidedName =
                    "treasuryflow-outbox-publisher"
            };

            _connection =
                await connectionFactory.CreateConnectionAsync(
                    cancellationToken);

            var channelOptions = new CreateChannelOptions(
                publisherConfirmationsEnabled: true,
                publisherConfirmationTrackingEnabled: true);

            _channel = await _connection.CreateChannelAsync(
                channelOptions,
                cancellationToken);

            await DeclareTopologyAsync(
                _channel,
                cancellationToken);

            return _channel;
        }
        finally
        {
            _initializationLock.Release();
        }
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
    }

    private async Task DisposeConnectionResourcesAsync()
    {
        if (_channel is not null)
        {
            await _channel.DisposeAsync();
            _channel = null;
        }

        if (_connection is not null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
    }
}
