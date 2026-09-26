using MediatR;
using Microsoft.Extensions.Options;
using NotificationHub.Application.Abstractions;
using NotificationHub.Application.Email;
using NotificationHub.Domain.Entities;
using NotificationHub.Shared.Abstractions;

namespace NotificationHub.Application.Features.Auth.Commands.SendVerificationEmail;

public class SendVerificationEmailCommandHandler
    : IRequestHandler<SendVerificationEmailCommand, Result<bool>>
{
    private readonly IUserRepository _userRepository;
    private readonly IVerificationTokenRepository _tokenRepository;
    private readonly IEmailProvider _emailProvider;
    private readonly ITokenGenerator _tokenGenerator;
    private readonly IClock _clock;
    private readonly VerificationSettings _settings;

    public SendVerificationEmailCommandHandler(
        IUserRepository userRepository,
        IVerificationTokenRepository tokenRepository,
        IEmailProvider emailProvider,
        ITokenGenerator tokenGenerator,
        IClock clock,
        IOptions<VerificationSettings> settings)
    {
        _userRepository = userRepository;
        _tokenRepository = tokenRepository;
        _emailProvider = emailProvider;
        _tokenGenerator = tokenGenerator;
        _clock = clock;
        _settings = settings.Value;
    }

    public async Task<Result<bool>> Handle(SendVerificationEmailCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
            return Result<bool>.Failure("User not found.");

        // Rate limit: one verification email per 60s per user. Applies to every
        // caller (signup, login auto-resend, explicit resend endpoint) so the
        // endpoint can't be used to spam inboxes.
        var latest = await _tokenRepository.GetLatestForUserIdAsync(user.Id, cancellationToken);
        if (latest is not null && _clock.UtcNow - latest.CreatedAt < TimeSpan.FromSeconds(60))
            return Result<bool>.Success(true);

        var token = new VerificationToken
        {
            UserId = user.Id,
            Token = _tokenGenerator.Generate(32),
            ExpiresAt = _clock.UtcNow.AddHours(1),
            IsUsed = false
        };

        await _tokenRepository.AddAsync(token, cancellationToken);
        await _tokenRepository.SaveChangesAsync(cancellationToken);

        var link = $"{_settings.FrontendBaseUrl}/verify-email?token={token.Token}";

        var name = EmailTemplates.Encode(user.FullName);
        var inner = EmailTemplates.H1("Verify your email")
            + EmailTemplates.P($"Hi {name}, welcome to NotificationHub! Please confirm this email address so we know it's really you.")
            + EmailTemplates.Button(link, "Verify email address")
            + EmailTemplates.Muted("This link expires in 1 hour. If you didn't create this account, you can safely ignore this email.");

        await _emailProvider.SendAsync(new EmailMessage(
            From: "NotificationHub <notifications@mail.notificationhub.space>",
            To: user.Email,
            Subject: "Verify your email — NotificationHub",
            Html: EmailTemplates.Wrap("Confirm your NotificationHub account", inner),
            Text: $"Verify your account: {link}"
        ), cancellationToken);

        return Result<bool>.Success(true);
    }
}