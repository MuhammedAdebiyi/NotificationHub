using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NotificationHub.Application.Abstractions;

namespace NotificationHub.Infrastructure.Email.Providers;

public class BrevoAdapter : IEmailProvider, IEmailProviderDomains
{
    private readonly HttpClient _http;
    private readonly ILogger<BrevoAdapter> _logger;
    private const string Endpoint = "https://api.brevo.com/v3/smtp/email";
    private const string SendersEndpoint = "https://api.brevo.com/v3/smtp/senders";

    public BrevoAdapter(HttpClient http, string apiKey, ILogger<BrevoAdapter> logger)
    {
        _http = http;
        _logger = logger;
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("api-key", apiKey);
    }

    public async Task<string> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var payload = new Dictionary<string, object?>
        {
            ["sender"] = new Dictionary<string, string> { ["email"] = message.From },
            ["to"] = new[]
            {
                new Dictionary<string, string> { ["email"] = message.To }
            },
            ["subject"] = message.Subject,
            ["htmlContent"] = message.Html,
        };

        if (message.Text is not null)
            payload["textContent"] = message.Text;

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _logger.LogInformation("Sending email to {To} via Brevo", message.To);

        var response = await _http.PostAsync(Endpoint, content, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Brevo API error {Status}: {Body}",
                (int)response.StatusCode, responseBody);

            throw new HttpRequestException(
                $"Brevo returned {(int)response.StatusCode}: {responseBody}");
        }

        using var doc = JsonDocument.Parse(responseBody);
        var emailId = doc.RootElement.GetProperty("messageId").GetString() ?? string.Empty;

        _logger.LogInformation("Brevo accepted email id={EmailId} to={To}", emailId, message.To);
        return emailId;
    }

    public async Task<IReadOnlyList<ProviderDomain>> ListDomainsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _http.GetAsync(SendersEndpoint, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Brevo senders API error {Status}: {Body}", (int)response.StatusCode, responseBody);
            return Array.Empty<ProviderDomain>();
        }

        using var doc = JsonDocument.Parse(responseBody);
        var senders = doc.RootElement.GetProperty("senders");
        var domains = new Dictionary<string, ProviderDomain>();

        foreach (var item in senders.EnumerateArray())
        {
            var email = item.TryGetProperty("email", out var e) ? e.GetString() ?? string.Empty : string.Empty;
            var domain = string.IsNullOrEmpty(email) ? string.Empty : email.Split('@').LastOrDefault() ?? string.Empty;

            if (string.IsNullOrEmpty(domain) || domains.ContainsKey(domain))
                continue;

            var quality = item.TryGetProperty("quality", out var q) ? q.GetString() ?? "unknown" : "unknown";
            domains[domain] = new ProviderDomain(
                Id: domain,
                Domain: domain,
                Status: quality == "valid" ? "verified" : quality == "pending" ? "pending" : "unknown",
                VerifiedAt: null,
                DeliverabilityReady: quality == "valid"
            );
        }

        return domains.Values.ToList();
    }
}
