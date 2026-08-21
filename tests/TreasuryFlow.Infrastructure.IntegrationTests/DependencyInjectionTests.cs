using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TreasuryFlow.Application.PaymentOrders.Repositories;
using TreasuryFlow.Infrastructure.Persistence;
using TreasuryFlow.Infrastructure.Persistence.Repositories;

namespace TreasuryFlow.Infrastructure.IntegrationTests;

public sealed class DependencyInjectionTests
{
    [Fact]
    public void AddInfrastructure_ShouldRegisterDbContextAndRepository()
    {
        using var connection = new SqliteConnection(
            "Data Source=:memory:");

        var services = new ServiceCollection();

        var result = services.AddInfrastructure(
            dbContextOptions =>
                dbContextOptions.UseSqlite(connection));

        using var serviceProvider =
            services.BuildServiceProvider();

        using var scope =
            serviceProvider.CreateScope();

        var dbContext =
            scope.ServiceProvider
                .GetRequiredService<TreasuryFlowDbContext>();

        var repository =
            scope.ServiceProvider
                .GetRequiredService<IPaymentOrderRepository>();

        Assert.Same(
            services,
            result);

        Assert.NotNull(
            dbContext);

        Assert.IsType<PaymentOrderRepository>(
            repository);
    }
}