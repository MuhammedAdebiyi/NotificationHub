using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotificationHub.Domain.Entities;

namespace NotificationHub.Infrastructure.Persistence.Configurations;

public class DataSourceConfiguration : IEntityTypeConfiguration<DataSource>
{
    public void Configure(EntityTypeBuilder<DataSource> builder)
    {
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Name).IsRequired().HasMaxLength(200);
        builder.Property(d => d.Type).HasConversion<string>().HasMaxLength(20);
        builder.Property(d => d.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(d => d.Host).HasMaxLength(255);
        builder.Property(d => d.Database).HasMaxLength(255);
        builder.Property(d => d.EncryptedConnectionString).IsRequired();

        builder.HasIndex(d => new { d.OrganizationId, d.Status });
        builder.HasIndex(d => d.OrganizationId);

        builder.HasOne(d => d.Organization)
            .WithMany()
            .HasForeignKey(d => d.OrganizationId);

        builder.HasMany(d => d.ImportJobs)
            .WithOne(j => j.DataSource)
            .HasForeignKey(j => j.DataSourceId);
    }
}