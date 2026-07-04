using MediatR;
using NotificationHub.Shared.Abstractions;

namespace NotificationHub.Application.Features.Auth.Commands.VerifyEmail;

public record VerifyEmailCommand(string Token) : IRequest<Result<VerifyEmailResult>>;

public record VerifyEmailResult(string Token, Guid UserId, Guid OrganizationId, string Email);