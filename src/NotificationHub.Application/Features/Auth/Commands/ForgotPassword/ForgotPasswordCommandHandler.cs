using MediatR;
using NotificationHub.Application.Abstractions;
using NotificationHub.Domain.Entities;
using NotificationHub.Shared.Abstractions;

namespace NotificationHub.Application.Features.Auth.Commands.ForgotPassword;

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result<bool>>
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailProvider _emailProvider;
    private readonly IVerificationTokenRepository _tokenRepository;

    public ForgotPasswordCommandHandler(
        IUserRepository userRepository,
        IEmailProvider emailProvider,
        IVerificationTokenRepository tokenRepository)
    {
        _userRepository = userRepository;
        _emailProvider = emailProvider;
        _tokenRepository = tokenRepository;
    }

    public async Task<Result<bool>> Handle(
        ForgotPasswordCommand request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(
            request.Email.ToLowerInvariant(), cancellationToken);

        // Always return success — never reveal whether email exists
        if (user is null)
            return Result<bool>.Success(true);

        var token = new VerificationToken
        {
            UserId = user.Id,
            Token = Guid.NewGuid().ToString("N"),
            ExpiresAt = DateTime.UtcNow.AddHours(1),
        };

        await _tokenRepository.AddAsync(token, cancellationToken);
        await _tokenRepository.SaveChangesAsync(cancellationToken);

        var resetLink = $"http://localhost:5173/reset-password?token={token.Token}";

    await _emailProvider.SendAsync(new EmailMessage(
        From: "noreply@coursevaultai.app",
        To: user.Email,
        Subject: "Reset your NotificationHub password",
        Html: $"""
            <p>Hi {user.FullName},</p>
            <p>Click the link below to reset your password. This link expires in 1 hour.</p>
            <p><a href="{resetLink}">Reset Password</a></p>
            <p>If you didn't request this, ignore this email.</p>
        """,
        Text: $"Hi {user.FullName}, reset your password here: {resetLink} (expires in 1 hour)"
    ), cancellationToken);

        return Result<bool>.Success(true);
    }
}