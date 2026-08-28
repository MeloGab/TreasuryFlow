using System.Text;
using Microsoft.Extensions.Options;
using TreasuryFlow.Infrastructure.Messaging.RabbitMq;

namespace TreasuryFlow.Infrastructure.IntegrationTests.Messaging.RabbitMq;

public sealed class RabbitMqMessageRetryPolicyTests
{
    [Fact]
    public void Decide_WithoutRetryHeader_ShouldScheduleFirstRetry()
    {
        var policy = CreatePolicy();

        var decision = policy.Decide(
            headers: null);

        Assert.Equal(
            RabbitMqRetryAction.Retry,
            decision.Action);
        Assert.Equal(1, decision.RetryCount);
    }

    [Fact]
    public void Decide_BelowMaximum_ShouldIncrementRetryCount()
    {
        var policy = CreatePolicy();
        var headers = CreateHeaders(
            retryCount: 2);

        var decision = policy.Decide(headers);

        Assert.Equal(
            RabbitMqRetryAction.Retry,
            decision.Action);
        Assert.Equal(3, decision.RetryCount);
    }

    [Fact]
    public void Decide_AtMaximum_ShouldMoveMessageToFailed()
    {
        var policy = CreatePolicy();
        var headers = CreateHeaders(
            retryCount: 3L);

        var decision = policy.Decide(headers);

        Assert.Equal(
            RabbitMqRetryAction.MoveToFailed,
            decision.Action);
        Assert.Equal(3, decision.RetryCount);
    }

    [Fact]
    public void Decide_WithEncodedRetryCount_ShouldReadHeader()
    {
        var policy = CreatePolicy();
        var headers = CreateHeaders(
            retryCount: Encoding.UTF8.GetBytes("2"));

        var decision = policy.Decide(headers);

        Assert.Equal(
            RabbitMqRetryAction.Retry,
            decision.Action);
        Assert.Equal(3, decision.RetryCount);
    }

    [Fact]
    public void Decide_WithInvalidRetryCount_ShouldStartAtFirstRetry()
    {
        var policy = CreatePolicy();
        var headers = CreateHeaders(
            retryCount: "invalid");

        var decision = policy.Decide(headers);

        Assert.Equal(
            RabbitMqRetryAction.Retry,
            decision.Action);
        Assert.Equal(1, decision.RetryCount);
    }

    [Fact]
    public void CreateRetryHeaders_ShouldPreserveOriginalWithoutMutatingIt()
    {
        var policy = CreatePolicy();
        var originalHeaders = new Dictionary<string, object?>
        {
            ["correlation-context"] = "context-value"
        };

        var retryHeaders = policy.CreateRetryHeaders(
            originalHeaders,
            retryCount: 1);

        Assert.Equal(
            "context-value",
            retryHeaders["correlation-context"]);
        Assert.Equal(
            1,
            retryHeaders[
                RabbitMqMessageRetryPolicy.RetryCountHeaderName]);
        Assert.DoesNotContain(
            RabbitMqMessageRetryPolicy.RetryCountHeaderName,
            originalHeaders.Keys);
    }

    private static RabbitMqMessageRetryPolicy CreatePolicy()
    {
        var options = Options.Create(
            new RabbitMqOptions
            {
                ConsumerMaximumRetryAttempts = 3
            });

        return new RabbitMqMessageRetryPolicy(
            options);
    }

    private static Dictionary<string, object?> CreateHeaders(
        object retryCount)
    {
        return new Dictionary<string, object?>
        {
            [RabbitMqMessageRetryPolicy.RetryCountHeaderName] =
                retryCount
        };
    }
}
