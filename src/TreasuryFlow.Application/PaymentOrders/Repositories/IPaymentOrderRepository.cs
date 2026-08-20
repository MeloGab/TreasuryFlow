using TreasuryFlow.Domain.PaymentOrders;

namespace TreasuryFlow.Application.PaymentOrders.Repositories;

public interface IPaymentOrderRepository
{
    Task AddAsync(
        PaymentOrder paymentOrder,
        CancellationToken cancellationToken = default);
}