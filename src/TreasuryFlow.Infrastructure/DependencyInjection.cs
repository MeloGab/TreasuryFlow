using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TreasuryFlow.Domain.PaymentOrders.Repositories;
using TreasuryFlow.Infrastructure.Messaging;
using TreasuryFlow.Infrastructure.Messaging.RabbitMq;
using TreasuryFlow.Infrastructure.Persistence;
using TreasuryFlow.Infrastructure.Persistence.Outbox;
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
        string connectionString,
        IConfiguration configuration)
    {
        services.AddInfrastructure(
            connectionString);

        services.AddRabbitMqPublisher(
            configuration);

        return services;
    }

    public static IServiceCollection AddWorkerInfrastructure(
        this IServiceCollection services,
        string connectionString,
        IConfiguration configuration)
    {
        services.AddInfrastructure(
            connectionString);

        services.AddRabbitMqOptions(
            configuration);

        services.AddSingleton(
            TimeProvider.System);

        services.AddScoped<
            PaymentOrderSubmittedIntegrationEventHandler>();

        services.AddHostedService<
            RabbitMqConsumerBackgroundService>();

        return services;
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

    private static void AddRabbitMqPublisher(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddRabbitMqOptions(
            configuration);

        services.AddSingleton(
            TimeProvider.System);

        services.AddSingleton<
            IIntegrationEventPublisher,
            RabbitMqIntegrationEventPublisher>();

        services.AddScoped<OutboxMessageProcessor>();

        services.AddHostedService<
            OutboxPublisherBackgroundService>();
    }

    private static void AddRabbitMqOptions(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<RabbitMqOptions>()
            .Bind(
                configuration.GetSection(
                    RabbitMqOptions.SectionName))
            .Validate(
                options =>
                    !options.Enabled ||
                    (!string.IsNullOrWhiteSpace(options.HostName) &&
                        options.Port > 0 &&
                        !string.IsNullOrWhiteSpace(options.UserName) &&
                        !string.IsNullOrWhiteSpace(options.Password) &&
                        !string.IsNullOrWhiteSpace(options.ExchangeName) &&
                        !string.IsNullOrWhiteSpace(options.QueueName) &&
                        !string.IsNullOrWhiteSpace(
                            options.SubmittedRoutingKey) &&
                        !string.IsNullOrWhiteSpace(
                            options.FailedExchangeName) &&
                        !string.IsNullOrWhiteSpace(
                            options.FailedQueueName) &&
                        !string.IsNullOrWhiteSpace(
                            options.FailedRoutingKey) &&
                        options.BatchSize > 0 &&
                        options.PollingIntervalSeconds > 0 &&
                        options.RetryDelaySeconds > 0 &&
                        options.ConsumerRetryDelaySeconds > 0),
                "RabbitMQ configuration is invalid when enabled.")
            .ValidateOnStart();
    }
}
