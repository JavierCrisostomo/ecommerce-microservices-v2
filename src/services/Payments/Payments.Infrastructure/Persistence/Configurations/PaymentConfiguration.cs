using Payments.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Payments.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.OrderId).IsRequired();
        builder.HasIndex(p => p.OrderId).IsUnique();

        builder.Property(p => p.Amount).HasColumnType("decimal(18,2)");
        builder.Property(p => p.Status).HasConversion<string>().IsRequired().HasMaxLength(20);
        builder.Property(p => p.FailureReason).HasMaxLength(500);
        builder.Property(p => p.CreatedAt).IsRequired();
    }
}
