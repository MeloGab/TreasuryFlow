using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TreasuryFlow.Application.PaymentOrders.Processing;
using TreasuryFlow.Application.PaymentOrders.Receipts;
using TreasuryFlow.Contracts.PaymentOrders;
using TreasuryFlow.Domain.Common.Exceptions;
using TreasuryFlow.Domain.PaymentOrders;
using TreasuryFlow.Infrastructure.Messaging.RabbitMq;
using TreasuryFlow.Infrastructure.Persistence;

namespace TreasuryFlow.Infrastructure.IntegrationTests.Messaging.RabbitMq;

public sealed class PaymentOrderSubmittedIntegrationEventHandlerTests
{
    private static readonly DateTimeOffset ProcessedAt =
        new(2026, 8, 26, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task HandleAsync_WithNewMessage_ShouldProcessAndPersistInbox()
    {
        await using var connection =
            await CreateOpenConnectionAsync();

        await using var dbContext =
            await CreateDbContextAsync(connection);

        var paymentOrder = CreatePendingPaymentOrder();

        dbContext.PaymentOrders.Add(paymentOrder);
        await dbContext.SaveChangesAsync();

        var integrationEvent = CreateIntegrationEvent(
            paymentOrder.Id);
        var handler = CreateHandler(dbContext);

        var result = await handler.HandleAsync(
            integrationEvent);

        dbContext.ChangeTracker.Clear();

        var persistedPaymentOrder =
            await dbContext.PaymentOrders.SingleAsync();
        var inboxMessage =
            await dbContext.InboxMessages.SingleAsync();

        Assert.Equal(
            IntegrationEventHandlingResult.Processed,
            result);
        Assert.Equal(
            PaymentOrderStatus.Completed,
            persistedPaymentOrder.Status);
        Assert.Equal(
            integrationEvent.MessageId,
            inboxMessage.Id);
        Assert.Equal(
            typeof(PaymentOrderSubmittedIntegrationEvent).FullName,
            inboxMessage.Type);
        Assert.Equal(
            ProcessedAt.UtcDateTime,
            inboxMessage.ProcessedAt);
    }

    [Fact]
    public async Task HandleAsync_WithProcessedMessage_ShouldBeIdempotent()
    {
        await using var connection =
            await CreateOpenConnectionAsync();

        await using var dbContext =
            await CreateDbContextAsync(connection);

        var paymentOrder = CreatePendingPaymentOrder();

        dbContext.PaymentOrders.Add(paymentOrder);
        await dbContext.SaveChangesAsync();

        var integrationEvent = CreateIntegrationEvent(
            paymentOrder.Id);
        var handler = CreateHandler(dbContext);

        await handler.HandleAsync(
            integrationEvent);

        var secondResult = await handler.HandleAsync(
            integrationEvent);

        Assert.Equal(
            IntegrationEventHandlingResult.AlreadyProcessed,
            secondResult);
        Assert.Equal(
            1,
            await dbContext.InboxMessages.CountAsync());
        Assert.Equal(
            PaymentOrderStatus.Completed,
            (await dbContext.PaymentOrders.SingleAsync()).Status);
    }

    [Fact]
    public async Task HandleAsync_WhenPaymentOrderDoesNotExist_ShouldNotPersistInbox()
    {
        await using var connection =
            await CreateOpenConnectionAsync();

        await using var dbContext =
            await CreateDbContextAsync(connection);

        var handler = CreateHandler(dbContext);
        var integrationEvent = CreateIntegrationEvent(
            Guid.NewGuid());

        var exception = await Assert.ThrowsAsync<
            NonRetryableIntegrationEventException>(
                () => handler.HandleAsync(
                    integrationEvent));

        Assert.Contains(
            integrationEvent.PaymentOrderId.ToString(),
            exception.Message);
        Assert.Empty(
            await dbContext.InboxMessages.ToListAsync());
    }

    [Fact]
    public async Task HandleAsync_WhenTransitionIsInvalid_ShouldNotPersistInbox()
    {
        await using var connection =
            await CreateOpenConnectionAsync();

        await using var dbContext =
            await CreateDbContextAsync(connection);

        var paymentOrder = PaymentOrder.Create(
            "Draft order",
            50m,
            "BRL",
            "Beneficiary");

        dbContext.PaymentOrders.Add(paymentOrder);
        await dbContext.SaveChangesAsync();

        var handler = CreateHandler(dbContext);

        var exception = await Assert.ThrowsAsync<
            NonRetryableIntegrationEventException>(
                () => handler.HandleAsync(
                    CreateIntegrationEvent(
                        paymentOrder.Id)));

        Assert.IsType<DomainException>(
            exception.InnerException);

        Assert.Empty(
            await dbContext.InboxMessages.ToListAsync());
        Assert.Equal(
            PaymentOrderStatus.Draft,
            (await dbContext.PaymentOrders.SingleAsync()).Status);
    }

    private static PaymentOrderSubmittedIntegrationEventHandler
        CreateHandler(
            TreasuryFlowDbContext dbContext)
    {
        return new PaymentOrderSubmittedIntegrationEventHandler(
            dbContext,
            new StubPaymentProcessor(),
            new StubPaymentReceiptStorage(),
            new FixedTimeProvider(ProcessedAt),
            NullLogger<
                PaymentOrderSubmittedIntegrationEventHandler>.Instance);
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

    private static PaymentOrderSubmittedIntegrationEvent
        CreateIntegrationEvent(
            Guid paymentOrderId)
    {
        return new PaymentOrderSubmittedIntegrationEvent(
            Guid.NewGuid(),
            paymentOrderId,
            125.50m,
            "BRL",
            ProcessedAt.UtcDateTime);
    }

    private static async Task<SqliteConnection>
        CreateOpenConnectionAsync()
    {
        var connection = new SqliteConnection(
            "Data Source=:memory:");

        await connection.OpenAsync();

        return connection;
    }

    private static async Task<TreasuryFlowDbContext>
        CreateDbContextAsync(
            SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<
                TreasuryFlowDbContext>()
            .UseSqlite(connection)
            .Options;

        var dbContext = new TreasuryFlowDbContext(
            options);

        await dbContext.Database.EnsureCreatedAsync();

        return dbContext;
    }

    private sealed class FixedTimeProvider(
        DateTimeOffset utcNow)
        : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() =>
            utcNow;
    }

    private sealed class StubPaymentProcessor : IPaymentProcessor
    {
        public Task<PaymentProcessingResult> ProcessAsync(
            PaymentProcessingRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                new PaymentProcessingResult(
                    PaymentProcessingOutcome.Approved));
        }
    }

    private sealed class StubPaymentReceiptStorage
        : IPaymentReceiptStorage
    {
        public Task StoreAsync(
            PaymentReceipt receipt,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
