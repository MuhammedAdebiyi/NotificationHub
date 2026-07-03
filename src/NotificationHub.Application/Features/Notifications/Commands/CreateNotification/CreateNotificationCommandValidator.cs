using FluentValidation;

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
    }
}