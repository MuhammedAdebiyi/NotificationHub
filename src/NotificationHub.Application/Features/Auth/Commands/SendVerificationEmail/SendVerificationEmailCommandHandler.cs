using MediatR;
using Microsoft.Extensions.Options;
using NotificationHub.Application.Abstractions;
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

        await _emailProvider.SendAsync(new EmailMessage(
            From: "noreply@coursevaultai.app",
            To: user.Email,
            Subject: "Verify your email",
            Html: $"<p>Click to verify your account:</p><p><a href=\"{link}\">{link}</a></p>",
            Text: $"Verify your account: {link}"
        ), cancellationToken);

        return Result<bool>.Success(true);
    }
}