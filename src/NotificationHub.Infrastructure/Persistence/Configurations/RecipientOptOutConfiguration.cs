using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotificationHub.Domain.Entities;

namespace NotificationHub.Infrastructure.Persistence.Configurations;

public class RecipientOptOutConfiguration : IEntityTypeConfiguration<RecipientOptOut>
{
    public void Configure(EntityTypeBuilder<RecipientOptOut> builder)
    {
        builder.ToTable("RecipientOptOuts");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Email).HasMaxLength(320);
        builder.HasIndex(o => new { o.OrganizationId, o.Email }).IsUnique();
    }
}
