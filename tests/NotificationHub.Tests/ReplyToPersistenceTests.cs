using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NotificationHub.Application.Abstractions;
using NotificationHub.Domain.Entities;
using NotificationHub.Domain.Enums;
using NotificationHub.Infrastructure.Messaging.Providers;
using NotificationHub.Infrastructure.Persistence;
using NotificationHub.Infrastructure.Services;
using Xunit;

namespace NotificationHub.Tests;

/// <summary>
/// The value frozen at enqueue time is what actually goes out: retries and
/// DLQ replays re-read the notification row, so a settings change made after
/// the request was accepted must not change the Reply-To of a queued send.
/// Uses a real (SQLite) database because that is the contract being tested.
/// </summary>
public class ReplyToPersistenceTests : IDisposable
{
    private readonly Microsoft.Data.Sqlite.SqliteConnection _connection =
        new("DataSource=:memory:");

    private readonly AppDbContext _context;

    public ReplyToPersistenceTests()
    {
        _connection.Open();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new AppDbContext(options);
        _context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    private sealed class CapturingEmailProviderFactory : IEmailProviderFactory
    {
        public List<EmailMessage> Messages { get; } = new();

        public Task<ProviderSendResult> SendWithFallbackAsync(
            Guid organizationId, EmailMessage message, CancellationToken cancellationToken = default)
        {
            Messages.Add(message);
            return Task.FromResult(new ProviderSendResult(
                "Test", "msg-1", Array.Empty<ProviderSendFailure>()));
        }

        public IEmailProvider GetProvider(Guid organizationId) =>
            throw new NotSupportedException();

        public Task<IEmailProvider> GetProviderAsync(
            Guid organizationId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<DomainsResult> ListDomainsWithHealthAsync(
            Guid organizationId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<EmailProviderInfo>> GetAllProvidersAsync(
            Guid organizationId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IEmailProvider> GetProviderWithFallbackAsync(
            Guid organizationId, string? preferredProviderType = null,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private StubNotificationProvider BuildProvider(CapturingEmailProviderFactory factory)
    {
        var unsubscribe = new Mock<IUnsubscribeTokenService>();
        unsubscribe
            .Setup(s => s.CreateToken(It.IsAny<Guid>(), It.IsAny<string>()))
            .Returns("token");

        return new StubNotificationProvider(
            factory,
            NullLogger<StubNotificationProvider>.Instance,
            _context,
            unsubscribe.Object,
            new ConfigurationBuilder().Build());
    }

    private async Task<(Organization Organization, Notification Notification)> SeedAsync(
        string? replyTo,
        ResolutionSource replyToSource,
        string? senderEmail = null,
        string? senderName = null)
    {
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Example",
            Slug = "example-" + Guid.NewGuid().ToString("N")[..8],
            FromName = "Org Name",
            FromEmail = "org-sender@example.com",
            ReplyToEmail = "org-replies@example.com",
        };

        var notification = new Notification
        {
            OrganizationId = organization.Id,
            RecipientEmail = "to@example.com",
            Type = "transactional",
            Channel = NotificationChannel.Email,
            Payload = "{\"subject\":\"Hi\",\"body\":\"Hello\"}",
            ReplyTo = replyTo,
            ReplyToSource = replyToSource,
            SenderEmail = senderEmail,
            SenderName = senderName,
            SenderSource = senderEmail is null ? ResolutionSource.ProviderDefault : ResolutionSource.Request,
        };

        _context.Organizations.Add(organization);
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        return (organization, notification);
    }

    [Fact]
    public async Task A_queued_send_keeps_its_reply_to_after_the_settings_change()
    {
        var (organization, notification) = await SeedAsync(
            replyTo: "frozen@example.com",
            replyToSource: ResolutionSource.Request,
            senderEmail: "custom@example.com",
            senderName: "Custom Sender");

        // Settings changed between enqueue and the retry.
        organization.ReplyToEmail = "changed@example.com";
        organization.FromEmail = "changed-sender@example.com";
        organization.FromName = "Changed Name";
        await _context.SaveChangesAsync();

        var factory = new CapturingEmailProviderFactory();
        var provider = BuildProvider(factory);

        var sent = await provider.SendAsync(notification);

        Assert.True(sent);
        var message = Assert.Single(factory.Messages);
        Assert.Equal("frozen@example.com", message.ReplyTo);
        Assert.Equal("Custom Sender <custom@example.com>", message.From);
    }

    [Fact]
    public async Task A_send_without_a_stored_reply_to_uses_no_reply_to_header()
    {
        var (_, notification) = await SeedAsync(
            replyTo: null,
            replyToSource: ResolutionSource.ProviderDefault);

        var factory = new CapturingEmailProviderFactory();
        var provider = BuildProvider(factory);

        var sent = await provider.SendAsync(notification);

        Assert.True(sent);
        var message = Assert.Single(factory.Messages);
        Assert.Null(message.ReplyTo);
    }

    [Fact]
    public async Task Rows_created_before_the_feature_fall_back_to_the_org_sender()
    {
        // Legacy row: no stored sender at all (the migration leaves those null).
        var (organization, notification) = await SeedAsync(
            replyTo: null,
            replyToSource: ResolutionSource.ProviderDefault);
        notification.SenderEmail = null;
        notification.SenderName = null;
        await _context.SaveChangesAsync();

        var factory = new CapturingEmailProviderFactory();
        var provider = BuildProvider(factory);

        var sent = await provider.SendAsync(notification);

        Assert.True(sent);
        var message = Assert.Single(factory.Messages);
        Assert.Equal(
            $"{organization.FromName} <{organization.FromEmail}>",
            message.From);
    }

    [Fact]
    public async Task Notification_detail_exposes_the_effective_reply_to_and_its_source()
    {
        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Example",
            Slug = "detail-" + Guid.NewGuid().ToString("N")[..8],
            FromName = "Org Name",
            FromEmail = "org-sender@example.com",
        };

        var template = new Template
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            Name = "Welcome",
            Subject = "Welcome!",
            Body = "<h1>Hi</h1>",
            ReplyTo = "template-replies@example.com",
        };

        var notification = new Notification
        {
            OrganizationId = organization.Id,
            RecipientEmail = "to@example.com",
            Type = "transactional",
            Channel = NotificationChannel.Email,
            Payload = "{\"subject\":\"Hi\",\"body\":\"Hello\"}",
            TemplateId = template.Id,
            ReplyTo = "template-replies@example.com",
            ReplyToSource = ResolutionSource.Template,
            SenderEmail = "news@example.com",
            SenderName = "Example News",
            SenderSource = ResolutionSource.Organization,
        };

        _context.Organizations.Add(organization);
        _context.Templates.Add(template);
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        var service = new NotificationService(
            _context, Mock.Of<INotificationQueue>());

        var detail = await service.GetDetailAsync(
            organization.Id, notification.PublicId);

        Assert.NotNull(detail);
        Assert.Equal("template-replies@example.com", detail!.ReplyTo);
        Assert.Equal("template", detail.ReplyToSource);
        Assert.Equal("news@example.com", detail.SenderEmail);
        Assert.Equal("Example News", detail.SenderName);
        Assert.Equal("organization", detail.SenderSource);
        Assert.Equal(template.Id, detail.TemplateId);
        Assert.Equal("Welcome", detail.TemplateName);
    }

    [Fact]
    public async Task Notification_detail_reports_the_provider_default_for_legacy_rows()
    {
        var (organization, notification) = await SeedAsync(
            replyTo: null,
            replyToSource: ResolutionSource.ProviderDefault);

        var service = new NotificationService(_context, Mock.Of<INotificationQueue>());

        var detail = await service.GetDetailAsync(organization.Id, notification.PublicId);

        Assert.NotNull(detail);
        Assert.Null(detail!.ReplyTo);
        Assert.Equal("default", detail.ReplyToSource);
    }
}
