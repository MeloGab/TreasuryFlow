using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TreasuryFlow.Application.PaymentOrders.Repositories;
using TreasuryFlow.Infrastructure.Persistence;
using TreasuryFlow.Infrastructure.Persistence.Repositories;

namespace TreasuryFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        return services.AddInfrastructure(
            dbContextOptions =>
                dbContextOptions.UseSqlServer(
                    connectionString));
    }

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> configureDbContext)
    {
        services.AddDbContext<TreasuryFlowDbContext>(
            configureDbContext);

        services.AddScoped<
            IPaymentOrderRepository,
            PaymentOrderRepository>();

        return services;
    }
}