using MediatR;
using Microsoft.Extensions.Options;
using NotificationHub.Application.Abstractions;
using NotificationHub.Application.Email;
using NotificationHub.Domain.Entities;
using NotificationHub.Shared.Abstractions;

namespace NotificationHub.Application.Features.Auth.Commands.ForgotPassword;

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result<bool>>
{
    private readonly IUserRepository _userRepository;
    private readonly IEmailProvider _emailProvider;
    private readonly IVerificationTokenRepository _tokenRepository;
    private readonly VerificationSettings _settings;

    public ForgotPasswordCommandHandler(
        IUserRepository userRepository,
        IEmailProvider emailProvider,
        IVerificationTokenRepository tokenRepository,
        IOptions<VerificationSettings> settings)
    {
        _userRepository = userRepository;
        _emailProvider = emailProvider;
        _tokenRepository = tokenRepository;
        _settings = settings.Value;
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

        var resetLink = $"{_settings.FrontendBaseUrl}/reset-password?token={token.Token}";

        var name = EmailTemplates.Encode(user.FullName);
        var inner = EmailTemplates.H1("Reset your password")
            + EmailTemplates.P($"Hi {name}, we received a request to reset your NotificationHub password.")
            + EmailTemplates.Button(resetLink, "Reset password")
            + EmailTemplates.Muted("This link expires in 1 hour. If you didn't request this, you can safely ignore this email — your password won't change.");

        await _emailProvider.SendAsync(new EmailMessage(
            From: "NotificationHub <notifications@mail.notificationhub.space>",
            To: user.Email,
            Subject: "Reset your NotificationHub password",
            Html: EmailTemplates.Wrap("Reset your NotificationHub password", inner),
            Text: $"Hi {user.FullName}, reset your password here: {resetLink} (expires in 1 hour)"
        ), cancellationToken);

        return Result<bool>.Success(true);
    }
}