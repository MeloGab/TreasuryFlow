using MediatR;
using TreasuryFlow.Application.Common.Exceptions;
using TreasuryFlow.Domain.PaymentOrders.Repositories;

namespace TreasuryFlow.Application.PaymentOrders.Commands.UpdatePaymentOrder;

public sealed class UpdatePaymentOrderCommandHandler(
    IPaymentOrderRepository paymentOrderRepository)
    : IRequestHandler<UpdatePaymentOrderCommand>
{
    public async Task Handle(
        UpdatePaymentOrderCommand request,
        CancellationToken cancellationToken)
    {
        var paymentOrder =
            await paymentOrderRepository.GetByIdAsync(
                request.Id,
                cancellationToken)
            ?? throw new PaymentOrderNotFoundException(
                request.Id);

        paymentOrder.UpdateDetails(
            request.Description,
            request.Amount,
            request.Currency,
            request.Beneficiary);

        await paymentOrderRepository.UpdateAsync(
            paymentOrder,
            cancellationToken);
    }
}
