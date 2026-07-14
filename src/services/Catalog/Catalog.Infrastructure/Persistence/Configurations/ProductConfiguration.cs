using Catalog.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Sku).IsRequired().HasMaxLength(64);
        builder.HasIndex(p => p.Sku).IsUnique();

        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Description).IsRequired().HasMaxLength(2000);
        builder.Property(p => p.Category).IsRequired().HasMaxLength(100);
        builder.Property(p => p.Price).HasColumnType("decimal(18,2)");
        builder.Property(p => p.CreatedAt).IsRequired();
    }
}
