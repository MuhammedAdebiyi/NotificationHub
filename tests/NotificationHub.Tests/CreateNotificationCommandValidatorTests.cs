using FluentValidation.Results;
using NotificationHub.Application.Features.Notifications.Commands.CreateNotification;
using NotificationHub.Domain.Enums;
using Xunit;

namespace NotificationHub.Tests;

public class CreateNotificationCommandValidatorTests
{
    private readonly CreateNotificationCommandValidator _validator = new();

    private static CreateNotificationCommand Command(
        string? replyTo = null,
        string? senderEmail = null,
        string? senderName = null,
        string? idempotencyKey = null) =>
        new(
            OrganizationId: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            RecipientEmail: "to@example.com",
            Type: "transactional",
            Channel: NotificationChannel.Email,
            Payload: "{\"subject\":\"Hi\",\"body\":\"Hello\"}",
            IdempotencyKey: idempotencyKey,
            ReplyTo: replyTo,
            SenderEmail: senderEmail,
            SenderName: senderName);

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("a@example.com, b@example.com")]
    [InlineData("a@example.com;b@example.com")]
    [InlineData("Support <a@example.com>")]
    [InlineData("a@example.com b@example.com")]
    public void Malformed_reply_to_is_rejected_with_a_stable_code(string replyTo)
    {
        var result = _validator.Validate(Command(replyTo: replyTo));

        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal("invalid_reply_to", error.ErrorCode);
    }

    [Theory]
    [InlineData("support@example.com")]
    [InlineData("support+tag@example.co.uk")]
    [InlineData("  padded@example.com  ")]
    public void A_single_reply_to_address_is_accepted(string replyTo)
    {
        var result = _validator.Validate(Command(replyTo: replyTo));

        Assert.True(result.IsValid, string.Join("; ", result.Errors.Select(e => e.ErrorMessage)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void An_absent_reply_to_is_accepted(string? replyTo)
    {
        var result = _validator.Validate(Command(replyTo: replyTo));

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("nope")]
    [InlineData("a@example.com, b@example.com")]
    public void Malformed_sender_email_is_rejected_with_a_stable_code(string senderEmail)
    {
        var result = _validator.Validate(Command(senderEmail: senderEmail));

        Assert.False(result.IsValid);
        var error = Assert.Single(result.Errors);
        Assert.Equal("invalid_sender_email", error.ErrorCode);
    }

    [Fact]
    public void Valid_sender_email_and_name_pass()
    {
        var result = _validator.Validate(Command(
            senderEmail: "news@example.com",
            senderName: "Example News"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void An_over_long_sender_name_is_rejected()
    {
        var result = _validator.Validate(Command(senderName: new string('x', 101)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorCode == "invalid_sender_name");
    }

    [Fact]
    public void Multiple_failures_keep_their_own_codes()
    {
        var result = _validator.Validate(Command(
            replyTo: "nope",
            senderEmail: "also-nope"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorCode == "invalid_reply_to");
        Assert.Contains(result.Errors, e => e.ErrorCode == "invalid_sender_email");
    }

    [Fact]
    public void Core_fields_are_still_validated()
    {
        var result = _validator.Validate(new CreateNotificationCommand(
            Guid.Empty, Guid.Empty, "not-an-email", "", NotificationChannel.Email, "", null));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("RecipientEmail"));
    }
}
