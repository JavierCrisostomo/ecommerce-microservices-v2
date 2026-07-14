using Orders.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Orders.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.CustomerId).IsRequired();
        builder.Property(o => o.Status).HasConversion<string>().IsRequired().HasMaxLength(20);
        builder.Property(o => o.TotalAmount).HasColumnType("decimal(18,2)");
        builder.Property(o => o.CancellationReason).HasMaxLength(500);
        builder.Property(o => o.CreatedAt).IsRequired();

        builder.OwnsMany(o => o.Lines, lines =>
        {
            lines.ToTable("OrderLines");
            lines.WithOwner().HasForeignKey("OrderId");
            lines.HasKey(l => l.Id);

            lines.Property(l => l.ProductId).IsRequired();
            lines.Property(l => l.ProductName).IsRequired().HasMaxLength(200);
            lines.Property(l => l.UnitPrice).HasColumnType("decimal(18,2)");
            lines.Property(l => l.Quantity).IsRequired();

            lines.Ignore(l => l.LineTotal);
        });

        builder.Navigation(o => o.Lines).Metadata.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
