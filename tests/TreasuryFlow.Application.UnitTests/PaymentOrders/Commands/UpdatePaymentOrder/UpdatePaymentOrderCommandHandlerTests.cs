using TreasuryFlow.Application.Common.Exceptions;
using TreasuryFlow.Application.PaymentOrders.Commands.UpdatePaymentOrder;
using TreasuryFlow.Domain.Common.Exceptions;
using TreasuryFlow.Domain.PaymentOrders;
using TreasuryFlow.Domain.PaymentOrders.Repositories;

namespace TreasuryFlow.Application.UnitTests.PaymentOrders.Commands.UpdatePaymentOrder;

public sealed class UpdatePaymentOrderCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenDraft_ShouldUpdateAndPersistPaymentOrder()
    {
        var paymentOrder = CreateDraftPaymentOrder();
        var repository = new FakePaymentOrderRepository(
            paymentOrder);
        var handler = new UpdatePaymentOrderCommandHandler(
            repository);
        using var cancellationTokenSource =
            new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        await handler.Handle(
            CreateCommand(paymentOrder.Id),
            cancellationToken);

        Assert.Equal(
            "Updated supplier payment",
            paymentOrder.Description);
        Assert.Equal(2500.75m, paymentOrder.Amount.Value);
        Assert.Equal("EUR", paymentOrder.Amount.Currency);
        Assert.Equal(
            "Updated Supplier Ltd.",
            paymentOrder.Beneficiary);
        Assert.Equal(
            PaymentOrderStatus.Draft,
            paymentOrder.Status);
        Assert.Same(
            paymentOrder,
            repository.UpdatedPaymentOrder);
        Assert.Equal(
            cancellationToken,
            repository.GetCancellationToken);
        Assert.Equal(
            cancellationToken,
            repository.UpdateCancellationToken);
    }

    [Fact]
    public async Task Handle_WhenPaymentOrderDoesNotExist_ShouldThrowNotFound()
    {
        var paymentOrderId = Guid.NewGuid();
        var repository = new FakePaymentOrderRepository(
            paymentOrder: null);
        var handler = new UpdatePaymentOrderCommandHandler(
            repository);

        var exception = await Assert.ThrowsAsync<
            PaymentOrderNotFoundException>(
                () => handler.Handle(
                    CreateCommand(paymentOrderId),
                    CancellationToken.None));

        Assert.Equal(
            paymentOrderId,
            exception.PaymentOrderId);
        Assert.Null(repository.UpdatedPaymentOrder);
    }

    [Fact]
    public async Task Handle_WhenNotDraft_ShouldNotPersistChanges()
    {
        var paymentOrder = CreateDraftPaymentOrder();
        paymentOrder.Submit();
        var repository = new FakePaymentOrderRepository(
            paymentOrder);
        var handler = new UpdatePaymentOrderCommandHandler(
            repository);
        var previousDescription = paymentOrder.Description;
        var previousAmount = paymentOrder.Amount;
        var previousBeneficiary = paymentOrder.Beneficiary;

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => handler.Handle(
                CreateCommand(paymentOrder.Id),
                CancellationToken.None));

        Assert.Equal(
            "Only draft payment orders can be updated.",
            exception.Message);
        Assert.Equal(previousDescription, paymentOrder.Description);
        Assert.Equal(previousAmount, paymentOrder.Amount);
        Assert.Equal(previousBeneficiary, paymentOrder.Beneficiary);
        Assert.Equal(
            PaymentOrderStatus.Pending,
            paymentOrder.Status);
        Assert.Null(repository.UpdatedPaymentOrder);
    }

    private static PaymentOrder CreateDraftPaymentOrder()
    {
        return PaymentOrder.Create(
            "Supplier payment",
            1250.50m,
            "BRL",
            "Supplier Ltd.");
    }

    private static UpdatePaymentOrderCommand CreateCommand(
        Guid paymentOrderId)
    {
        return new UpdatePaymentOrderCommand(
            paymentOrderId,
            "Updated supplier payment",
            2500.75m,
            "EUR",
            "Updated Supplier Ltd.");
    }

    private sealed class FakePaymentOrderRepository(
        PaymentOrder? paymentOrder)
        : IPaymentOrderRepository
    {
        public PaymentOrder? UpdatedPaymentOrder { get; private set; }

        public CancellationToken GetCancellationToken
        {
            get;
            private set;
        }

        public CancellationToken UpdateCancellationToken
        {
            get;
            private set;
        }

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
