using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using TreasuryFlow.Application.PaymentOrders.Processing;
using TreasuryFlow.Application.PaymentOrders.Receipts;
using TreasuryFlow.Contracts.PaymentOrders;
using TreasuryFlow.Domain.PaymentOrders;
using TreasuryFlow.Infrastructure.Messaging.RabbitMq;
using TreasuryFlow.Infrastructure.Persistence;

namespace TreasuryFlow.Infrastructure.IntegrationTests.Messaging.RabbitMq;

public sealed class PaymentOrderReceiptHandlingTests
{
    private static readonly DateTimeOffset InboxProcessedAt =
        new(2026, 8, 29, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task HandleAsync_WhenApproved_ShouldStoreReceiptAndComplete()
    {
        await using var connection = await CreateOpenConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        var paymentOrder = CreatePendingPaymentOrder();
        dbContext.PaymentOrders.Add(paymentOrder);
        await dbContext.SaveChangesAsync();

        var receiptStorage = new StubPaymentReceiptStorage();
        var handler = CreateHandler(
            dbContext,
            PaymentProcessingOutcome.Approved,
            receiptStorage);

        await handler.HandleAsync(
            CreateIntegrationEvent(paymentOrder.Id));

        dbContext.ChangeTracker.Clear();

        var persistedPaymentOrder =
            await dbContext.PaymentOrders.SingleAsync();
        var receipt = Assert.Single(receiptStorage.Receipts);

        Assert.Equal(
            PaymentOrderStatus.Completed,
            persistedPaymentOrder.Status);
        Assert.Equal(paymentOrder.Id, receipt.PaymentOrderId);
        Assert.Equal(paymentOrder.Description, receipt.Description);
        Assert.Equal(paymentOrder.Amount.Value, receipt.Amount);
        Assert.Equal(paymentOrder.Amount.Currency, receipt.Currency);
        Assert.Equal(paymentOrder.Beneficiary, receipt.Beneficiary);
        Assert.Equal(persistedPaymentOrder.ProcessedAt, receipt.ProcessedAt);
        Assert.Equal(1, await dbContext.InboxMessages.CountAsync());
    }

    [Fact]
    public async Task HandleAsync_WhenRejected_ShouldNotStoreReceipt()
    {
        await using var connection = await CreateOpenConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        var paymentOrder = CreatePendingPaymentOrder();
        dbContext.PaymentOrders.Add(paymentOrder);
        await dbContext.SaveChangesAsync();

        var receiptStorage = new StubPaymentReceiptStorage();
        var handler = CreateHandler(
            dbContext,
            PaymentProcessingOutcome.Rejected,
            receiptStorage);

        await handler.HandleAsync(
            CreateIntegrationEvent(paymentOrder.Id));

        dbContext.ChangeTracker.Clear();

        Assert.Equal(
            PaymentOrderStatus.Failed,
            (await dbContext.PaymentOrders.SingleAsync()).Status);
        Assert.Empty(receiptStorage.Receipts);
        Assert.Equal(1, await dbContext.InboxMessages.CountAsync());
    }

    [Fact]
    public async Task HandleAsync_WhenStorageIsUnavailable_ShouldRemainProcessingWithoutInbox()
    {
        await using var connection = await CreateOpenConnectionAsync();
        await using var dbContext = await CreateDbContextAsync(connection);

        var paymentOrder = CreatePendingPaymentOrder();
        dbContext.PaymentOrders.Add(paymentOrder);
        await dbContext.SaveChangesAsync();

        var receiptStorage = new StubPaymentReceiptStorage(
            new InvalidOperationException(
                "Receipt storage unavailable."));
        var handler = CreateHandler(
            dbContext,
            PaymentProcessingOutcome.Approved,
            receiptStorage);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(
                CreateIntegrationEvent(paymentOrder.Id)));

        dbContext.ChangeTracker.Clear();

        var persistedPaymentOrder =
            await dbContext.PaymentOrders.SingleAsync();

        Assert.Equal(
            PaymentOrderStatus.Processing,
            persistedPaymentOrder.Status);
        Assert.Null(persistedPaymentOrder.ProcessedAt);
        Assert.Single(receiptStorage.Receipts);
        Assert.Empty(await dbContext.InboxMessages.ToListAsync());
    }

    private static PaymentOrderSubmittedIntegrationEventHandler CreateHandler(
        TreasuryFlowDbContext dbContext,
        PaymentProcessingOutcome outcome,
        IPaymentReceiptStorage receiptStorage)
    {
        return new PaymentOrderSubmittedIntegrationEventHandler(
            dbContext,
            new StubPaymentProcessor(outcome),
            receiptStorage,
            new FixedTimeProvider(InboxProcessedAt),
            NullLogger<PaymentOrderSubmittedIntegrationEventHandler>.Instance);
    }

    private static PaymentOrder CreatePendingPaymentOrder()
    {
        var paymentOrder = PaymentOrder.Create(
            "Payment order receipt",
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
        PaymentProcessingOutcome outcome)
        : IPaymentProcessor
    {
        public Task<PaymentProcessingResult> ProcessAsync(
            PaymentProcessingRequest request,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                new PaymentProcessingResult(outcome));
        }
    }

    private sealed class StubPaymentReceiptStorage(
        Exception? exception = null)
        : IPaymentReceiptStorage
    {
        public List<PaymentReceipt> Receipts { get; } = [];

        public Task StoreAsync(
            PaymentReceipt receipt,
            CancellationToken cancellationToken = default)
        {
            Receipts.Add(receipt);

            if (exception is not null)
            {
                throw exception;
            }

            return Task.CompletedTask;
        }
    }
}
