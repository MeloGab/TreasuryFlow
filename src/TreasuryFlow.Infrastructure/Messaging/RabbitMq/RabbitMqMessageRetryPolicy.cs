using System.Globalization;
using System.Text;
using Microsoft.Extensions.Options;

namespace TreasuryFlow.Infrastructure.Messaging.RabbitMq;

public enum RabbitMqRetryAction
{
    Retry,
    MoveToFailed
}

public sealed record RabbitMqRetryDecision(
    RabbitMqRetryAction Action,
    int RetryCount);

public sealed class RabbitMqMessageRetryPolicy(
    IOptions<RabbitMqOptions> options)
{
    public const string RetryCountHeaderName =
        "x-treasuryflow-retry-count";

    private readonly int _maximumRetryAttempts =
        options.Value.ConsumerMaximumRetryAttempts;

    public RabbitMqRetryDecision Decide(
        IDictionary<string, object?>? headers)
    {
        var currentRetryCount = GetRetryCount(
            headers);

        if (currentRetryCount >= _maximumRetryAttempts)
        {
            return new RabbitMqRetryDecision(
                RabbitMqRetryAction.MoveToFailed,
                currentRetryCount);
        }

        return new RabbitMqRetryDecision(
            RabbitMqRetryAction.Retry,
            currentRetryCount + 1);
    }

    public IDictionary<string, object?> CreateRetryHeaders(
        IDictionary<string, object?>? headers,
        int retryCount)
    {
        var copiedHeaders = CopyHeaders(
            headers);

        copiedHeaders[RetryCountHeaderName] = retryCount;

        return copiedHeaders;
    }

    public IDictionary<string, object?> CopyHeaders(
        IDictionary<string, object?>? headers)
    {
        return headers is null
            ? new Dictionary<string, object?>()
            : new Dictionary<string, object?>(headers);
    }

    private static int GetRetryCount(
        IDictionary<string, object?>? headers)
    {
        if (headers is null ||
            !headers.TryGetValue(
                RetryCountHeaderName,
                out var value))
        {
            return 0;
        }

        return value switch
        {
            byte byteValue => byteValue,
            sbyte sbyteValue when sbyteValue >= 0 => sbyteValue,
            short shortValue when shortValue >= 0 => shortValue,
            ushort ushortValue => ushortValue,
            int intValue when intValue >= 0 => intValue,
            uint uintValue when uintValue <= int.MaxValue =>
                (int)uintValue,
            long longValue when
                longValue >= 0 &&
                longValue <= int.MaxValue =>
                (int)longValue,
            ulong ulongValue when ulongValue <= int.MaxValue =>
                (int)ulongValue,
            byte[] bytes => ParseRetryCount(
                Encoding.UTF8.GetString(bytes)),
            string stringValue => ParseRetryCount(
                stringValue),
            _ => 0
        };
    }

    private static int ParseRetryCount(
        string value)
    {
        return int.TryParse(
            value,
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out var retryCount) &&
            retryCount >= 0
            ? retryCount
            : 0;
    }
}
