using MediatR;
using NotificationHub.Application.Abstractions;
using NotificationHub.Application.Features.Auth.Commands.SendVerificationEmail;
using NotificationHub.Shared.Abstractions;

namespace NotificationHub.Application.Features.Auth.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<LoginResult>>
{
    private readonly IUserRepository _userRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IMediator _mediator;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IOrganizationRepository organizationRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IMediator mediator)
    {
        _userRepository = userRepository;
        _organizationRepository = organizationRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _mediator = mediator;
    }

    public async Task<Result<LoginResult>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(
            request.Email.ToLowerInvariant(), cancellationToken);

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            return Result<LoginResult>.Failure("Invalid email or password.");

        if (!user.IsEmailVerified)
        {
            try { await _mediator.Send(new SendVerificationEmailCommand(user.Id), cancellationToken); }
            catch { }
            return Result<LoginResult>.Failure(
                "Email not verified. We've resent the verification link — check your inbox.");
        }

        var memberships = await _organizationRepository.GetAllMembershipsAsync(
            user.Id, cancellationToken);

        // No org at all — show the no-org page
        if (memberships.Count == 0)
            return Result<LoginResult>.Success(new LoginResult(
                Token: null,
                UserId: user.Id,
                OrganizationId: null,
                Email: user.Email,
                FullName: user.FullName,
                RequiresOrgSelection: false,
                Organizations: null
            ));

        // Exactly one org — issue JWT directly
        if (memberships.Count == 1)
        {
            var m = memberships[0];
            var token = _jwtTokenGenerator.Generate(user, m.OrganizationId, m.Role);
            return Result<LoginResult>.Success(new LoginResult(
                Token: token,
                UserId: user.Id,
                OrganizationId: m.OrganizationId,
                Email: user.Email,
                FullName: user.FullName,
                RequiresOrgSelection: false,
                Organizations: null
            ));
        }

        // Multiple orgs — return org list, no JWT yet
        var orgOptions = memberships.Select(m => new OrgOption(
            m.OrganizationId,
            m.Organization?.Name ?? "Unknown",
            m.Role
        )).ToList();

        return Result<LoginResult>.Success(new LoginResult(
            Token: null,
            UserId: user.Id,
            OrganizationId: null,
            Email: user.Email,
            FullName: user.FullName,
            RequiresOrgSelection: true,
            Organizations: orgOptions
        ));
    }
}