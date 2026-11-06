using Moq;
using NotificationHub.Application.Abstractions;
using NotificationHub.Application.Features.Notifications.Commands.CreateNotification;
using NotificationHub.Domain.Entities;
using NotificationHub.Domain.Enums;
using Xunit;

namespace NotificationHub.Tests;

/// <summary>
/// Enqueue-time resolution: the effective reply-to/sender is worked out once
/// from request → template → organization → sender, verified against the
/// provider's verified domains, and persisted on the notification row.
/// </summary>
public class CreateNotificationCommandHandlerTests
{
    private static readonly Guid OrgId = Guid.NewGuid();

    private sealed class Harness
    {
        public Mock<INotificationRepository> Notifications { get; } = new();
        public Mock<INotificationQueue> Queue { get; } = new();
        public Mock<IOrganizationRepository> Organizations { get; } = new();
        public Mock<ITemplateRepository> Templates { get; } = new();
        public Mock<IEmailProviderFactory> EmailProviders { get; } = new();

        public Notification? Saved { get; private set; }

        public Harness WithOrganization(Organization organization)
        {
            Organizations
                .Setup(r => r.GetByIdAsync(OrgId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(organization);
            return this;
        }

        public Harness WithTemplate(Template template)
        {
            Templates
                .Setup(r => r.GetByIdAsync(template.Id, OrgId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(template);
            return this;
        }

        public Harness WithVerifiedDomains(params string[] domains)
        {
            var list = domains
                .Select(d => new ProviderDomain(Id: d, Domain: d, DeliverabilityReady: true))
                .ToList();

            EmailProviders
                .Setup(f => f.ListDomainsWithHealthAsync(OrgId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DomainsResult(list, Array.Empty<ProviderHealthCheck>()));
            return this;
        }

        public Harness WithNoVerifiedDomains()
        {
            EmailProviders
                .Setup(f => f.ListDomainsWithHealthAsync(OrgId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DomainsResult(
                    Array.Empty<ProviderDomain>(), Array.Empty<ProviderHealthCheck>()));
            return this;
        }

        public CreateNotificationCommandHandler Build()
        {
            Notifications
                .Setup(r => r.AddAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
                .Callback<Notification, CancellationToken>((n, _) => Saved = n)
                .Returns(Task.CompletedTask);

            Notifications
                .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            Queue
                .Setup(q => q.EnqueueAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            return new CreateNotificationCommandHandler(
                Notifications.Object,
                Queue.Object,
                Organizations.Object,
                Templates.Object,
                EmailProviders.Object);
        }
    }

    private static Organization Organization(
        string? replyToEmail = "org-replies@example.com",
        string fromEmail = "news@example.com",
        string fromName = "Example") =>
        new()
        {
            Id = OrgId,
            Name = "Example",
            Slug = "example",
            ReplyToEmail = replyToEmail,
            FromEmail = fromEmail,
            FromName = fromName,
        };

    private static CreateNotificationCommand Command(
        string? replyTo = null,
        string? senderEmail = null,
        string? senderName = null,
        Guid? templateId = null,
        string? idempotencyKey = null) =>
        new(
            OrganizationId: OrgId,
            UserId: Guid.NewGuid(),
            RecipientEmail: "to@example.com",
            Type: "transactional",
            Channel: NotificationChannel.Email,
            Payload: "{\"subject\":\"Hi\",\"body\":\"Hello\"}",
            IdempotencyKey: idempotencyKey,
            ReplyTo: replyTo,
            SenderEmail: senderEmail,
            SenderName: senderName,
            TemplateId: templateId);

    [Fact]
    public async Task Request_reply_to_is_persisted_with_its_source()
    {
        var harness = new Harness()
            .WithOrganization(Organization())
            .WithVerifiedDomains("example.com");

        var result = await harness.Build().Handle(
            Command(replyTo: "direct@example.com"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(harness.Saved);
        Assert.Equal("direct@example.com", harness.Saved!.ReplyTo);
        Assert.Equal(ResolutionSource.Request, harness.Saved.ReplyToSource);
    }

    [Fact]
    public async Task Template_reply_to_is_used_when_the_request_sets_none()
    {
        var template = new Template
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            Name = "Welcome",
            Subject = "Welcome!",
            Body = "<h1>Hi</h1>",
            ReplyTo = "template-replies@example.com",
        };

        var harness = new Harness()
            .WithOrganization(Organization())
            .WithTemplate(template);

        var result = await harness.Build().Handle(
            Command(templateId: template.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("template-replies@example.com", harness.Saved!.ReplyTo);
        Assert.Equal(ResolutionSource.Template, harness.Saved.ReplyToSource);
        Assert.Equal(template.Id, harness.Saved.TemplateId);
    }

    [Fact]
    public async Task Request_reply_to_wins_over_the_template()
    {
        var template = new Template
        {
            Id = Guid.NewGuid(),
            OrganizationId = OrgId,
            Name = "Welcome",
            Subject = "Welcome!",
            Body = "<h1>Hi</h1>",
            ReplyTo = "template-replies@example.com",
        };

        var harness = new Harness()
            .WithOrganization(Organization())
            .WithTemplate(template)
            .WithVerifiedDomains("example.com");

        var result = await harness.Build().Handle(
            Command(replyTo: "direct@example.com", templateId: template.Id),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("direct@example.com", harness.Saved!.ReplyTo);
        Assert.Equal(ResolutionSource.Request, harness.Saved.ReplyToSource);
    }

    [Fact]
    public async Task Organization_reply_to_is_used_when_nothing_else_sets_one()
    {
        var harness = new Harness().WithOrganization(Organization());

        var result = await harness.Build().Handle(Command(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("org-replies@example.com", harness.Saved!.ReplyTo);
        Assert.Equal(ResolutionSource.Organization, harness.Saved.ReplyToSource);
    }

    [Fact]
    public async Task Organization_sender_email_is_the_reply_to_fallback()
    {
        var harness = new Harness().WithOrganization(
            Organization(replyToEmail: null, fromEmail: "sender@example.com"));

        var result = await harness.Build().Handle(Command(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("sender@example.com", harness.Saved!.ReplyTo);
        Assert.Equal(ResolutionSource.Sender, harness.Saved.ReplyToSource);
    }

    [Fact]
    public async Task Request_sender_is_persisted_together_with_the_org_fallback_sender()
    {
        var harness = new Harness()
            .WithOrganization(Organization(fromEmail: "org@example.com", fromName: "Org Name"))
            .WithVerifiedDomains("example.com");

        var result = await harness.Build().Handle(
            Command(senderEmail: "custom@example.com", senderName: "Custom Name"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("custom@example.com", harness.Saved!.SenderEmail);
        Assert.Equal("Custom Name", harness.Saved.SenderName);
        Assert.Equal(ResolutionSource.Request, harness.Saved.SenderSource);
    }

    [Fact]
    public async Task Organization_sender_is_used_when_the_request_sets_none()
    {
        var harness = new Harness().WithOrganization(
            Organization(fromEmail: "org@example.com", fromName: "Org Name"));

        var result = await harness.Build().Handle(Command(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("org@example.com", harness.Saved!.SenderEmail);
        Assert.Equal("Org Name", harness.Saved.SenderName);
        Assert.Equal(ResolutionSource.Organization, harness.Saved.SenderSource);
    }

    [Fact]
    public async Task Unverified_reply_to_domain_is_rejected_and_names_the_domain()
    {
        var harness = new Harness()
            .WithOrganization(Organization())
            .WithVerifiedDomains("example.com");

        var result = await harness.Build().Handle(
            Command(replyTo: "replies@evil.example"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("unverified_reply_to_domain", result.Code);
        Assert.Contains("evil.example", result.Error);
        Assert.Contains("example.com", result.Error);
        Assert.Null(harness.Saved);
    }

    [Fact]
    public async Task A_subdomain_of_a_verified_domain_is_accepted()
    {
        var harness = new Harness()
            .WithOrganization(Organization())
            .WithVerifiedDomains("example.com");

        var result = await harness.Build().Handle(
            Command(replyTo: "replies@mail.example.com"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("replies@mail.example.com", harness.Saved!.ReplyTo);
    }

    [Fact]
    public async Task Unverified_sender_domain_is_rejected_with_its_own_code()
    {
        var harness = new Harness()
            .WithOrganization(Organization())
            .WithVerifiedDomains("example.com");

        var result = await harness.Build().Handle(
            Command(senderEmail: "news@elsewhere.net"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("unverified_sender_domain", result.Code);
        Assert.Contains("elsewhere.net", result.Error);
    }

    [Fact]
    public async Task Domain_check_is_skipped_when_no_provider_reports_a_verified_domain()
    {
        var harness = new Harness()
            .WithOrganization(Organization())
            .WithNoVerifiedDomains();

        var result = await harness.Build().Handle(
            Command(replyTo: "replies@anything.example"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("replies@anything.example", harness.Saved!.ReplyTo);
    }

    [Fact]
    public async Task Domain_check_is_not_even_called_without_request_addresses()
    {
        var harness = new Harness().WithOrganization(Organization());

        var result = await harness.Build().Handle(Command(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        harness.EmailProviders.Verify(
            f => f.ListDomainsWithHealthAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Unknown_template_id_fails_with_a_stable_code()
    {
        var missingId = Guid.NewGuid();
        var harness = new Harness().WithOrganization(Organization());

        var result = await harness.Build().Handle(
            Command(templateId: missingId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("template_not_found", result.Code);
        Assert.Null(harness.Saved);
    }

    [Fact]
    public async Task Duplicate_idempotency_key_fails_without_a_code_so_it_maps_to_409()
    {
        var harness = new Harness().WithOrganization(Organization());
        harness.Notifications
            .Setup(r => r.IdempotencyKeyExistsAsync(OrgId, "dup-key", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await harness.Build().Handle(
            Command(idempotencyKey: "dup-key"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Code);
        Assert.Contains("idempotency key", result.Error);
        Assert.Null(harness.Saved);
    }
}
