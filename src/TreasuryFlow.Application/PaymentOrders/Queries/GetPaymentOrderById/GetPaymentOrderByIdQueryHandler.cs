using MediatR;
using TreasuryFlow.Domain.PaymentOrders.Repositories;

namespace TreasuryFlow.Application.PaymentOrders.Queries.GetPaymentOrderById;

public sealed class GetPaymentOrderByIdQueryHandler(
    IPaymentOrderRepository paymentOrderRepository)
    : IRequestHandler<
        GetPaymentOrderByIdQuery,
        GetPaymentOrderByIdResult?>
{
    public async Task<GetPaymentOrderByIdResult?> Handle(
        GetPaymentOrderByIdQuery request,
        CancellationToken cancellationToken)
    {
        var paymentOrder =
            await paymentOrderRepository.GetByIdAsync(
                request.Id,
                cancellationToken);

        if (paymentOrder is null)
        {
            return null;
        }

        return new GetPaymentOrderByIdResult(
            paymentOrder.Id,
            paymentOrder.Description,
            paymentOrder.Amount.Value,
            paymentOrder.Amount.Currency,
            paymentOrder.Beneficiary,
            paymentOrder.Status,
            paymentOrder.CreatedAt,
            paymentOrder.ProcessedAt);
    }
}
