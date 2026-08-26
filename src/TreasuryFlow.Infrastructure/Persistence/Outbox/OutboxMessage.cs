namespace TreasuryFlow.Infrastructure.Persistence.Outbox;

public sealed class OutboxMessage
{
    public Guid Id { get; private set; }

    public string Type { get; private set; }

    public string Content { get; private set; }

    public DateTime OccurredAt { get; private set; }

    public DateTime? ProcessedAt { get; private set; }

    public string? Error { get; private set; }

    public int RetryCount { get; private set; }

    public DateTime? NextAttemptAt { get; private set; }

    private OutboxMessage()
    {
        Type = null!;
        Content = null!;
    }

    public OutboxMessage(
        Guid id,
        string type,
        string content,
        DateTime occurredAt)
    {
        Id = id;
        Type = type;
        Content = content;
        OccurredAt = occurredAt;
    }

    public void MarkAsProcessed(
        DateTime processedAt)
    {
        ProcessedAt = processedAt;
        Error = null;
        NextAttemptAt = null;
    }

    public void MarkAsFailed(
        string error,
        DateTime nextAttemptAt)
    {
        RetryCount++;
        Error = error;
        NextAttemptAt = nextAttemptAt;
    }
}
