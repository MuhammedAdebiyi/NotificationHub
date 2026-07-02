using MediatR;
using NotificationHub.Shared.Abstractions;

namespace NotificationHub.Application.Features.Auth.Commands.Signup;

public record SignupCommand(
    string FullName,
    string Email,
    string Password,
    string ConfirmPassword
) : IRequest<Result<SignupResult>>;

public record SignupResult(string Token, Guid UserId, string Email);