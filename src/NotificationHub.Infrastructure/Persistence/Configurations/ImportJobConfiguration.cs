using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotificationHub.Domain.Entities;

namespace NotificationHub.Infrastructure.Persistence.Configurations;

public class ImportJobConfiguration : IEntityTypeConfiguration<ImportJob>
{
    public void Configure(EntityTypeBuilder<ImportJob> builder)
    {
        builder.HasKey(j => j.Id);

        builder.Property(j => j.TableName).IsRequired().HasMaxLength(200);
        builder.Property(j => j.PrimaryKeyColumn).IsRequired().HasMaxLength(100);
        builder.Property(j => j.EmailColumn).IsRequired().HasMaxLength(100);
        builder.Property(j => j.FirstNameColumn).HasMaxLength(100);
        builder.Property(j => j.LastNameColumn).HasMaxLength(100);
        builder.Property(j => j.WhereClause).HasMaxLength(500);
        builder.Property(j => j.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(j => j.LastCursorId).HasMaxLength(100);

        builder.HasIndex(j => new { j.OrganizationId, j.Status });
        builder.HasIndex(j => j.DataSourceId);
        builder.HasIndex(j => j.CampaignId);
        builder.HasIndex(j => new { j.OrganizationId, j.CreatedAt });

        builder.HasOne(j => j.Organization)
            .WithMany()
            .HasForeignKey(j => j.OrganizationId);

        builder.HasOne(j => j.Campaign)
            .WithMany()
            .HasForeignKey(j => j.CampaignId);
    }
}