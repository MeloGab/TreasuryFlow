using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TreasuryFlow.Domain.PaymentOrders;
using TreasuryFlow.Infrastructure.Persistence;

namespace TreasuryFlow.Infrastructure.IntegrationTests.Persistence;

public sealed class PaymentOrderPersistenceTests
{
    [Fact]
    public async Task SaveAndLoadAsync_ShouldRehydratePaymentOrder()
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
            1500.75m,
            "brl",
            "Acme Ltd.");

        paymentOrder.Submit();

        var expectedId = paymentOrder.Id;
        var expectedCreatedAt = paymentOrder.CreatedAt;

        await using (var writeContext =
            new TreasuryFlowDbContext(options))
        {
            await writeContext.Database.EnsureCreatedAsync();

            writeContext.PaymentOrders.Add(paymentOrder);

            await writeContext.SaveChangesAsync();
        }

        PaymentOrder rehydratedPaymentOrder;

        await using (var readContext =
            new TreasuryFlowDbContext(options))
        {
            rehydratedPaymentOrder =
                await readContext.PaymentOrders
                    .AsNoTracking()
                    .SingleAsync(
                        paymentOrder =>
                            paymentOrder.Id == expectedId);
        }

        Assert.Equal(
            expectedId,
            rehydratedPaymentOrder.Id);

        Assert.Equal(
            "Supplier payment",
            rehydratedPaymentOrder.Description);

        Assert.Equal(
            1500.75m,
            rehydratedPaymentOrder.Amount.Value);

        Assert.Equal(
            "BRL",
            rehydratedPaymentOrder.Amount.Currency);

        Assert.Equal(
            "Acme Ltd.",
            rehydratedPaymentOrder.Beneficiary);

        Assert.Equal(
            PaymentOrderStatus.Pending,
            rehydratedPaymentOrder.Status);

        Assert.Equal(
            expectedCreatedAt,
            rehydratedPaymentOrder.CreatedAt);

        Assert.Null(
            rehydratedPaymentOrder.ProcessedAt);

        Assert.Empty(
            rehydratedPaymentOrder.DomainEvents);
    }
}