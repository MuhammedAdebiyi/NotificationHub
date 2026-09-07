using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using NotificationHub.Application.Abstractions;

namespace NotificationHub.Infrastructure.Email.Providers;

public class SendByteAdapter : IEmailProvider
{
    private readonly HttpClient _http;
    private readonly ILogger<SendByteAdapter> _logger;
    private const string Endpoint = "https://api.sendbyte.africa/v1/emails";

    public SendByteAdapter(HttpClient http, string apiKey, ILogger<SendByteAdapter> logger)
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
            ["from"] = message.From,
            ["to"] = new[] { message.To },
            ["subject"] = message.Subject,
            ["html"] = message.Html,
        };

        if (message.Text is not null)
            payload["text"] = message.Text;

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        _logger.LogInformation("Sending email to {To} via SendByte", message.To);

        var response = await _http.PostAsync(Endpoint, content, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "SendByte API error {Status}: {Body}",
                (int)response.StatusCode, responseBody);

            throw new HttpRequestException(
                $"SendByte returned {(int)response.StatusCode}: {responseBody}");
        }

        using var doc = JsonDocument.Parse(responseBody);
        var emailId = doc.RootElement.GetProperty("id").GetString() ?? string.Empty;

        _logger.LogInformation("SendByte accepted email id={EmailId} to={To}", emailId, message.To);
        return emailId;
    }
}
