using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotificationHub.Domain.Entities;
using NotificationHub.Domain.Enums;

namespace NotificationHub.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(n => n.Id);

        builder.Property(n => n.RecipientEmail).IsRequired().HasMaxLength(320);
        builder.Property(n => n.Type).IsRequired().HasMaxLength(100);
        builder.Property(n => n.Payload).IsRequired();
        builder.Property(n => n.Channel).HasConversion<string>().HasMaxLength(20);
        builder.Property(n => n.Status).HasConversion<string>().HasMaxLength(20);

        builder.Property(n => n.ReplyTo).HasMaxLength(320);
        builder.Property(n => n.SenderEmail).HasMaxLength(320);
        builder.Property(n => n.SenderName).HasMaxLength(100);
        // Rows written before this feature exist: they read back as
        // ProviderDefault instead of an unparsable empty string.
        builder.Property(n => n.ReplyToSource).HasConversion<string>().HasMaxLength(20)
            .HasDefaultValue(ResolutionSource.ProviderDefault);
        builder.Property(n => n.SenderSource).HasConversion<string>().HasMaxLength(20)
            .HasDefaultValue(ResolutionSource.ProviderDefault);

        // Template context is informational — deleting a template must not
        // delete (or block deleting) the notifications sent from it.
        builder.HasOne(n => n.Template)
            .WithMany()
            .HasForeignKey(n => n.TemplateId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(n => n.Campaign)
            .WithMany()
            .HasForeignKey(n => n.CampaignId)
            .IsRequired(false);

        builder.HasIndex(n => n.PublicId).IsUnique();
        builder.HasIndex(n => new { n.OrganizationId, n.Status });
        builder.HasIndex(n => new { n.OrganizationId, n.CreatedAt });

        builder.HasOne(n => n.Organization)
            .WithMany()
            .HasForeignKey(n => n.OrganizationId);

        builder.HasOne(n => n.Organization)
            .WithMany(o => o.Notifications)
            .HasForeignKey(n => n.OrganizationId);
            
        builder.HasMany(n => n.Logs)
            .WithOne(l => l.Notification)
            .HasForeignKey(l => l.NotificationId);
    }
}