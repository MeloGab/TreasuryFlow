namespace TreasuryFlow.Domain.PaymentOrders.Repositories;

public interface IPaymentOrderRepository
{
    Task<PaymentOrder?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        PaymentOrder paymentOrder,
        CancellationToken cancellationToken = default);
}
