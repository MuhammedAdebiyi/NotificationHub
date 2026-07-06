using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NotificationHub.Domain.Entities;

namespace NotificationHub.Infrastructure.Persistence.Configurations;

public class OrgInviteConfiguration : IEntityTypeConfiguration<OrgInvite>
{
    public void Configure(EntityTypeBuilder<OrgInvite> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Email).IsRequired().HasMaxLength(256);
        builder.Property(i => i.Token).IsRequired().HasMaxLength(200);
        builder.Property(i => i.Role).IsRequired().HasMaxLength(20);
        builder.HasIndex(i => i.Token).IsUnique();

        builder.HasOne(i => i.Organization)
            .WithMany()
            .HasForeignKey(i => i.OrganizationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}