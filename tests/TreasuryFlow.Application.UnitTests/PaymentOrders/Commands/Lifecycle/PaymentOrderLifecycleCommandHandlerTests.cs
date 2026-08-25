using TreasuryFlow.Application.Common.Exceptions;
using TreasuryFlow.Application.PaymentOrders.Commands.Lifecycle;
using TreasuryFlow.Domain.Common.Exceptions;
using TreasuryFlow.Domain.PaymentOrders;
using TreasuryFlow.Domain.PaymentOrders.Repositories;

namespace TreasuryFlow.Application.UnitTests.PaymentOrders.Commands.Lifecycle;

public sealed class PaymentOrderLifecycleCommandHandlerTests
{
    [Fact]
    public async Task HandleSubmit_WhenDraft_ShouldPersistPendingStatus()
    {
        var paymentOrder = CreateInState(
            PaymentOrderStatus.Draft);

        var repository = new FakePaymentOrderRepository(
            paymentOrder);

        var handler = new PaymentOrderLifecycleCommandHandler(
            repository);

        await handler.Handle(
            new SubmitPaymentOrderCommand(paymentOrder.Id),
            CancellationToken.None);

        Assert.Equal(
            PaymentOrderStatus.Pending,
            paymentOrder.Status);

        Assert.Same(
            paymentOrder,
            repository.UpdatedPaymentOrder);
    }

    [Fact]
    public async Task HandleStartProcessing_WhenPending_ShouldPersistProcessingStatus()
    {
        var paymentOrder = CreateInState(
            PaymentOrderStatus.Pending);

        var repository = new FakePaymentOrderRepository(
            paymentOrder);

        var handler = new PaymentOrderLifecycleCommandHandler(
            repository);

        await handler.Handle(
            new StartProcessingPaymentOrderCommand(
                paymentOrder.Id),
            CancellationToken.None);

        Assert.Equal(
            PaymentOrderStatus.Processing,
            paymentOrder.Status);

        Assert.Same(
            paymentOrder,
            repository.UpdatedPaymentOrder);
    }

    [Fact]
    public async Task HandleComplete_WhenProcessing_ShouldPersistCompletedStatus()
    {
        var paymentOrder = CreateInState(
            PaymentOrderStatus.Processing);

        var repository = new FakePaymentOrderRepository(
            paymentOrder);

        var handler = new PaymentOrderLifecycleCommandHandler(
            repository);

        await handler.Handle(
            new CompletePaymentOrderCommand(paymentOrder.Id),
            CancellationToken.None);

        Assert.Equal(
            PaymentOrderStatus.Completed,
            paymentOrder.Status);

        Assert.NotNull(
            paymentOrder.ProcessedAt);

        Assert.Same(
            paymentOrder,
            repository.UpdatedPaymentOrder);
    }

    [Fact]
    public async Task HandleFail_WhenProcessing_ShouldPersistFailedStatus()
    {
        var paymentOrder = CreateInState(
            PaymentOrderStatus.Processing);

        var repository = new FakePaymentOrderRepository(
            paymentOrder);

        var handler = new PaymentOrderLifecycleCommandHandler(
            repository);

        await handler.Handle(
            new FailPaymentOrderCommand(paymentOrder.Id),
            CancellationToken.None);

        Assert.Equal(
            PaymentOrderStatus.Failed,
            paymentOrder.Status);

        Assert.NotNull(
            paymentOrder.ProcessedAt);

        Assert.Same(
            paymentOrder,
            repository.UpdatedPaymentOrder);
    }

    [Theory]
    [InlineData(PaymentOrderStatus.Draft)]
    [InlineData(PaymentOrderStatus.Pending)]
    public async Task HandleCancel_WhenAllowed_ShouldPersistCancelledStatus(
        PaymentOrderStatus initialStatus)
    {
        var paymentOrder = CreateInState(
            initialStatus);

        var repository = new FakePaymentOrderRepository(
            paymentOrder);

        var handler = new PaymentOrderLifecycleCommandHandler(
            repository);

        await handler.Handle(
            new CancelPaymentOrderCommand(paymentOrder.Id),
            CancellationToken.None);

        Assert.Equal(
            PaymentOrderStatus.Cancelled,
            paymentOrder.Status);

        Assert.Same(
            paymentOrder,
            repository.UpdatedPaymentOrder);
    }

    [Fact]
    public async Task Handle_WhenPaymentOrderDoesNotExist_ShouldThrowNotFoundException()
    {
        var paymentOrderId = Guid.NewGuid();

        var repository = new FakePaymentOrderRepository(
            paymentOrder: null);

        var handler = new PaymentOrderLifecycleCommandHandler(
            repository);

        var action = () => handler.Handle(
            new SubmitPaymentOrderCommand(paymentOrderId),
            CancellationToken.None);

        var exception =
            await Assert.ThrowsAsync<PaymentOrderNotFoundException>(
                action);

        Assert.Equal(
            paymentOrderId,
            exception.PaymentOrderId);

        Assert.Null(
            repository.UpdatedPaymentOrder);
    }

    [Fact]
    public async Task Handle_WhenTransitionIsInvalid_ShouldNotPersistChanges()
    {
        var paymentOrder = CreateInState(
            PaymentOrderStatus.Pending);

        var repository = new FakePaymentOrderRepository(
            paymentOrder);

        var handler = new PaymentOrderLifecycleCommandHandler(
            repository);

        var action = () => handler.Handle(
            new SubmitPaymentOrderCommand(paymentOrder.Id),
            CancellationToken.None);

        await Assert.ThrowsAsync<DomainException>(
            action);

        Assert.Equal(
            PaymentOrderStatus.Pending,
            paymentOrder.Status);

        Assert.Null(
            repository.UpdatedPaymentOrder);
    }

    [Fact]
    public async Task Handle_ShouldForwardCancellationTokenToRepository()
    {
        var paymentOrder = CreateInState(
            PaymentOrderStatus.Draft);

        var repository = new FakePaymentOrderRepository(
            paymentOrder);

        var handler = new PaymentOrderLifecycleCommandHandler(
            repository);

        using var cancellationTokenSource =
            new CancellationTokenSource();

        var cancellationToken =
            cancellationTokenSource.Token;

        await handler.Handle(
            new CancelPaymentOrderCommand(paymentOrder.Id),
            cancellationToken);

        Assert.Equal(
            cancellationToken,
            repository.GetCancellationToken);

        Assert.Equal(
            cancellationToken,
            repository.UpdateCancellationToken);
    }

    private static PaymentOrder CreateInState(
        PaymentOrderStatus status)
    {
        var paymentOrder = PaymentOrder.Create(
            "Supplier payment",
            1250.75m,
            "BRL",
            "Supplier Ltd.");

        switch (status)
        {
            case PaymentOrderStatus.Draft:
                break;

            case PaymentOrderStatus.Pending:
                paymentOrder.Submit();
                break;

            case PaymentOrderStatus.Processing:
                paymentOrder.Submit();
                paymentOrder.StartProcessing();
                break;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(status),
                    status,
                    "Unsupported initial status.");
        }

        paymentOrder.ClearDomainEvents();

        return paymentOrder;
    }

    private sealed class FakePaymentOrderRepository(
        PaymentOrder? paymentOrder)
        : IPaymentOrderRepository
    {
        public PaymentOrder? UpdatedPaymentOrder { get; private set; }

        public CancellationToken GetCancellationToken { get; private set; }

        public CancellationToken UpdateCancellationToken { get; private set; }

        public Task<PaymentOrder?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            GetCancellationToken = cancellationToken;

            var result = paymentOrder?.Id == id
                ? paymentOrder
                : null;

            return Task.FromResult(result);
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
            UpdatedPaymentOrder = paymentOrder;
            UpdateCancellationToken = cancellationToken;

            return Task.CompletedTask;
        }
    }
}
