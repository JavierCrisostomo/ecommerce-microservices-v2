using Orders.Infrastructure.Persistence.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Orders.Infrastructure.Persistence.Configurations;

public class OrderReadModelConfiguration : IEntityTypeConfiguration<OrderReadModel>
{
    public void Configure(EntityTypeBuilder<OrderReadModel> builder)
    {
        builder.ToTable("OrderReadModels");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.CustomerId).IsRequired();
        builder.HasIndex(o => o.CustomerId);

        builder.Property(o => o.Status).IsRequired().HasMaxLength(20);
        builder.Property(o => o.TotalAmount).HasColumnType("decimal(18,2)");
        builder.Property(o => o.LinesJson).IsRequired();
        builder.Property(o => o.CancellationReason).HasMaxLength(500);
        builder.Property(o => o.CreatedAt).IsRequired();
    }
}
