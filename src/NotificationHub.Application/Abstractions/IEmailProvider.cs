namespace NotificationHub.Application.Abstractions;

public interface IEmailProvider
{
    Task<string> SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}

public record EmailMessage(
    string From,
    string To,
    string Subject,
    string Html,
    string? Text = null
);