using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TreasuryFlow.Domain.PaymentOrders;
using TreasuryFlow.Infrastructure.Persistence;
using TreasuryFlow.Infrastructure.Persistence.Repositories;

namespace TreasuryFlow.Infrastructure.IntegrationTests.Persistence.Repositories;

public sealed class UpdatePaymentOrderRepositoryTests
{
    [Fact]
    public async Task UpdateAsync_ShouldPersistModifiedPaymentOrder()
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
            890.25m,
            "USD",
            "Global Supplier Inc.");

        await using (var seedContext =
            new TreasuryFlowDbContext(options))
        {
            await seedContext.Database.EnsureCreatedAsync();

            seedContext.PaymentOrders.Add(
                paymentOrder);

            await seedContext.SaveChangesAsync();
        }

        await using (var updateContext =
            new TreasuryFlowDbContext(options))
        {
            var repository = new PaymentOrderRepository(
                updateContext);

            var paymentOrderToUpdate =
                await repository.GetByIdAsync(
                    paymentOrder.Id,
                    CancellationToken.None);

            Assert.NotNull(paymentOrderToUpdate);

            paymentOrderToUpdate.Submit();

            await repository.UpdateAsync(
                paymentOrderToUpdate,
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
            PaymentOrderStatus.Pending,
            persistedPaymentOrder.Status);

        Assert.Null(
            persistedPaymentOrder.ProcessedAt);
    }
}
