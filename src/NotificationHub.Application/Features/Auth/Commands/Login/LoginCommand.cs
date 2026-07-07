using MediatR;
using NotificationHub.Shared.Abstractions;

namespace NotificationHub.Application.Features.Auth.Commands.Login;

public record LoginCommand(string Email, string Password) : IRequest<Result<LoginResult>>;

public record OrgOption(Guid OrganizationId, string OrgName, string Role);

public record LoginResult(
    string? Token,           // null when multi-org picker needed
    Guid UserId,
    Guid? OrganizationId,    // null when multi-org picker needed
    string Email,
    string FullName,
    bool RequiresOrgSelection,
    IReadOnlyList<OrgOption>? Organizations  // populated when RequiresOrgSelection = true
);