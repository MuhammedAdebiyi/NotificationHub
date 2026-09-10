using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotificationHub.Domain.Entities;

namespace NotificationHub.Infrastructure.Persistence.Configurations;

public class EmailProviderConfigConfiguration : IEntityTypeConfiguration<EmailProviderConfig>
{
    public void Configure(EntityTypeBuilder<EmailProviderConfig> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.ProviderType).IsRequired().HasMaxLength(50);
        builder.Property(c => c.EncryptedApiKey).IsRequired();
        builder.Property(c => c.SenderEmail).HasMaxLength(200);

        builder.HasIndex(c => new { c.OrganizationId, c.ProviderType }).IsUnique();
        builder.HasIndex(c => c.OrganizationId);

        builder.HasOne(c => c.Organization)
            .WithMany()
            .HasForeignKey(c => c.OrganizationId);
    }
}
