using TreasuryFlow.Application.PaymentOrders.Queries.GetPaymentOrderById;
using TreasuryFlow.Domain.PaymentOrders;
using TreasuryFlow.Domain.PaymentOrders.Repositories;

namespace TreasuryFlow.Application.UnitTests.PaymentOrders.Queries.GetPaymentOrderById;

public sealed class GetPaymentOrderByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenPaymentOrderExists_ShouldReturnResult()
    {
        var paymentOrder = PaymentOrder.Create(
            "Supplier payment",
            875.40m,
            "usd",
            "Global Supplier Inc.");

        paymentOrder.Submit();

        var repository = new FakePaymentOrderRepository(
            paymentOrder);

        var handler = new GetPaymentOrderByIdQueryHandler(
            repository);

        var query = new GetPaymentOrderByIdQuery(
            paymentOrder.Id);

        var result = await handler.Handle(
            query,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(paymentOrder.Id, result.Id);
        Assert.Equal(paymentOrder.Description, result.Description);
        Assert.Equal(paymentOrder.Amount.Value, result.Amount);
        Assert.Equal(paymentOrder.Amount.Currency, result.Currency);
        Assert.Equal(paymentOrder.Beneficiary, result.Beneficiary);
        Assert.Equal(paymentOrder.Status, result.Status);
        Assert.Equal(paymentOrder.CreatedAt, result.CreatedAt);
        Assert.Equal(paymentOrder.ProcessedAt, result.ProcessedAt);
    }

    [Fact]
    public async Task Handle_WhenPaymentOrderDoesNotExist_ShouldReturnNull()
    {
        var repository = new FakePaymentOrderRepository(
            paymentOrder: null);

        var handler = new GetPaymentOrderByIdQueryHandler(
            repository);

        var query = new GetPaymentOrderByIdQuery(
            Guid.NewGuid());

        var result = await handler.Handle(
            query,
            CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_ShouldForwardArgumentsToRepository()
    {
        var paymentOrder = PaymentOrder.Create(
            "Supplier payment",
            200m,
            "BRL",
            "Supplier Ltd.");

        var repository = new FakePaymentOrderRepository(
            paymentOrder);

        var handler = new GetPaymentOrderByIdQueryHandler(
            repository);

        using var cancellationTokenSource =
            new CancellationTokenSource();

        var cancellationToken =
            cancellationTokenSource.Token;

        var query = new GetPaymentOrderByIdQuery(
            paymentOrder.Id);

        await handler.Handle(
            query,
            cancellationToken);

        Assert.Equal(
            paymentOrder.Id,
            repository.ReceivedId);

        Assert.Equal(
            cancellationToken,
            repository.ReceivedCancellationToken);
    }

    private sealed class FakePaymentOrderRepository(
        PaymentOrder? paymentOrder)
        : IPaymentOrderRepository
    {
        public Guid ReceivedId { get; private set; }

        public CancellationToken ReceivedCancellationToken
        {
            get;
            private set;
        }

        public Task<PaymentOrder?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            ReceivedId = id;
            ReceivedCancellationToken = cancellationToken;

            var result = paymentOrder?.Id == id
                ? paymentOrder
                : null;

            return Task.FromResult(
                result);
        }

        public Task AddAsync(
            PaymentOrder paymentOrder,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task UpdateAsync(
            PaymentOrder paymentOrder,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
