using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotificationHub.Domain.Entities;

namespace NotificationHub.Infrastructure.Persistence.Configurations;

public class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.HasKey(k => k.Id);
        builder.Property(k => k.Name).IsRequired().HasMaxLength(200);
        builder.Property(k => k.KeyHash).IsRequired();
        builder.Property(k => k.KeyPrefix).IsRequired().HasMaxLength(20);

        builder.HasIndex(k => k.KeyHash).IsUnique();

        builder.HasOne(k => k.CreatedBy)
            .WithMany()
            .HasForeignKey(k => k.CreatedByUserId);
    }
}