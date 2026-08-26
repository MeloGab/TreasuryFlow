using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TreasuryFlow.Infrastructure.Persistence.Outbox;

namespace TreasuryFlow.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration
    : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(
        EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(
            message => message.Id);

        builder.Property(
                message => message.Id)
            .ValueGeneratedNever();

        builder.Property(
                message => message.Type)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(
                message => message.Content)
            .IsRequired();

        builder.Property(
                message => message.OccurredAt)
            .IsRequired();

        builder.Property(
            message => message.ProcessedAt);

        builder.Property(
            message => message.Error);

        builder.Property(
                message => message.RetryCount)
            .IsRequired();

        builder.Property(
            message => message.NextAttemptAt);

        builder.HasIndex(
            message => new
            {
                message.ProcessedAt,
                message.NextAttemptAt,
                message.OccurredAt
            });
    }
}
