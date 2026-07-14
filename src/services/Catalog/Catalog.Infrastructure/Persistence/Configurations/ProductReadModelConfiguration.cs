using Catalog.Infrastructure.Persistence.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Persistence.Configurations;

public class ProductReadModelConfiguration : IEntityTypeConfiguration<ProductReadModel>
{
    public void Configure(EntityTypeBuilder<ProductReadModel> builder)
    {
        builder.ToTable("ProductReadModels");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Sku).IsRequired().HasMaxLength(64);
        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Category).IsRequired().HasMaxLength(100);
        builder.HasIndex(p => p.Category);
        builder.Property(p => p.Price).HasColumnType("decimal(18,2)");
        builder.Property(p => p.CreatedAt).IsRequired();
    }
}
