using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TreasuryFlow.Domain.PaymentOrders;
using TreasuryFlow.Domain.PaymentOrders.ValueObjects;

namespace TreasuryFlow.Infrastructure.Persistence.Configurations;

public sealed class PaymentOrderConfiguration
    : IEntityTypeConfiguration<PaymentOrder>
{
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
            .IsRequired();

        builder.Property(
                paymentOrder => paymentOrder.Beneficiary)
            .IsRequired();

        builder.Property(
                paymentOrder => paymentOrder.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(
                paymentOrder => paymentOrder.CreatedAt)
            .IsRequired();

        builder.Property(
            paymentOrder => paymentOrder.ProcessedAt);

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