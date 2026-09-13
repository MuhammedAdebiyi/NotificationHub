using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotificationHub.Domain.Entities;

namespace NotificationHub.Infrastructure.Persistence.Configurations;

public class WebhookDeliveryConfiguration : IEntityTypeConfiguration<WebhookDelivery>
{
    public void Configure(EntityTypeBuilder<WebhookDelivery> builder)
    {
        builder.HasKey(wd => wd.Id);
        builder.Property(wd => wd.Event).IsRequired().HasMaxLength(100);
        builder.Property(wd => wd.Payload).IsRequired();
        builder.HasIndex(wd => wd.WebhookId);
        builder.HasOne(wd => wd.Webhook)
            .WithMany()
            .HasForeignKey(wd => wd.WebhookId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
