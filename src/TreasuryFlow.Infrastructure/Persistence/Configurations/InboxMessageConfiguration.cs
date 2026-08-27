using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TreasuryFlow.Infrastructure.Persistence.Inbox;

namespace TreasuryFlow.Infrastructure.Persistence.Configurations;

public sealed class InboxMessageConfiguration
    : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(
        EntityTypeBuilder<InboxMessage> builder)
    {
        builder.ToTable(
            "InboxMessages");

        builder.HasKey(
            message => message.Id);

        builder.Property(
                message => message.Type)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(
                message => message.ProcessedAt)
            .IsRequired();
    }
}
