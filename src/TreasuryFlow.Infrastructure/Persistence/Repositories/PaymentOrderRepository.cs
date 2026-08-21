using TreasuryFlow.Application.PaymentOrders.Repositories;
using TreasuryFlow.Domain.PaymentOrders;

namespace TreasuryFlow.Infrastructure.Persistence.Repositories;

public sealed class PaymentOrderRepository(
    TreasuryFlowDbContext dbContext)
    : IPaymentOrderRepository
{
    public async Task AddAsync(
        PaymentOrder paymentOrder,
        CancellationToken cancellationToken = default)
    {
        dbContext.PaymentOrders.Add(
            paymentOrder);

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }
}