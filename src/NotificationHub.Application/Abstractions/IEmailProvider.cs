namespace NotificationHub.Application.Abstractions;

public interface IEmailProvider
{
    string ProviderType { get; }
    Task<string> SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}

public record EmailMessage(
    string From,
    string To,
    string Subject,
    string Html,
    string? Text = null,
    string? ProviderApiKey = null,
    string? UnsubscribeUrl = null,
    // Resolved once at enqueue time and carried through every provider
    // attempt — null means "provider default" (no Reply-To header).
    string? ReplyTo = null
);