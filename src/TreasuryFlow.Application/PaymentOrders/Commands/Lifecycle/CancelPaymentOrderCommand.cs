using MediatR;

namespace TreasuryFlow.Application.PaymentOrders.Commands.Lifecycle;

public sealed record CancelPaymentOrderCommand(
    Guid Id)
    : IRequest;
