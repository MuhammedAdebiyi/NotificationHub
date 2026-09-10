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
    private const string DomainsEndpoint = "https://api.brevo.com/v3/senders/domains";

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
        var response = await _http.GetAsync(DomainsEndpoint, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Brevo domains API error {Status}: {Body}", (int)response.StatusCode, responseBody);
            return Array.Empty<ProviderDomain>();
        }

        using var doc = JsonDocument.Parse(responseBody);
        var domains = new List<ProviderDomain>();

        if (doc.RootElement.TryGetProperty("domains", out var domainsArray))
        {
            foreach (var item in domainsArray.EnumerateArray())
            {
                var domainName = item.TryGetProperty("domain_name", out var dn) ? dn.GetString() ?? string.Empty : string.Empty;
                if (string.IsNullOrEmpty(domainName)) continue;

                var verified = item.TryGetProperty("verified", out var v) && v.GetBoolean();
                var authenticated = item.TryGetProperty("authenticated", out var a) && a.GetBoolean();

                domains.Add(new ProviderDomain(
                    Id: item.TryGetProperty("id", out var id) ? id.GetString() ?? domainName : domainName,
                    Domain: domainName,
                    Status: verified && authenticated ? "verified" : verified ? "pending" : "unverified",
                    VerifiedAt: null,
                    DeliverabilityReady: verified && authenticated
                ));
            }
        }

        return domains;
    }
}
