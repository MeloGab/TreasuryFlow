using MediatR;

namespace TreasuryFlow.Application.PaymentOrders.Commands.Lifecycle;

public sealed record CompletePaymentOrderCommand(
    Guid Id)
    : IRequest;
