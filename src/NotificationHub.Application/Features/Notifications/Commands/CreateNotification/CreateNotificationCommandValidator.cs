using FluentValidation;
using NotificationHub.Application.Validation;

namespace NotificationHub.Application.Features.Notifications.Commands.CreateNotification;
public class CreateNotificationCommandValidator
    : AbstractValidator<CreateNotificationCommand>
{
    public CreateNotificationCommandValidator()
    {
        RuleFor(x => x.OrganizationId)
            .NotEmpty().WithMessage("OrganizationId is required.");

        RuleFor(x => x.RecipientEmail)
            .NotEmpty().WithMessage("RecipientEmail is required.")
            .EmailAddress().WithMessage("RecipientEmail must be a valid email.");

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Type is required.")
            .MaximumLength(100);

        RuleFor(x => x.Payload)
            .NotEmpty().WithMessage("Payload is required.");

        // Reply-to: exactly one bare address. Providers take a single
        // reply-to, so lists ("a@b.c, d@e.f") and display names are rejected
        // here with a stable code instead of failing at the provider.
        RuleFor(x => x.ReplyTo)
            .Must(value => string.IsNullOrWhiteSpace(value) || EmailAddressRules.IsSingleEmailAddress(value))
            .WithMessage("ReplyTo must be a single email address.")
            .WithErrorCode(NotificationErrorCodes.InvalidReplyTo)
            .When(x => !string.IsNullOrWhiteSpace(x.ReplyTo));

        RuleFor(x => x.SenderEmail)
            .Must(value => string.IsNullOrWhiteSpace(value) || EmailAddressRules.IsSingleEmailAddress(value))
            .WithMessage("SenderEmail must be a single email address.")
            .WithErrorCode(NotificationErrorCodes.InvalidSenderEmail)
            .When(x => !string.IsNullOrWhiteSpace(x.SenderEmail));

        RuleFor(x => x.SenderName)
            .MaximumLength(100)
            .WithErrorCode(NotificationErrorCodes.InvalidSenderName)
            .When(x => !string.IsNullOrWhiteSpace(x.SenderName));
    }
}
