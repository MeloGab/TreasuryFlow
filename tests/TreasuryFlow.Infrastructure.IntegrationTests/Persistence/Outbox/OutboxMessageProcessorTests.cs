using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TreasuryFlow.Contracts.PaymentOrders;
using TreasuryFlow.Domain.PaymentOrders.Events;
using TreasuryFlow.Infrastructure.Messaging;
using TreasuryFlow.Infrastructure.Messaging.RabbitMq;
using TreasuryFlow.Infrastructure.Persistence;
using TreasuryFlow.Infrastructure.Persistence.Outbox;

namespace TreasuryFlow.Infrastructure.IntegrationTests.Persistence.Outbox;

public sealed class OutboxMessageProcessorTests
{
    private static readonly DateTimeOffset CurrentTime =
        new(2026, 8, 25, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ProcessBatchAsync_WithPendingMessage_ShouldPublishAndMarkAsProcessed()
    {
        await using var connection = await CreateConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        var domainEvent = new PaymentOrderSubmittedDomainEvent(
            Guid.NewGuid(),
            1750.45m,
            "USD",
            CurrentTime.UtcDateTime);

        var outboxMessage = CreateOutboxMessage(
            domainEvent);

        dbContext.OutboxMessages.Add(
            outboxMessage);

        await dbContext.SaveChangesAsync();

        var publisher = new FakeIntegrationEventPublisher();
        var options = CreateOptions();

        var processor = CreateProcessor(
            dbContext,
            publisher,
            options);

        var processedCount = await processor.ProcessBatchAsync();

        Assert.Equal(1, processedCount);
        Assert.Equal(1, publisher.PublishCount);
        Assert.Equal(
            options.SubmittedRoutingKey,
            publisher.RoutingKey);
        Assert.Equal(
            outboxMessage.Id,
            publisher.MessageId);

        var integrationEvent = Assert.IsType<
            PaymentOrderSubmittedIntegrationEvent>(
                publisher.IntegrationEvent);

        Assert.Equal(outboxMessage.Id, integrationEvent.MessageId);
        Assert.Equal(domainEvent.PaymentOrderId, integrationEvent.PaymentOrderId);
        Assert.Equal(domainEvent.Amount, integrationEvent.Amount);
        Assert.Equal(domainEvent.Currency, integrationEvent.Currency);
        Assert.Equal(domainEvent.OccurredAt, integrationEvent.OccurredAt);
        Assert.Equal(CurrentTime.UtcDateTime, outboxMessage.ProcessedAt);
        Assert.Equal(0, outboxMessage.RetryCount);
        Assert.Null(outboxMessage.NextAttemptAt);
        Assert.Null(outboxMessage.Error);
    }

    [Fact]
    public async Task ProcessBatchAsync_WhenPublishFails_ShouldScheduleRetry()
    {
        await using var connection = await CreateConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        var domainEvent = new PaymentOrderSubmittedDomainEvent(
            Guid.NewGuid(),
            980.25m,
            "BRL",
            CurrentTime.UtcDateTime);

        var outboxMessage = CreateOutboxMessage(
            domainEvent);

        dbContext.OutboxMessages.Add(
            outboxMessage);

        await dbContext.SaveChangesAsync();

        var publisher = new FakeIntegrationEventPublisher(
            new InvalidOperationException(
                "RabbitMQ is unavailable."));

        var options = CreateOptions();

        var processor = CreateProcessor(
            dbContext,
            publisher,
            options);

        var processedCount = await processor.ProcessBatchAsync();

        Assert.Equal(1, processedCount);
        Assert.Equal(1, publisher.PublishCount);
        Assert.Null(outboxMessage.ProcessedAt);
        Assert.Equal(1, outboxMessage.RetryCount);
        Assert.Equal(
            CurrentTime.AddSeconds(
                    options.RetryDelaySeconds)
                .UtcDateTime,
            outboxMessage.NextAttemptAt);
        Assert.Contains(
            "RabbitMQ is unavailable.",
            outboxMessage.Error);
    }

    [Fact]
    public async Task ProcessBatchAsync_BeforeNextAttempt_ShouldSkipMessage()
    {
        await using var connection = await CreateConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        var domainEvent = new PaymentOrderSubmittedDomainEvent(
            Guid.NewGuid(),
            450.10m,
            "EUR",
            CurrentTime.UtcDateTime);

        var outboxMessage = CreateOutboxMessage(
            domainEvent);

        outboxMessage.MarkAsFailed(
            "Previous failure.",
            CurrentTime.AddMinutes(1).UtcDateTime);

        dbContext.OutboxMessages.Add(
            outboxMessage);

        await dbContext.SaveChangesAsync();

        var publisher = new FakeIntegrationEventPublisher();

        var processor = CreateProcessor(
            dbContext,
            publisher,
            CreateOptions());

        var processedCount = await processor.ProcessBatchAsync();

        Assert.Equal(0, processedCount);
        Assert.Equal(0, publisher.PublishCount);
        Assert.Null(outboxMessage.ProcessedAt);
        Assert.Equal(1, outboxMessage.RetryCount);
    }

    [Fact]
    public async Task ProcessBatchAsync_WithUnknownMessageType_ShouldRecordFailure()
    {
        await using var connection = await CreateConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        var outboxMessage = new OutboxMessage(
            Guid.NewGuid(),
            "Unknown.Event",
            "{}",
            CurrentTime.UtcDateTime);

        dbContext.OutboxMessages.Add(
            outboxMessage);

        await dbContext.SaveChangesAsync();

        var publisher = new FakeIntegrationEventPublisher();

        var processor = CreateProcessor(
            dbContext,
            publisher,
            CreateOptions());

        var processedCount = await processor.ProcessBatchAsync();

        Assert.Equal(1, processedCount);
        Assert.Equal(0, publisher.PublishCount);
        Assert.Equal(1, outboxMessage.RetryCount);
        Assert.Null(outboxMessage.ProcessedAt);
        Assert.Contains(
            "Unknown.Event",
            outboxMessage.Error);
    }

    private static OutboxMessageProcessor CreateProcessor(
        TreasuryFlowDbContext dbContext,
        IIntegrationEventPublisher publisher,
        RabbitMqOptions options)
    {
        return new OutboxMessageProcessor(
            dbContext,
            publisher,
            Options.Create(options),
            new FixedTimeProvider(CurrentTime),
            NullLogger<OutboxMessageProcessor>.Instance);
    }

    private static RabbitMqOptions CreateOptions()
    {
        return new RabbitMqOptions
        {
            BatchSize = 10,
            RetryDelaySeconds = 30,
            SubmittedRoutingKey = "payment-order.submitted"
        };
    }

    private static OutboxMessage CreateOutboxMessage(
        PaymentOrderSubmittedDomainEvent domainEvent)
    {
        return new OutboxMessage(
            Guid.NewGuid(),
            typeof(PaymentOrderSubmittedDomainEvent).FullName!,
            JsonSerializer.Serialize(domainEvent),
            domainEvent.OccurredAt);
    }

    private static async Task<SqliteConnection> CreateConnectionAsync()
    {
        var connection = new SqliteConnection(
            "Data Source=:memory:");

        await connection.OpenAsync();

        return connection;
    }

    private static async Task<TreasuryFlowDbContext> CreateDbContextAsync(
        SqliteConnection connection)
    {
        var options =
            new DbContextOptionsBuilder<TreasuryFlowDbContext>()
                .UseSqlite(connection)
                .Options;

        var dbContext = new TreasuryFlowDbContext(options);

        await dbContext.Database.EnsureCreatedAsync();

        return dbContext;
    }

    private sealed class FakeIntegrationEventPublisher(
        Exception? exception = null)
        : IIntegrationEventPublisher
    {
        public int PublishCount { get; private set; }

        public string? RoutingKey { get; private set; }

        public object? IntegrationEvent { get; private set; }

        public Guid MessageId { get; private set; }

        public Task PublishAsync(
            string routingKey,
            object integrationEvent,
            Guid messageId,
            CancellationToken cancellationToken = default)
        {
            PublishCount++;
            RoutingKey = routingKey;
            IntegrationEvent = integrationEvent;
            MessageId = messageId;

            return exception is null
                ? Task.CompletedTask
                : Task.FromException(exception);
        }
    }

    private sealed class FixedTimeProvider(
        DateTimeOffset utcNow)
        : TimeProvider
    {
        public override DateTimeOffset GetUtcNow()
        {
            return utcNow;
        }
    }
}
