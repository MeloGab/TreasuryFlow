using MediatR;

namespace TreasuryFlow.Application.PaymentOrders.Commands.Lifecycle;

public sealed record FailPaymentOrderCommand(
    Guid Id)
    : IRequest;
