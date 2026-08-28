using MediatR;

namespace TreasuryFlow.Application.PaymentOrders.Commands.UpdatePaymentOrder;

public sealed record UpdatePaymentOrderCommand(
    Guid Id,
    string Description,
    decimal Amount,
    string Currency,
    string Beneficiary)
    : IRequest;
