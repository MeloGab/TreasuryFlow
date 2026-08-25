using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TreasuryFlow.Domain.PaymentOrders;
using TreasuryFlow.Infrastructure.Persistence;
using TreasuryFlow.Infrastructure.Persistence.Repositories;

namespace TreasuryFlow.Infrastructure.IntegrationTests.Persistence.Repositories;

public sealed class GetPaymentOrderByIdRepositoryTests
{
    [Fact]
    public async Task GetByIdAsync_WhenPaymentOrderExists_ShouldReturnUntrackedEntity()
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
            450.90m,
            "EUR",
            "European Supplier GmbH");

        paymentOrder.Submit();

        await using (var writeContext =
            new TreasuryFlowDbContext(options))
        {
            await writeContext.Database.EnsureCreatedAsync();

            writeContext.PaymentOrders.Add(
                paymentOrder);

            await writeContext.SaveChangesAsync();
        }

        await using var readContext =
            new TreasuryFlowDbContext(options);

        var repository = new PaymentOrderRepository(
            readContext);

        var result = await repository.GetByIdAsync(
            paymentOrder.Id,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(paymentOrder.Id, result.Id);
        Assert.Equal(paymentOrder.Description, result.Description);
        Assert.Equal(paymentOrder.Amount.Value, result.Amount.Value);
        Assert.Equal(paymentOrder.Amount.Currency, result.Amount.Currency);
        Assert.Equal(paymentOrder.Beneficiary, result.Beneficiary);
        Assert.Equal(paymentOrder.Status, result.Status);
        Assert.Equal(paymentOrder.CreatedAt, result.CreatedAt);
        Assert.Equal(paymentOrder.ProcessedAt, result.ProcessedAt);
        Assert.Empty(result.DomainEvents);
        Assert.Empty(
            readContext.ChangeTracker.Entries<PaymentOrder>());
    }

    [Fact]
    public async Task GetByIdAsync_WhenPaymentOrderDoesNotExist_ShouldReturnNull()
    {
        await using var connection = new SqliteConnection(
            "Data Source=:memory:");

        await connection.OpenAsync();

        var options =
            new DbContextOptionsBuilder<TreasuryFlowDbContext>()
                .UseSqlite(connection)
                .Options;

        await using var dbContext =
            new TreasuryFlowDbContext(options);

        await dbContext.Database.EnsureCreatedAsync();

        var repository = new PaymentOrderRepository(
            dbContext);

        var result = await repository.GetByIdAsync(
            Guid.NewGuid(),
            CancellationToken.None);

        Assert.Null(result);
    }
}
