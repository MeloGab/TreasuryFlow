using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TreasuryFlow.Domain.PaymentOrders;
using TreasuryFlow.Domain.PaymentOrders.ValueObjects;

namespace TreasuryFlow.Infrastructure.Persistence.Configurations;

public sealed class PaymentOrderConfiguration
    : IEntityTypeConfiguration<PaymentOrder>
{
    private static readonly ValueConverter<DateTime, DateTime>
        UtcDateTimeConverter = new(
            dateTime => dateTime,
            dateTime => DateTime.SpecifyKind(
                dateTime,
                DateTimeKind.Utc));

    private static readonly ValueConverter<DateTime?, DateTime?>
        NullableUtcDateTimeConverter = new(
            dateTime => dateTime,
            dateTime => dateTime.HasValue
                ? DateTime.SpecifyKind(
                    dateTime.Value,
                    DateTimeKind.Utc)
                : null);

    public void Configure(
        EntityTypeBuilder<PaymentOrder> builder)
    {
        builder.ToTable("PaymentOrders");

        builder.HasKey(
            paymentOrder => paymentOrder.Id);

        builder.Property(
                paymentOrder => paymentOrder.Id)
            .ValueGeneratedNever();

        builder.Property(
                paymentOrder => paymentOrder.Description)
            .HasMaxLength(PaymentOrder.MaxDescriptionLength)
            .IsRequired();

        builder.Property(
                paymentOrder => paymentOrder.Beneficiary)
            .HasMaxLength(PaymentOrder.MaxBeneficiaryLength)
            .IsRequired();

        builder.Property(
                paymentOrder => paymentOrder.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(
                paymentOrder => paymentOrder.CreatedAt)
            .HasConversion(UtcDateTimeConverter)
            .IsRequired();

        builder.Property(
                paymentOrder => paymentOrder.ProcessedAt)
            .HasConversion(NullableUtcDateTimeConverter);

        builder.ComplexProperty<Money>(
            paymentOrder => paymentOrder.Amount,
            amountBuilder =>
            {
                amountBuilder.Property<decimal>(
                        money => money.Value)
                    .HasColumnName("Amount")
                    .HasPrecision(18, 2)
                    .IsRequired();

                amountBuilder.Property<string>(
                        money => money.Currency)
                    .HasColumnName("Currency")
                    .HasMaxLength(3)
                    .IsRequired();
            });

        builder.Ignore(
            paymentOrder => paymentOrder.DomainEvents);
    }
}