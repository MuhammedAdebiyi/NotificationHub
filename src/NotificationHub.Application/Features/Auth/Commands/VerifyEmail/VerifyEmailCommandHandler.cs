using MediatR;
using NotificationHub.Application.Abstractions;
using NotificationHub.Shared.Abstractions;

namespace NotificationHub.Application.Features.Auth.Commands.VerifyEmail;

public class VerifyEmailCommandHandler
    : IRequestHandler<VerifyEmailCommand, Result<VerifyEmailResult>>
{
    private readonly IVerificationTokenRepository _tokenRepository;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IClock _clock;

    public VerifyEmailCommandHandler(
        IVerificationTokenRepository tokenRepository,
        IOrganizationRepository organizationRepository,
        IJwtTokenGenerator jwtTokenGenerator,
        IClock clock)
    {
        _tokenRepository = tokenRepository;
        _organizationRepository = organizationRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
        _clock = clock;
    }

    public async Task<Result<VerifyEmailResult>> Handle(
        VerifyEmailCommand request,
        CancellationToken cancellationToken)
    {
        var token = await _tokenRepository.GetByTokenAsync(request.Token, cancellationToken);

        if (token is null)
            return Result<VerifyEmailResult>.Failure("Invalid verification token.");

        if (token.IsUsed)
            return Result<VerifyEmailResult>.Failure("This verification link has already been used.");

        if (token.ExpiresAt < _clock.UtcNow)
            return Result<VerifyEmailResult>.Failure("This verification link has expired.");

        if (token.User is null)
            return Result<VerifyEmailResult>.Failure("User not found for this token.");

        // Mark verified
        token.User.IsEmailVerified = true;
        token.IsUsed = true;
        await _tokenRepository.SaveChangesAsync(cancellationToken);

        // Get the user's org membership to include orgId + role in JWT
        var membership = await _organizationRepository
            .GetMembershipByUserIdAsync(token.User.Id, cancellationToken);

        if (membership is null)
            return Result<VerifyEmailResult>.Failure("No organization found for this user.");

        var jwtToken = _jwtTokenGenerator.Generate(
            token.User, membership.OrganizationId, membership.Role);

        return Result<VerifyEmailResult>.Success(new VerifyEmailResult(
            jwtToken,
            token.User.Id,
            membership.OrganizationId,
            token.User.Email));
    }
}