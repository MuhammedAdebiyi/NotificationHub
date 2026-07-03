using FluentValidation;

namespace NotificationHub.Application.Features.Notifications.Commands.CreateNotification;

public class CreateNotificationCommandValidator : AbstractValidator<CreateNotificationCommand>
{
    public CreateNotificationCommandValidator()
    {
        RuleFor(x => x.RecipientEmail)
            .NotEmpty().WithMessage("RecipientEmail is required.")
            .EmailAddress().WithMessage("RecipientEmail must be a valid email address.");

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Notification type is required.")
            .MaximumLength(100).WithMessage("Type must not exceed 100 characters.");

        RuleFor(x => x.Payload)
            .NotEmpty().WithMessage("Payload is required.");

        RuleFor(x => x.Channel)
            .IsInEnum().WithMessage("Invalid notification channel.");
    }
}