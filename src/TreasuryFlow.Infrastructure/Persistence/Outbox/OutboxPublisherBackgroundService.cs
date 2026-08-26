using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TreasuryFlow.Infrastructure.Messaging.RabbitMq;

namespace TreasuryFlow.Infrastructure.Persistence.Outbox;

public sealed class OutboxPublisherBackgroundService(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<RabbitMqOptions> options,
    ILogger<OutboxPublisherBackgroundService> logger)
    : BackgroundService
{
    private readonly RabbitMqOptions _options = options.Value;

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation(
                "RabbitMQ outbox publisher is disabled.");

            return;
        }

        logger.LogInformation(
            "RabbitMQ outbox publisher started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope =
                    serviceScopeFactory.CreateAsyncScope();

                var processor = scope.ServiceProvider
                    .GetRequiredService<OutboxMessageProcessor>();

                var processedCount =
                    await processor.ProcessBatchAsync(
                        stoppingToken);

                if (processedCount > 0)
                {
                    continue;
                }
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Unexpected error while processing the outbox.");
            }

            await Task.Delay(
                TimeSpan.FromSeconds(
                    _options.PollingIntervalSeconds),
                stoppingToken);
        }
    }
}
