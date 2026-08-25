using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TreasuryFlow.Domain.PaymentOrders;
using TreasuryFlow.Domain.PaymentOrders.Events;
using TreasuryFlow.Infrastructure.Persistence;
using TreasuryFlow.Infrastructure.Persistence.Outbox;

namespace TreasuryFlow.Infrastructure.IntegrationTests.Persistence.Outbox;

public sealed class OutboxPersistenceTests
{
    [Fact]
    public async Task SaveChangesAsync_WithDomainEvent_ShouldPersistOutboxMessage()
    {
        await using var connection = new SqliteConnection(
            "Data Source=:memory:");

        await connection.OpenAsync();

        var options =
            new DbContextOptionsBuilder<TreasuryFlowDbContext>()
                .UseSqlite(connection)
                .Options;

        var paymentOrder = PaymentOrder.Create(
            "Supplier payment",
            1475.80m,
            "USD",
            "Global Supplier Inc.");

        paymentOrder.Submit();

        var domainEvent = Assert.IsType<
            PaymentOrderSubmittedDomainEvent>(
                Assert.Single(paymentOrder.DomainEvents));

        await using (var writeContext =
            new TreasuryFlowDbContext(options))
        {
            await writeContext.Database.EnsureCreatedAsync();

            writeContext.PaymentOrders.Add(
                paymentOrder);

            await writeContext.SaveChangesAsync();
        }

        Assert.Empty(
            paymentOrder.DomainEvents);

        await using var readContext =
            new TreasuryFlowDbContext(options);

        var persistedPaymentOrder =
            await readContext.PaymentOrders
                .AsNoTracking()
                .SingleAsync();

        var outboxMessage =
            await readContext.OutboxMessages
                .AsNoTracking()
                .SingleAsync();

        Assert.Equal(
            PaymentOrderStatus.Pending,
            persistedPaymentOrder.Status);

        Assert.Equal(
            typeof(PaymentOrderSubmittedDomainEvent).FullName,
            outboxMessage.Type);

        Assert.Equal(
            domainEvent.OccurredAt,
            outboxMessage.OccurredAt);

        Assert.Null(
            outboxMessage.ProcessedAt);

        Assert.Null(
            outboxMessage.Error);

        using var content = JsonDocument.Parse(
            outboxMessage.Content);

        var root = content.RootElement;

        Assert.Equal(
            paymentOrder.Id,
            root.GetProperty("PaymentOrderId").GetGuid());

        Assert.Equal(
            1475.80m,
            root.GetProperty("Amount").GetDecimal());

        Assert.Equal(
            "USD",
            root.GetProperty("Currency").GetString());
    }

    [Fact]
    public async Task SaveChangesAsync_WithoutDomainEvents_ShouldNotPersistOutboxMessage()
    {
        await using var connection = new SqliteConnection(
            "Data Source=:memory:");

        await connection.OpenAsync();

        var options =
            new DbContextOptionsBuilder<TreasuryFlowDbContext>()
                .UseSqlite(connection)
                .Options;

        var paymentOrder = PaymentOrder.Create(
            "Supplier payment",
            640.25m,
            "BRL",
            "Supplier Ltd.");

        await using var dbContext =
            new TreasuryFlowDbContext(options);

        await dbContext.Database.EnsureCreatedAsync();

        dbContext.PaymentOrders.Add(
            paymentOrder);

        await dbContext.SaveChangesAsync();

        Assert.Empty(
            await dbContext.OutboxMessages
                .AsNoTracking()
                .ToListAsync());
    }

    [Fact]
    public async Task SaveChangesAsync_WhenSaveIsCancelled_ShouldPreserveDomainEventForRetry()
    {
        await using var connection = new SqliteConnection(
            "Data Source=:memory:");

        await connection.OpenAsync();

        var options =
            new DbContextOptionsBuilder<TreasuryFlowDbContext>()
                .UseSqlite(connection)
                .Options;

        var paymentOrder = PaymentOrder.Create(
            "Supplier payment",
            780.15m,
            "EUR",
            "European Supplier GmbH");

        paymentOrder.Submit();

        await using var dbContext =
            new TreasuryFlowDbContext(options);

        await dbContext.Database.EnsureCreatedAsync();

        dbContext.PaymentOrders.Add(
            paymentOrder);

        using var cancellationTokenSource =
            new CancellationTokenSource();

        cancellationTokenSource.Cancel();

        var action = () => dbContext.SaveChangesAsync(
            cancellationTokenSource.Token);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            action);

        Assert.Single(
            paymentOrder.DomainEvents);

        Assert.Empty(
            dbContext.ChangeTracker
                .Entries<OutboxMessage>());

        await dbContext.SaveChangesAsync();

        Assert.Empty(
            paymentOrder.DomainEvents);

        Assert.Single(
            await dbContext.OutboxMessages
                .AsNoTracking()
                .ToListAsync());
    }
}
