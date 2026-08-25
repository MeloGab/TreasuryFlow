using MediatR;

namespace TreasuryFlow.Application.PaymentOrders.Queries.GetPaymentOrderById;

public sealed record GetPaymentOrderByIdQuery(
    Guid Id)
    : IRequest<GetPaymentOrderByIdResult?>;
