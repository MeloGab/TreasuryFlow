namespace TreasuryFlow.Domain.PaymentOrders.Repositories;

public interface IPaymentOrderRepository
{
    Task AddAsync(
        PaymentOrder paymentOrder,
        CancellationToken cancellationToken = default);
}
