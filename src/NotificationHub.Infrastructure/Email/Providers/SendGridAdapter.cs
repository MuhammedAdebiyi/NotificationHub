using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NotificationHub.Application.Abstractions;

namespace NotificationHub.Infrastructure.Email.Providers;

public class SendGridAdapter : IEmailProvider, IEmailProviderDomains
{
    private readonly HttpClient _http;
    private readonly ILogger<SendGridAdapter> _logger;
    private const string Endpoint = "https://api.sendgrid.com/v3/mail/send";
    private const string DomainsEndpoint = "https://api.sendgrid.com/v3/verified_domains";

    public SendGridAdapter(HttpClient http, string apiKey, ILogger<SendGridAdapter> logger)
    {
        _http = http;
        _logger = logger;
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", apiKey);
    }

    public async Task<string> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var payload = new Dictionary<string, object?>
        {
            ["personalizations"] = new[]
            {
                new Dictionary<string, object>
                {
                    ["to"] = new[] { new Dictionary<string, string> { ["email"] = message.To } }
                }
            },
            ["from"] = new Dictionary<string, string> { ["email"] = message.From },
            ["subject"] = message.Subject,
            ["content"] = new[]
            {
                new Dictionary<string, string>
                {
                    ["type"] = "text/html",
                    ["value"] = message.Html
                }
            }
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _logger.LogInformation("Sending email to {To} via SendGrid", message.To);

        var response = await _http.PostAsync(Endpoint, content, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "SendGrid API error {Status}: {Body}",
                (int)response.StatusCode, responseBody);

            throw new HttpRequestException(
                $"SendGrid returned {(int)response.StatusCode}: {responseBody}");
        }

        var emailId = response.Headers.TryGetValues("X-Message-Id", out var values)
            ? values.FirstOrDefault() ?? Guid.NewGuid().ToString()
            : Guid.NewGuid().ToString();

        _logger.LogInformation("SendGrid accepted email id={EmailId} to={To}", emailId, message.To);
        return emailId;
    }

    public async Task<IReadOnlyList<ProviderDomain>> ListDomainsAsync(CancellationToken cancellationToken = default)
    {
        var response = await _http.GetAsync(DomainsEndpoint, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("SendGrid domains API error {Status}: {Body}", (int)response.StatusCode, responseBody);
            return Array.Empty<ProviderDomain>();
        }

        using var doc = JsonDocument.Parse(responseBody);
        var domains = new List<ProviderDomain>();

        foreach (var item in doc.RootElement.EnumerateArray())
        {
            domains.Add(new ProviderDomain(
                Id: item.TryGetProperty("id", out var id) ? id.GetString() ?? string.Empty : string.Empty,
                Domain: item.TryGetProperty("domain", out var d) ? d.GetString() ?? string.Empty : string.Empty,
                Status: item.TryGetProperty("verified", out var v) && v.GetBoolean() ? "verified" : "pending",
                VerifiedAt: item.TryGetProperty("verified_at", out var vt) && vt.ValueKind != JsonValueKind.Null
                    ? DateTime.Parse(vt.GetString()!)
                    : null,
                DeliverabilityReady: item.TryGetProperty("verified", out var vr) && vr.GetBoolean()
            ));
        }

        return domains;
    }
}
