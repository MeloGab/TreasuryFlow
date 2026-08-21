using Microsoft.EntityFrameworkCore;
using TreasuryFlow.Domain.PaymentOrders;

namespace TreasuryFlow.Infrastructure.Persistence;

public sealed class TreasuryFlowDbContext(
    DbContextOptions<TreasuryFlowDbContext> options)
    : DbContext(options)
{
    public DbSet<PaymentOrder> PaymentOrders =>
        Set<PaymentOrder>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(TreasuryFlowDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}