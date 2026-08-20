using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using TreasuryFlow.Application.PaymentOrders.Commands.CreatePaymentOrder;
using TreasuryFlow.Application.PaymentOrders.Repositories;
using TreasuryFlow.Domain.PaymentOrders;

namespace TreasuryFlow.Application.UnitTests.PaymentOrders.Commands.CreatePaymentOrder;

public class CreatePaymentOrderPipelineTests
{
    [Fact]
    public async Task Send_WithValidCommand_ShouldExecuteCompletePipeline()
    {
        var repository = new FakePaymentOrderRepository();

        using var serviceProvider =
            CreateServiceProvider(repository);

        var sender =
            serviceProvider.GetRequiredService<ISender>();

        var command = new CreatePaymentOrderCommand(
            Description: "Pagamento fornecedor",
            Amount: 15000m,
            Currency: " brl ",
            Beneficiary: "Fornecedor XPTO");

        var paymentOrderId = await sender.Send(
            command,
            CancellationToken.None);

        var paymentOrder =
            Assert.Single(repository.PaymentOrders);

        Assert.Equal(paymentOrder.Id, paymentOrderId);
        Assert.Equal("Pagamento fornecedor", paymentOrder.Description);
        Assert.Equal(15000m, paymentOrder.Amount.Value);
        Assert.Equal("BRL", paymentOrder.Amount.Currency);
        Assert.Equal("Fornecedor XPTO", paymentOrder.Beneficiary);
        Assert.Equal(PaymentOrderStatus.Draft, paymentOrder.Status);
    }

    [Fact]
    public async Task Send_WithInvalidCommand_ShouldStopBeforeRepository()
    {
        var repository = new FakePaymentOrderRepository();

        using var serviceProvider =
            CreateServiceProvider(repository);

        var sender =
            serviceProvider.GetRequiredService<ISender>();

        var command = new CreatePaymentOrderCommand(
            Description: string.Empty,
            Amount: 15000m,
            Currency: "BRL",
            Beneficiary: "Fornecedor XPTO");

        var action = () => sender.Send(
            command,
            CancellationToken.None);

        var exception =
            await Assert.ThrowsAsync<ValidationException>(
                action);

        Assert.Contains(
            exception.Errors,
            error => error.PropertyName ==
                nameof(CreatePaymentOrderCommand.Description));

        Assert.Empty(repository.PaymentOrders);
    }

    [Fact]
    public async Task Send_WithUnsupportedCurrency_ShouldStopBeforeRepository()
    {
        var repository = new FakePaymentOrderRepository();

        using var serviceProvider =
            CreateServiceProvider(repository);

        var sender =
            serviceProvider.GetRequiredService<ISender>();

        var command = new CreatePaymentOrderCommand(
            Description: "Pagamento fornecedor",
            Amount: 15000m,
            Currency: "JPY",
            Beneficiary: "Fornecedor XPTO");

        var action = () => sender.Send(
            command,
            CancellationToken.None);

        var exception =
            await Assert.ThrowsAsync<ValidationException>(
                action);

        Assert.Contains(
            exception.Errors,
            error =>
                error.PropertyName ==
                    nameof(CreatePaymentOrderCommand.Currency) &&
                error.ErrorMessage ==
                    "Currency is not supported.");

        Assert.Empty(repository.PaymentOrders);
    }

    private static ServiceProvider CreateServiceProvider(
        IPaymentOrderRepository repository)
    {
        var services = new ServiceCollection();

        services.AddApplication();

        services.AddSingleton(repository);

        return services.BuildServiceProvider();
    }

    private sealed class FakePaymentOrderRepository
        : IPaymentOrderRepository
    {
        public List<PaymentOrder> PaymentOrders { get; } = [];

        public Task AddAsync(
            PaymentOrder paymentOrder,
            CancellationToken cancellationToken = default)
        {
            PaymentOrders.Add(paymentOrder);

            return Task.CompletedTask;
        }
    }
}