namespace TreasuryFlow.Infrastructure.Persistence.Inbox;

public sealed class InboxMessage
{
    public Guid Id { get; private set; }

    public string Type { get; private set; }

    public DateTime ProcessedAt { get; private set; }

    private InboxMessage()
    {
        Type = null!;
    }

    public InboxMessage(
        Guid id,
        string type,
        DateTime processedAt)
    {
        Id = id;
        Type = type;
        ProcessedAt = processedAt;
    }
}
