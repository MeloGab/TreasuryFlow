using MediatR;

namespace TreasuryFlow.Application.PaymentOrders.Commands.CreatePaymentOrder;

public sealed record CreatePaymentOrderCommand(
    string Description,
    decimal Amount,
    string Currency,
    string Beneficiary)
    : IRequest<Guid>;