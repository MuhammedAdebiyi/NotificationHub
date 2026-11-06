using NotificationHub.Application.Features.Notifications.Commands.CreateNotification;
using NotificationHub.Domain.Enums;
using Xunit;

namespace NotificationHub.Tests;

/// <summary>
/// The five-level precedence contract: request → template → organization →
/// sender → provider default. Resolution runs exactly once, at enqueue time.
/// </summary>
public class AddressResolutionTests
{
    [Fact]
    public void Level_1_request_reply_to_wins_over_everything()
    {
        var resolved = AddressResolution.ResolveReplyTo(
            requestReplyTo: "request@example.com",
            templateReplyTo: "template@example.com",
            organizationReplyTo: "org@example.com",
            organizationSenderEmail: "sender@example.com");

        Assert.Equal("request@example.com", resolved.Address);
        Assert.Equal(ResolutionSource.Request, resolved.Source);
    }

    [Fact]
    public void Level_2_template_reply_to_wins_over_organization_and_sender()
    {
        var resolved = AddressResolution.ResolveReplyTo(
            requestReplyTo: null,
            templateReplyTo: "template@example.com",
            organizationReplyTo: "org@example.com",
            organizationSenderEmail: "sender@example.com");

        Assert.Equal("template@example.com", resolved.Address);
        Assert.Equal(ResolutionSource.Template, resolved.Source);
    }

    [Fact]
    public void Level_3_organization_reply_to_wins_over_the_sender_fallback()
    {
        var resolved = AddressResolution.ResolveReplyTo(
            requestReplyTo: null,
            templateReplyTo: null,
            organizationReplyTo: "org@example.com",
            organizationSenderEmail: "sender@example.com");

        Assert.Equal("org@example.com", resolved.Address);
        Assert.Equal(ResolutionSource.Organization, resolved.Source);
    }

    [Fact]
    public void Level_4_sender_email_is_the_reply_to_fallback()
    {
        var resolved = AddressResolution.ResolveReplyTo(
            requestReplyTo: null,
            templateReplyTo: null,
            organizationReplyTo: null,
            organizationSenderEmail: "sender@example.com");

        Assert.Equal("sender@example.com", resolved.Address);
        Assert.Equal(ResolutionSource.Sender, resolved.Source);
    }

    [Fact]
    public void Level_5_provider_default_when_nothing_is_configured()
    {
        var resolved = AddressResolution.ResolveReplyTo(null, null, null, null);

        Assert.Null(resolved.Address);
        Assert.Equal(ResolutionSource.ProviderDefault, resolved.Source);
    }

    [Theory]
    [InlineData("   ")]
    [InlineData("")]
    public void Blank_values_are_treated_as_absent(string blank)
    {
        var resolved = AddressResolution.ResolveReplyTo(
            blank, blank, blank, blank);

        Assert.Null(resolved.Address);
        Assert.Equal(ResolutionSource.ProviderDefault, resolved.Source);
    }

    [Fact]
    public void Resolved_values_are_trimmed()
    {
        var resolved = AddressResolution.ResolveReplyTo(
            "  padded@example.com  ", null, null, null);

        Assert.Equal("padded@example.com", resolved.Address);
    }

    [Fact]
    public void Sender_email_and_name_follow_the_same_precedence()
    {
        var fromRequest = AddressResolution.ResolveSender(
            "req@example.com", "Request Name",
            "tpl@example.com", "Template Name",
            "org@example.com", "Org Name");

        Assert.Equal("req@example.com", fromRequest.Email);
        Assert.Equal("Request Name", fromRequest.Name);
        Assert.Equal(ResolutionSource.Request, fromRequest.Source);

        var fromTemplate = AddressResolution.ResolveSender(
            null, null,
            "tpl@example.com", "Template Name",
            "org@example.com", "Org Name");

        Assert.Equal("tpl@example.com", fromTemplate.Email);
        Assert.Equal("Template Name", fromTemplate.Name);
        Assert.Equal(ResolutionSource.Template, fromTemplate.Source);

        var fromOrganization = AddressResolution.ResolveSender(
            null, null, null, null,
            "org@example.com", "Org Name");

        Assert.Equal("org@example.com", fromOrganization.Email);
        Assert.Equal("Org Name", fromOrganization.Name);
        Assert.Equal(ResolutionSource.Organization, fromOrganization.Source);
    }

    [Fact]
    public void Sender_name_resolves_independently_of_the_email()
    {
        var resolved = AddressResolution.ResolveSender(
            requestEmail: null,
            requestName: "Only A Name",
            templateEmail: null,
            templateName: null,
            organizationEmail: "org@example.com",
            organizationName: "Org Name");

        Assert.Equal("org@example.com", resolved.Email);
        Assert.Equal("Only A Name", resolved.Name);
        // The request contributed part of the identity, so that is the source.
        Assert.Equal(ResolutionSource.Request, resolved.Source);
    }

    [Fact]
    public void Sender_falls_back_to_the_provider_default()
    {
        var resolved = AddressResolution.ResolveSender(
            null, null, null, null, null, null);

        Assert.Null(resolved.Email);
        Assert.Null(resolved.Name);
        Assert.Equal(ResolutionSource.ProviderDefault, resolved.Source);
    }
}
