using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TreasuryFlow.Application;
using TreasuryFlow.Application.PaymentOrders.Commands.CreatePaymentOrder;
using TreasuryFlow.Domain.PaymentOrders;
using TreasuryFlow.Infrastructure.Persistence;

namespace TreasuryFlow.Infrastructure.IntegrationTests.Application.PaymentOrders;

public sealed class CreatePaymentOrderIntegrationTests
{
    [Fact]
    public async Task SendAsync_WithValidCommand_ShouldPersistPaymentOrder()
    {
        await using var connection = new SqliteConnection(
            "Data Source=:memory:");

        await connection.OpenAsync();

        var services = new ServiceCollection();

        services.AddApplication();

        services.AddInfrastructure(
            dbContextOptions =>
                dbContextOptions.UseSqlite(connection));

        using var serviceProvider =
            services.BuildServiceProvider();

        Guid createdPaymentOrderId;

        using (var commandScope =
            serviceProvider.CreateScope())
        {
            var dbContext =
                commandScope.ServiceProvider
                    .GetRequiredService<TreasuryFlowDbContext>();

            await dbContext.Database.EnsureCreatedAsync();

            var sender =
                commandScope.ServiceProvider
                    .GetRequiredService<ISender>();

            var command = new CreatePaymentOrderCommand(
                "Integrated supplier payment",
                980.25m,
                "eur",
                "European Supplier GmbH");

            createdPaymentOrderId =
                await sender.Send(
                    command,
                    CancellationToken.None);
        }

        using var verificationScope =
            serviceProvider.CreateScope();

        var verificationContext =
            verificationScope.ServiceProvider
                .GetRequiredService<TreasuryFlowDbContext>();

        var persistedPaymentOrder =
            await verificationContext.PaymentOrders
                .AsNoTracking()
                .SingleAsync(
                    paymentOrder =>
                        paymentOrder.Id ==
                        createdPaymentOrderId);

        Assert.NotEqual(
            Guid.Empty,
            createdPaymentOrderId);

        Assert.Equal(
            createdPaymentOrderId,
            persistedPaymentOrder.Id);

        Assert.Equal(
            "Integrated supplier payment",
            persistedPaymentOrder.Description);

        Assert.Equal(
            980.25m,
            persistedPaymentOrder.Amount.Value);

        Assert.Equal(
            "EUR",
            persistedPaymentOrder.Amount.Currency);

        Assert.Equal(
            "European Supplier GmbH",
            persistedPaymentOrder.Beneficiary);

        Assert.Equal(
            PaymentOrderStatus.Draft,
            persistedPaymentOrder.Status);

        Assert.Null(
            persistedPaymentOrder.ProcessedAt);
    }
}