using MediatR;

namespace TreasuryFlow.Application.PaymentOrders.Commands.Lifecycle;

public sealed record SubmitPaymentOrderCommand(
    Guid Id)
    : IRequest;
