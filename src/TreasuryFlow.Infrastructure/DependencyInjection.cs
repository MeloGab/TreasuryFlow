using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Minio;
using TreasuryFlow.Application.PaymentOrders.Processing;
using TreasuryFlow.Application.PaymentOrders.Receipts;
using TreasuryFlow.Domain.PaymentOrders.Repositories;
using TreasuryFlow.Infrastructure.Messaging;
using TreasuryFlow.Infrastructure.Messaging.RabbitMq;
using TreasuryFlow.Infrastructure.PaymentProcessing;
using TreasuryFlow.Infrastructure.Persistence;
using TreasuryFlow.Infrastructure.Persistence.Outbox;
using TreasuryFlow.Infrastructure.Persistence.Repositories;
using TreasuryFlow.Infrastructure.Storage.Minio;

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

        services.AddSingleton<
            RabbitMqMessageRetryPolicy>();

        services.AddOptions<PaymentProcessorOptions>()
            .Bind(
                configuration.GetSection(
                    PaymentProcessorOptions.SectionName))
            .Validate(
                options =>
                    Enum.TryParse<PaymentProcessingOutcome>(
                        options.SimulatedOutcome,
                        ignoreCase: true,
                        out _),
                "Payment processor configuration is invalid.")
            .ValidateOnStart();

        services.AddOptions<MinioOptions>()
            .Bind(
                configuration.GetSection(
                    MinioOptions.SectionName))
            .Validate(
                options =>
                    !string.IsNullOrWhiteSpace(options.Endpoint) &&
                    !string.IsNullOrWhiteSpace(options.AccessKey) &&
                    !string.IsNullOrWhiteSpace(options.SecretKey) &&
                    !string.IsNullOrWhiteSpace(options.BucketName),
                "MinIO configuration is invalid.")
            .ValidateOnStart();

        services.AddSingleton<IMinioClient>(
            serviceProvider =>
            {
                var options = serviceProvider
                    .GetRequiredService<IOptions<MinioOptions>>()
                    .Value;

                return new MinioClient()
                    .WithEndpoint(
                        options.Endpoint)
                    .WithCredentials(
                        options.AccessKey,
                        options.SecretKey)
                    .WithSSL(options.UseSsl)
                    .Build();
            });

        services.AddSingleton<
            IPaymentReceiptStorage,
            MinioPaymentReceiptStorage>();

        services.AddSingleton<
            IPaymentProcessor,
            SimulatedPaymentProcessor>();

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
                            options.RetryExchangeName) &&
                        !string.IsNullOrWhiteSpace(
                            options.RetryQueueName) &&
                        !string.IsNullOrWhiteSpace(
                            options.RetryRoutingKey) &&
                        !string.IsNullOrWhiteSpace(
                            options.FailedExchangeName) &&
                        !string.IsNullOrWhiteSpace(
                            options.FailedQueueName) &&
                        !string.IsNullOrWhiteSpace(
                            options.FailedRoutingKey) &&
                        options.BatchSize > 0 &&
                        options.PollingIntervalSeconds > 0 &&
                        options.RetryDelaySeconds > 0 &&
                        options.ConsumerRetryDelaySeconds > 0 &&
                        options.ConsumerMaximumRetryAttempts > 0),
                "RabbitMQ configuration is invalid when enabled.")
            .ValidateOnStart();
    }
}
