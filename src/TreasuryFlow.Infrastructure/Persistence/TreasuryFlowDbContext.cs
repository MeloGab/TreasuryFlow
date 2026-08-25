using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TreasuryFlow.Domain.Common.Events;
using TreasuryFlow.Domain.PaymentOrders;
using TreasuryFlow.Infrastructure.Persistence.Outbox;

namespace TreasuryFlow.Infrastructure.Persistence;

public sealed class TreasuryFlowDbContext(
    DbContextOptions<TreasuryFlowDbContext> options)
    : DbContext(options)
{
    public DbSet<PaymentOrder> PaymentOrders =>
        Set<PaymentOrder>();

    public DbSet<OutboxMessage> OutboxMessages =>
        Set<OutboxMessage>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(TreasuryFlowDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }

    public override int SaveChanges(
        bool acceptAllChangesOnSuccess)
    {
        var pendingOutbox = AddOutboxMessages();

        try
        {
            var result = base.SaveChanges(
                acceptAllChangesOnSuccess);

            ClearDomainEvents(
                pendingOutbox.Aggregates);

            return result;
        }
        catch
        {
            DetachOutboxMessages(
                pendingOutbox.Messages);

            throw;
        }
    }

    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        var pendingOutbox = AddOutboxMessages();

        try
        {
            var result = await base.SaveChangesAsync(
                acceptAllChangesOnSuccess,
                cancellationToken);

            ClearDomainEvents(
                pendingOutbox.Aggregates);

            return result;
        }
        catch
        {
            DetachOutboxMessages(
                pendingOutbox.Messages);

            throw;
        }
    }

    private PendingOutbox AddOutboxMessages()
    {
        var aggregates = ChangeTracker
            .Entries<PaymentOrder>()
            .Select(entry => entry.Entity)
            .Where(paymentOrder =>
                paymentOrder.DomainEvents.Count > 0)
            .Distinct()
            .ToArray();

        var messages = aggregates
            .SelectMany(paymentOrder =>
                paymentOrder.DomainEvents)
            .Select(CreateOutboxMessage)
            .ToArray();

        OutboxMessages.AddRange(
            messages);

        return new PendingOutbox(
            aggregates,
            messages);
    }

    private static OutboxMessage CreateOutboxMessage(
        IDomainEvent domainEvent)
    {
        var eventType = domainEvent.GetType();

        return new OutboxMessage(
            Guid.NewGuid(),
            eventType.FullName ?? eventType.Name,
            JsonSerializer.Serialize(
                domainEvent,
                eventType),
            domainEvent.OccurredAt);
    }

    private static void ClearDomainEvents(
        IEnumerable<PaymentOrder> paymentOrders)
    {
        foreach (var paymentOrder in paymentOrders)
        {
            paymentOrder.ClearDomainEvents();
        }
    }

    private void DetachOutboxMessages(
        IEnumerable<OutboxMessage> messages)
    {
        foreach (var message in messages)
        {
            Entry(message).State = EntityState.Detached;
        }
    }

    private sealed record PendingOutbox(
        IReadOnlyCollection<PaymentOrder> Aggregates,
        IReadOnlyCollection<OutboxMessage> Messages);
}