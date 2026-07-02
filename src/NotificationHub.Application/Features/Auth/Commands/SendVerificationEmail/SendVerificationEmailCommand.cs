using MediatR;
using NotificationHub.Shared.Abstractions;

namespace NotificationHub.Application.Features.Auth.Commands.SendVerificationEmail;

public record SendVerificationEmailCommand(Guid UserId) : IRequest<Result<bool>>;