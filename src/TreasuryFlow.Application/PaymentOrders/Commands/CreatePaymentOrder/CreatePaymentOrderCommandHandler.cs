using MediatR;
using TreasuryFlow.Domain.PaymentOrders;
using TreasuryFlow.Domain.PaymentOrders.Repositories;

namespace TreasuryFlow.Application.PaymentOrders.Commands.CreatePaymentOrder;

public sealed class CreatePaymentOrderCommandHandler(
    IPaymentOrderRepository paymentOrderRepository)
    : IRequestHandler<CreatePaymentOrderCommand, Guid>
{
    public async Task<Guid> Handle(
        CreatePaymentOrderCommand request,
        CancellationToken cancellationToken)
    {
        var paymentOrder = PaymentOrder.Create(
            request.Description,
            request.Amount,
            request.Currency,
            request.Beneficiary);

        await paymentOrderRepository.AddAsync(
            paymentOrder,
            cancellationToken);

        return paymentOrder.Id;
    }
}