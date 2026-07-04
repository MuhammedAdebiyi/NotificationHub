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
            // Resend verification link — swallowed so login error is clear
            try
            {
                await _mediator.Send(new SendVerificationEmailCommand(user.Id), cancellationToken);
            }
            catch { }

            return Result<LoginResult>.Failure(
                "Email not verified. We've resent the verification link — check your inbox.");
        }

        // Load the user's primary org membership
        var membership = await _organizationRepository.GetMembershipByUserIdAsync(
                user.Id, cancellationToken);

        if (membership is null)
            return Result<LoginResult>.Failure("No organization found for this account.");

        var token = _jwtTokenGenerator.Generate(user, membership.OrganizationId, membership.Role);

        return Result<LoginResult>.Success(
            new LoginResult(token, user.Id, membership.OrganizationId, user.Email, user.FullName));
    }
}