using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotificationHub.Domain.Entities;

namespace NotificationHub.Infrastructure.Persistence.Configurations;

public class WebhookConfiguration : IEntityTypeConfiguration<Webhook>
{
    public void Configure(EntityTypeBuilder<Webhook> builder)
    {
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Url).IsRequired().HasMaxLength(500);
        builder.Property(w => w.Events).HasColumnType("text[]");
        builder.HasIndex(w => w.OrganizationId);
    }
}
