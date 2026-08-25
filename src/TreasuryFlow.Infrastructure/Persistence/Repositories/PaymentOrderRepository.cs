using Microsoft.EntityFrameworkCore;
using TreasuryFlow.Domain.PaymentOrders;
using TreasuryFlow.Domain.PaymentOrders.Repositories;

namespace TreasuryFlow.Infrastructure.Persistence.Repositories;

public sealed class PaymentOrderRepository(
    TreasuryFlowDbContext dbContext)
    : IPaymentOrderRepository
{
    public async Task<PaymentOrder?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.PaymentOrders
            .AsNoTracking()
            .SingleOrDefaultAsync(
                paymentOrder =>
                    paymentOrder.Id == id,
                cancellationToken);
    }

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