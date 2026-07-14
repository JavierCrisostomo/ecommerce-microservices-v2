using Notifications.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Notifications.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.OrderId).IsRequired();
        builder.HasIndex(n => n.OrderId);

        builder.Property(n => n.CustomerId).IsRequired();
        builder.Property(n => n.Type).HasConversion<string>().IsRequired().HasMaxLength(30);
        builder.Property(n => n.Message).IsRequired().HasMaxLength(1000);
        builder.Property(n => n.SentAt).IsRequired();
    }
}
