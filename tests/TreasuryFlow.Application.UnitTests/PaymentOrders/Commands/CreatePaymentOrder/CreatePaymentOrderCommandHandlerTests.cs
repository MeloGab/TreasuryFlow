using TreasuryFlow.Application.PaymentOrders.Commands.CreatePaymentOrder;
using TreasuryFlow.Domain.PaymentOrders;
using TreasuryFlow.Domain.PaymentOrders.Repositories;

namespace TreasuryFlow.Application.UnitTests.PaymentOrders.Commands.CreatePaymentOrder;

public class CreatePaymentOrderCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateAndPersistDraftPaymentOrder()
    {
        var repository = new FakePaymentOrderRepository();
        var handler = new CreatePaymentOrderCommandHandler(repository);

        var command = new CreatePaymentOrderCommand(
            Description: "Pagamento fornecedor",
            Amount: 15000m,
            Currency: "BRL",
            Beneficiary: "Fornecedor XPTO");

        var paymentOrderId = await handler.Handle(
            command,
            CancellationToken.None);

        var paymentOrder = Assert.Single(repository.PaymentOrders);

        Assert.Equal(paymentOrder.Id, paymentOrderId);
        Assert.Equal("Pagamento fornecedor", paymentOrder.Description);
        Assert.Equal(15000m, paymentOrder.Amount.Value);
        Assert.Equal("BRL", paymentOrder.Amount.Currency);
        Assert.Equal("Fornecedor XPTO", paymentOrder.Beneficiary);
        Assert.Equal(PaymentOrderStatus.Draft, paymentOrder.Status);
        Assert.Null(paymentOrder.ProcessedAt);
        Assert.Empty(paymentOrder.DomainEvents);
    }

    [Fact]
    public async Task Handle_ShouldForwardCancellationTokenToRepository()
    {
        var repository = new FakePaymentOrderRepository();
        var handler = new CreatePaymentOrderCommandHandler(repository);

        var command = new CreatePaymentOrderCommand(
            Description: "Pagamento fornecedor",
            Amount: 15000m,
            Currency: "BRL",
            Beneficiary: "Fornecedor XPTO");

        using var cancellationTokenSource =
            new CancellationTokenSource();

        var cancellationToken = cancellationTokenSource.Token;

        await handler.Handle(
            command,
            cancellationToken);

        Assert.Equal(
            cancellationToken,
            repository.ReceivedCancellationToken);
    }

    private sealed class FakePaymentOrderRepository
        : IPaymentOrderRepository
    {
        public List<PaymentOrder> PaymentOrders { get; } = [];

        public CancellationToken ReceivedCancellationToken
        {
            get;
            private set;
        }

        public Task<PaymentOrder?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<PaymentOrder?>(null);
        }

        public Task AddAsync(
            PaymentOrder paymentOrder,
            CancellationToken cancellationToken = default)
        {
            PaymentOrders.Add(paymentOrder);
            ReceivedCancellationToken = cancellationToken;

            return Task.CompletedTask;
        }
    }
}