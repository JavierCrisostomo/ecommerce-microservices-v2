using Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations;

public class OrderReservationConfiguration : IEntityTypeConfiguration<OrderReservation>
{
    public void Configure(EntityTypeBuilder<OrderReservation> builder)
    {
        builder.ToTable("OrderReservations");

        builder.HasKey(r => r.OrderId);

        builder.Property(r => r.Status).HasConversion<string>().IsRequired().HasMaxLength(20);
        builder.Property(r => r.CreatedAt).IsRequired();

        builder.OwnsMany(r => r.Lines, lines =>
        {
            lines.ToTable("OrderReservationLines");
            lines.WithOwner().HasForeignKey("OrderId");
            lines.HasKey(l => l.Id);

            lines.Property(l => l.ProductId).IsRequired();
            lines.Property(l => l.Quantity).IsRequired();
        });

        builder.Navigation(r => r.Lines).Metadata.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
