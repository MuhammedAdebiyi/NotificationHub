using MediatR;
using NotificationHub.Application.Abstractions;
using NotificationHub.Shared.Abstractions;

namespace NotificationHub.Application.Features.Auth.Commands.ResetPassword;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result<bool>>
{
    private readonly IVerificationTokenRepository _tokenRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public ResetPasswordCommandHandler(
        IVerificationTokenRepository tokenRepository,
        IUserRepository userRepository,
        IPasswordHasher passwordHasher)
    {
        _tokenRepository = tokenRepository;
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<bool>> Handle(
        ResetPasswordCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Password != request.ConfirmPassword)
            return Result<bool>.Failure("Passwords do not match.");

        var token = await _tokenRepository.GetByTokenAsync(request.Token, cancellationToken);

        if (token is null || token.IsUsed || token.ExpiresAt < DateTime.UtcNow)
            return Result<bool>.Failure("Reset link is invalid or has expired.");

        var user = await _userRepository.GetByIdAsync(token.UserId, cancellationToken);
        if (user is null)
            return Result<bool>.Failure("User not found.");

        user.PasswordHash = _passwordHasher.Hash(request.Password);
        token.IsUsed = true;

        await _userRepository.SaveChangesAsync(cancellationToken);
        await _tokenRepository.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}