using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotificationHub.Domain.Entities;

namespace NotificationHub.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Type).IsRequired().HasMaxLength(100);
        builder.Property(n => n.Payload).IsRequired();
        builder.Property(n => n.Channel).HasConversion<string>().HasMaxLength(20);
        builder.Property(n => n.Status).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(n => n.PublicId).IsUnique();
        builder.HasIndex(n => n.Status); // workers will query "give me all Pending"
        builder.HasIndex(n => n.UserId);

        builder.HasOne(n => n.Campaign)
            .WithMany()
            .HasForeignKey(n => n.CampaignId)
            .IsRequired(false);

        builder.HasMany(n => n.Logs)
            .WithOne(l => l.Notification)
            .HasForeignKey(l => l.NotificationId);
    }
}