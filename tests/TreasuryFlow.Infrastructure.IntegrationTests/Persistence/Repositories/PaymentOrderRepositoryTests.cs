using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TreasuryFlow.Domain.PaymentOrders;
using TreasuryFlow.Infrastructure.Persistence;
using TreasuryFlow.Infrastructure.Persistence.Repositories;

namespace TreasuryFlow.Infrastructure.IntegrationTests.Persistence.Repositories;

public sealed class PaymentOrderRepositoryTests
{
    [Fact]
    public async Task AddAsync_ShouldPersistPaymentOrder()
    {
        await using var connection = new SqliteConnection(
            "Data Source=:memory:");

        await connection.OpenAsync();

        var options =
            new DbContextOptionsBuilder<TreasuryFlowDbContext>()
                .UseSqlite(connection)
                .Options;

        var paymentOrder = PaymentOrder.Create(
            "Monthly supplier payment",
            2750.50m,
            "USD",
            "Global Supplier Inc.");

        await using (var writeContext =
            new TreasuryFlowDbContext(options))
        {
            await writeContext.Database.EnsureCreatedAsync();

            var repository =
                new PaymentOrderRepository(writeContext);

            await repository.AddAsync(
                paymentOrder,
                CancellationToken.None);
        }

        await using var readContext =
            new TreasuryFlowDbContext(options);

        var persistedPaymentOrder =
            await readContext.PaymentOrders
                .AsNoTracking()
                .SingleAsync(
                    persistedPaymentOrder =>
                        persistedPaymentOrder.Id ==
                        paymentOrder.Id);

        Assert.Equal(
            paymentOrder.Id,
            persistedPaymentOrder.Id);

        Assert.Equal(
            "Monthly supplier payment",
            persistedPaymentOrder.Description);

        Assert.Equal(
            2750.50m,
            persistedPaymentOrder.Amount.Value);

        Assert.Equal(
            "USD",
            persistedPaymentOrder.Amount.Currency);

        Assert.Equal(
            "Global Supplier Inc.",
            persistedPaymentOrder.Beneficiary);

        Assert.Equal(
            PaymentOrderStatus.Draft,
            persistedPaymentOrder.Status);

        Assert.Equal(
            paymentOrder.CreatedAt,
            persistedPaymentOrder.CreatedAt);

        Assert.Null(
            persistedPaymentOrder.ProcessedAt);

        Assert.Empty(
            persistedPaymentOrder.DomainEvents);
    }
}