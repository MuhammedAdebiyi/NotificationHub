using Microsoft.EntityFrameworkCore;
using NotificationHub.Domain.Entities;

namespace NotificationHub.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<OrganizationMember> OrganizationMembers => Set<OrganizationMember>();
    public DbSet<OrgInvite> OrgInvites => Set<OrgInvite>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();
    public DbSet<IdempotencyKey> IdempotencyKeys => Set<IdempotencyKey>();
    public DbSet<Template> Templates => Set<Template>();
    public DbSet<DataSource> DataSources => Set<DataSource>();
    public DbSet<ImportJob> ImportJobs => Set<ImportJob>();
    public DbSet<Campaign> Campaigns => Set<Campaign>();
    public DbSet<CampaignRecipient> CampaignRecipients => Set<CampaignRecipient>();
    public DbSet<VerificationToken> VerificationTokens => Set<VerificationToken>();
   


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}