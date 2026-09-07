using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using NotificationHub.Application.Abstractions;

namespace NotificationHub.Infrastructure.Email.Providers;

/// <summary>
/// Generic SMTP adapter for any provider that supports SMTP.
/// Credentials format: host|port|username|password (pipe-delimited)
/// </summary>
public class SmtpAdapter : IEmailProvider
{
    private readonly string _host;
    private readonly int _port;
    private readonly string _username;
    private readonly string _password;
    private readonly ILogger<SmtpAdapter> _logger;

    public SmtpAdapter(string credentials, ILogger<SmtpAdapter> logger)
    {
        _logger = logger;
        var parts = credentials.Split('|');
        if (parts is not { Length: 4 })
            throw new InvalidOperationException(
                "SMTP credentials must be in format: host|port|username|password");

        _host = parts[0];
        _port = int.Parse(parts[1]);
        _username = parts[2];
        _password = parts[3];
    }

    public async Task<string> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        using var client = new SmtpClient(_host, _port)
        {
            Credentials = new NetworkCredential(_username, _password),
            EnableSsl = true,
        };

        var mailMessage = new MailMessage
        {
            From = new MailAddress(message.From),
            Subject = message.Subject,
            Body = message.Html ?? message.Text ?? string.Empty,
            IsBodyHtml = message.Html is not null,
        };
        mailMessage.To.Add(message.To);

        _logger.LogInformation("Sending email to {To} via SMTP ({Host}:{Port})", message.To, _host, _port);

        await client.SendMailAsync(mailMessage, cancellationToken);

        var emailId = Guid.NewGuid().ToString();
        _logger.LogInformation("SMTP email sent id={EmailId} to={To}", emailId, message.To);
        return emailId;
    }
}
