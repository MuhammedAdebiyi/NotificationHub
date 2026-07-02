using MediatR;
using NotificationHub.Application.Abstractions;
using NotificationHub.Shared.Abstractions;

namespace NotificationHub.Application.Features.Auth.Commands.VerifyEmail;

public class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand, Result<bool>>
{
    private readonly IVerificationTokenRepository _tokenRepository;
    private readonly IClock _clock;

    public VerifyEmailCommandHandler(IVerificationTokenRepository tokenRepository, IClock clock)
    {
        _tokenRepository = tokenRepository;
        _clock = clock;
    }

    public async Task<Result<bool>> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        var token = await _tokenRepository.GetByTokenAsync(request.Token, cancellationToken);

        if (token is null)
            return Result<bool>.Failure("Invalid verification token.");

        if (token.IsUsed)
            return Result<bool>.Failure("This verification link has already been used.");

        if (token.ExpiresAt < _clock.UtcNow)
            return Result<bool>.Failure("This verification link has expired.");

        if (token.User is null)
            return Result<bool>.Failure("User not found for this token.");

        token.User.IsEmailVerified = true;
        token.IsUsed = true;

        await _tokenRepository.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
