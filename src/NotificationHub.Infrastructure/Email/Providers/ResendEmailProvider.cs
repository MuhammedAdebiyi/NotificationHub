using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NotificationHub.Application.Abstractions;

namespace NotificationHub.Infrastructure.Email.Providers;

public class ResendEmailProvider : IEmailProvider
{
    private readonly HttpClient _http;
    private readonly ILogger<ResendEmailProvider> _logger;
    private const string Endpoint = "https://api.resend.com/emails";

    public ResendEmailProvider(
        HttpClient http,
        IConfiguration config,
        ILogger<ResendEmailProvider> logger)
    {
        _http = http;
        _logger = logger;

        var apiKey = config["Resend:ApiKey"]
            ?? throw new InvalidOperationException("Resend:ApiKey is not configured.");

        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);
    }

    public async Task<string> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var payload = new Dictionary<string, object?>
        {
            ["from"] = message.From,
            ["to"] = new[] { message.To },
            ["subject"] = message.Subject,
            ["html"] = message.Html,
        };

        if (message.Text is not null)
            payload["text"] = message.Text;

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _logger.LogInformation("Sending email to {To} via Resend", message.To);

        var response = await _http.PostAsync(Endpoint, content, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Resend API error {Status}: {Body}",
                (int)response.StatusCode, responseBody);

            throw new HttpRequestException(
                $"Resend returned {(int)response.StatusCode}: {responseBody}");
        }

        using var doc = JsonDocument.Parse(responseBody);
        var emailId = doc.RootElement.GetProperty("id").GetString() ?? string.Empty;

        _logger.LogInformation("Resend accepted email id={EmailId} to={To}", emailId, message.To);
        return emailId;
    }
}
