using MediatR;
using TreasuryFlow.Application.Common.Exceptions;
using TreasuryFlow.Domain.PaymentOrders;
using TreasuryFlow.Domain.PaymentOrders.Repositories;

namespace TreasuryFlow.Application.PaymentOrders.Commands.Lifecycle;

public sealed class PaymentOrderLifecycleCommandHandler(
    IPaymentOrderRepository paymentOrderRepository)
    : IRequestHandler<SubmitPaymentOrderCommand>,
        IRequestHandler<CancelPaymentOrderCommand>
{
    public Task Handle(
        SubmitPaymentOrderCommand request,
        CancellationToken cancellationToken)
    {
        return ChangeStateAsync(
            request.Id,
            paymentOrder => paymentOrder.Submit(),
            cancellationToken);
    }

    public Task Handle(
        CancelPaymentOrderCommand request,
        CancellationToken cancellationToken)
    {
        return ChangeStateAsync(
            request.Id,
            paymentOrder => paymentOrder.Cancel(),
            cancellationToken);
    }

    private async Task ChangeStateAsync(
        Guid paymentOrderId,
        Action<PaymentOrder> changeState,
        CancellationToken cancellationToken)
    {
        var paymentOrder =
            await paymentOrderRepository.GetByIdAsync(
                paymentOrderId,
                cancellationToken)
            ?? throw new PaymentOrderNotFoundException(
                paymentOrderId);

        changeState(paymentOrder);

        await paymentOrderRepository.UpdateAsync(
            paymentOrder,
            cancellationToken);
    }
}
