using MediatR;
using NotificationHub.Shared.Abstractions;

namespace NotificationHub.Application.Features.Auth.Commands.ResendVerification;

public record ResendVerificationCommand(string Email) : IRequest<Result>;
