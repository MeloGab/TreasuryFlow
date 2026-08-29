using System.Text.Json;
using Microsoft.Extensions.Options;
using Minio;
using Minio.DataModel.Args;
using TreasuryFlow.Application.PaymentOrders.Receipts;

namespace TreasuryFlow.Infrastructure.Storage.Minio;

public sealed class MinioPaymentReceiptStorage(
    IMinioClient minioClient,
    IOptions<MinioOptions> options)
    : IPaymentReceiptStorage
{
    private static readonly JsonSerializerOptions SerializerOptions =
        new(JsonSerializerDefaults.Web);

    private readonly MinioOptions _options = options.Value;
    private readonly SemaphoreSlim _bucketInitializationLock =
        new(1, 1);
    private bool _bucketInitialized;

    public async Task StoreAsync(
        PaymentReceipt receipt,
        CancellationToken cancellationToken = default)
    {
        await EnsureBucketExistsAsync(
            cancellationToken);

        var content = JsonSerializer.SerializeToUtf8Bytes(
            receipt,
            SerializerOptions);

        await using var stream = new MemoryStream(
            content,
            writable: false);

        var objectName =
            $"payment-orders/{receipt.PaymentOrderId}/receipt.json";

        var putObjectArgs = new PutObjectArgs()
            .WithBucket(_options.BucketName)
            .WithObject(objectName)
            .WithStreamData(stream)
            .WithObjectSize(stream.Length)
            .WithContentType("application/json");

        await minioClient.PutObjectAsync(
            putObjectArgs,
            cancellationToken);
    }

    private async Task EnsureBucketExistsAsync(
        CancellationToken cancellationToken)
    {
        if (_bucketInitialized)
        {
            return;
        }

        await _bucketInitializationLock.WaitAsync(
            cancellationToken);

        try
        {
            if (_bucketInitialized)
            {
                return;
            }

            var bucketExistsArgs = new BucketExistsArgs()
                .WithBucket(_options.BucketName);

            var bucketExists = await minioClient.BucketExistsAsync(
                bucketExistsArgs,
                cancellationToken);

            if (!bucketExists)
            {
                var makeBucketArgs = new MakeBucketArgs()
                    .WithBucket(_options.BucketName);

                await minioClient.MakeBucketAsync(
                    makeBucketArgs,
                    cancellationToken);
            }

            _bucketInitialized = true;
        }
        finally
        {
            _bucketInitializationLock.Release();
        }
    }
}
