using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotificationHub.Domain.Entities;

namespace NotificationHub.Infrastructure.Persistence.Configurations;

public class CampaignRecipientConfiguration : IEntityTypeConfiguration<CampaignRecipient>
{
    public void Configure(EntityTypeBuilder<CampaignRecipient> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.RecipientEmail).IsRequired().HasMaxLength(320);
        builder.Property(r => r.FirstName).HasMaxLength(100);
        builder.Property(r => r.LastName).HasMaxLength(100);

        builder.HasIndex(r => new { r.CampaignId, r.RecipientEmail }).IsUnique();

        builder.HasOne(r => r.Campaign)
            .WithMany(c => c.Recipients)
            .HasForeignKey(r => r.CampaignId);

        builder.HasOne(r => r.Notification)
            .WithMany()
            .HasForeignKey(r => r.NotificationId)
            .IsRequired(false);
    }
}