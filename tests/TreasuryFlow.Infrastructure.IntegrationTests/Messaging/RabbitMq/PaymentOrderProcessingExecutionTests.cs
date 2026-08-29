using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TreasuryFlow.Application.PaymentOrders.Processing;
using TreasuryFlow.Contracts.PaymentOrders;
using TreasuryFlow.Domain.PaymentOrders;
using TreasuryFlow.Infrastructure.Messaging.RabbitMq;
using TreasuryFlow.Infrastructure.Persistence;

namespace TreasuryFlow.Infrastructure.IntegrationTests.Messaging.RabbitMq;

public sealed class PaymentOrderProcessingExecutionTests
{
    private static readonly DateTimeOffset InboxProcessedAt =
        new(2026, 8, 28, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task HandleAsync_WhenProcessorRejects_ShouldFailAndPersistInbox()
    {
        await using var connection = await CreateOpenConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        var paymentOrder = CreatePendingPaymentOrder();
        dbContext.PaymentOrders.Add(paymentOrder);
        await dbContext.SaveChangesAsync();

        var integrationEvent = CreateIntegrationEvent(paymentOrder.Id);
        var processor = new StubPaymentProcessor(
            PaymentProcessingOutcome.Rejected);
        var handler = CreateHandler(dbContext, processor);

        var result = await handler.HandleAsync(integrationEvent);

        dbContext.ChangeTracker.Clear();

        var persistedPaymentOrder =
            await dbContext.PaymentOrders.SingleAsync();

        Assert.Equal(IntegrationEventHandlingResult.Processed, result);
        Assert.Equal(PaymentOrderStatus.Failed, persistedPaymentOrder.Status);
        Assert.NotNull(persistedPaymentOrder.ProcessedAt);
        Assert.Equal(1, await dbContext.InboxMessages.CountAsync());
    }

    [Fact]
    public async Task HandleAsync_WhenProcessorIsUnavailable_ShouldLeaveProcessingWithoutInbox()
    {
        await using var connection = await CreateOpenConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        var paymentOrder = CreatePendingPaymentOrder();
        dbContext.PaymentOrders.Add(paymentOrder);
        await dbContext.SaveChangesAsync();

        var integrationEvent = CreateIntegrationEvent(paymentOrder.Id);
        var processor = new StubPaymentProcessor(
            exception: new InvalidOperationException(
                "Processor unavailable."));
        var handler = CreateHandler(dbContext, processor);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(integrationEvent));

        dbContext.ChangeTracker.Clear();

        var persistedPaymentOrder =
            await dbContext.PaymentOrders.SingleAsync();
        var request = Assert.Single(processor.Requests);

        Assert.Equal(PaymentOrderStatus.Processing, persistedPaymentOrder.Status);
        Assert.Null(persistedPaymentOrder.ProcessedAt);
        Assert.Empty(await dbContext.InboxMessages.ToListAsync());
        Assert.Equal(integrationEvent.MessageId, request.IdempotencyKey);
        Assert.Equal(paymentOrder.Id, request.PaymentOrderId);
        Assert.Equal(paymentOrder.Amount.Value, request.Amount);
        Assert.Equal(paymentOrder.Amount.Currency, request.Currency);
        Assert.Equal(paymentOrder.Beneficiary, request.Beneficiary);
    }

    [Fact]
    public async Task HandleAsync_WhenRedeliveredDuringProcessing_ShouldResumeAndComplete()
    {
        await using var connection = await CreateOpenConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        var paymentOrder = CreatePendingPaymentOrder();
        paymentOrder.StartProcessing();
        dbContext.PaymentOrders.Add(paymentOrder);
        await dbContext.SaveChangesAsync();

        var integrationEvent = CreateIntegrationEvent(paymentOrder.Id);
        var processor = new StubPaymentProcessor();
        var handler = CreateHandler(dbContext, processor);

        var result = await handler.HandleAsync(integrationEvent);

        dbContext.ChangeTracker.Clear();

        var persistedPaymentOrder =
            await dbContext.PaymentOrders.SingleAsync();

        Assert.Equal(IntegrationEventHandlingResult.Processed, result);
        Assert.Equal(PaymentOrderStatus.Completed, persistedPaymentOrder.Status);
        Assert.NotNull(persistedPaymentOrder.ProcessedAt);
        Assert.Single(processor.Requests);
        Assert.Equal(1, await dbContext.InboxMessages.CountAsync());
    }

    private static PaymentOrderSubmittedIntegrationEventHandler CreateHandler(
        TreasuryFlowDbContext dbContext,
        IPaymentProcessor processor)
    {
        return new PaymentOrderSubmittedIntegrationEventHandler(
            dbContext,
            processor,
            new FixedTimeProvider(InboxProcessedAt),
            NullLogger<PaymentOrderSubmittedIntegrationEventHandler>.Instance);
    }

    private static PaymentOrder CreatePendingPaymentOrder()
    {
        var paymentOrder = PaymentOrder.Create(
            "Payment order",
            125.50m,
            "BRL",
            "Beneficiary");

        paymentOrder.Submit();

        return paymentOrder;
    }

    private static PaymentOrderSubmittedIntegrationEvent CreateIntegrationEvent(
        Guid paymentOrderId)
    {
        return new PaymentOrderSubmittedIntegrationEvent(
            Guid.NewGuid(),
            paymentOrderId,
            125.50m,
            "BRL",
            InboxProcessedAt.UtcDateTime);
    }

    private static async Task<SqliteConnection> CreateOpenConnectionAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        return connection;
    }

    private static async Task<TreasuryFlowDbContext> CreateDbContextAsync(
        SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<TreasuryFlowDbContext>()
            .UseSqlite(connection)
            .Options;

        var dbContext = new TreasuryFlowDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();
        return dbContext;
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow)
        : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class StubPaymentProcessor(
        PaymentProcessingOutcome outcome = PaymentProcessingOutcome.Approved,
        Exception? exception = null)
        : IPaymentProcessor
    {
        public List<PaymentProcessingRequest> Requests { get; } = [];

        public Task<PaymentProcessingResult> ProcessAsync(
            PaymentProcessingRequest request,
            CancellationToken cancellationToken = default)
        {
            Requests.Add(request);

            if (exception is not null)
            {
                throw exception;
            }

            return Task.FromResult(new PaymentProcessingResult(outcome));
        }
    }
}
