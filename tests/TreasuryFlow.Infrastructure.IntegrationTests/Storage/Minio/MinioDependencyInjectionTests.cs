using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Minio;
using TreasuryFlow.Application.PaymentOrders.Receipts;
using TreasuryFlow.Infrastructure.Storage.Minio;

namespace TreasuryFlow.Infrastructure.IntegrationTests.Storage.Minio;

public sealed class MinioDependencyInjectionTests
{
    [Fact]
    public void AddWorkerInfrastructure_WithInvalidMinioConfiguration_ShouldRejectOptions()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection()
            .Build();

        services.AddWorkerInfrastructure(
            CreateConnectionString(),
            configuration);

        using var serviceProvider = services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(
            () => serviceProvider
                .GetRequiredService<IOptions<MinioOptions>>()
                .Value);
    }

    [Fact]
    public void AddWorkerInfrastructure_WithValidMinioConfiguration_ShouldRegisterStorage()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Minio:Endpoint"] = "127.0.0.1:9000",
                    ["Minio:UseSsl"] = "false",
                    ["Minio:AccessKey"] = "test-access-key",
                    ["Minio:SecretKey"] = "test-secret-key",
                    ["Minio:BucketName"] =
                        "treasuryflow-payment-receipts"
                })
            .Build();

        services.AddWorkerInfrastructure(
            CreateConnectionString(),
            configuration);

        using var serviceProvider = services.BuildServiceProvider();

        var minioClient = serviceProvider
            .GetRequiredService<IMinioClient>();
        var receiptStorage = serviceProvider
            .GetRequiredService<IPaymentReceiptStorage>();

        Assert.NotNull(minioClient);
        Assert.IsType<MinioPaymentReceiptStorage>(receiptStorage);
    }

    private static string CreateConnectionString()
    {
        return
            "Server=localhost;Database=TreasuryFlow;" +
            "Trusted_Connection=True;TrustServerCertificate=True";
    }
}
