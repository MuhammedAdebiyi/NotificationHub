using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NotificationHub.Client;

/// <summary>
/// Official C# SDK for NotificationHub.
/// </summary>
/// <example>
/// var nh = new NotificationHubClient("nhub_live_your_key");
/// var result = await nh.SendAsync(new {
///     recipientEmail = "user@example.com",
///     type = "transactional",
///     channel = "email",
///     payload = "{\"subject\":\"Hello\",\"html\":\"&lt;p&gt;Hi!&lt;/p&gt;\"}"
/// });
/// Console.WriteLine($"Sent: {result.PublicId}");
/// </example>
public class NotificationHubClient
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public NotificationHubClient(string apiKey, string baseUrl = "https://api.notificationhub.space")
    {
        if (string.IsNullOrEmpty(apiKey))
            throw new ArgumentException("API key is required. Get one at https://notificationhub.space/settings", nameof(apiKey));

        _apiKey = apiKey;
        _http = new HttpClient { BaseAddress = new Uri(baseUrl) };
        _http.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
    }

    /// <summary>Send a notification.</summary>
    public async Task<SendResult> SendAsync(object notification, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("/api/v1/notifications", notification, JsonOpts, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new NotificationHubException((int)response.StatusCode, body);

        return JsonSerializer.Deserialize<SendResult>(body, JsonOpts)!;
    }

    /// <summary>Get notification detail.</summary>
    public async Task<NotificationDetail> GetNotificationAsync(string publicId, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"/api/v1/notifications/{publicId}", ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new NotificationHubException((int)response.StatusCode, body);

        return JsonSerializer.Deserialize<NotificationDetail>(body, JsonOpts)!;
    }

    /// <summary>List notifications.</summary>
    public async Task<PaginatedResult<NotificationDetail>> ListNotificationsAsync(
        int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"/api/v1/notifications?page={page}&pageSize={pageSize}", ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new NotificationHubException((int)response.StatusCode, body);

        return JsonSerializer.Deserialize<PaginatedResult<NotificationDetail>>(body, JsonOpts)!;
    }

    /// <summary>Retry a failed notification.</summary>
    public async Task RetryAsync(string publicId, CancellationToken ct = default)
    {
        var response = await _http.PostAsync($"/api/v1/notifications/{publicId}/retry", null, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new NotificationHubException((int)response.StatusCode, body);
    }

    /// <summary>Create a template.</summary>
    public async Task<TemplateResult> CreateTemplateAsync(string name, string subject, string body, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("/api/v1/templates",
            new { name, subject, body }, JsonOpts, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new NotificationHubException((int)response.StatusCode, responseBody);

        return JsonSerializer.Deserialize<TemplateResult>(responseBody, JsonOpts)!;
    }

    /// <summary>Get campaign progress.</summary>
    public async Task<CampaignProgress> GetCampaignProgressAsync(string campaignId, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"/api/v1/campaigns/{campaignId}/progress", ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new NotificationHubException((int)response.StatusCode, body);

        return JsonSerializer.Deserialize<CampaignProgress>(body, JsonOpts)!;
    }
}

public record SendResult(
    [property: JsonPropertyName("publicId")] string PublicId
);

public record NotificationDetail(
    [property: JsonPropertyName("publicId")] string PublicId,
    [property: JsonPropertyName("recipientEmail")] string RecipientEmail,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("channel")] string Channel,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("createdAt")] string CreatedAt,
    [property: JsonPropertyName("processedAt")] string? ProcessedAt,
    [property: JsonPropertyName("lastError")] string? LastError,
    [property: JsonPropertyName("retryCount")] int RetryCount
);

public record TemplateResult(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name
);

public record CampaignProgress(
    [property: JsonPropertyName("totalRecipients")] int TotalRecipients,
    [property: JsonPropertyName("sent")] int Sent,
    [property: JsonPropertyName("pending")] int Pending,
    [property: JsonPropertyName("processing")] int Processing,
    [property: JsonPropertyName("retrying")] int Retrying,
    [property: JsonPropertyName("failed")] int Failed,
    [property: JsonPropertyName("deadLetter")] int DeadLetter,
    [property: JsonPropertyName("progressPercent")] double ProgressPercent
);

public record PaginatedResult<T>(
    [property: JsonPropertyName("items")] List<T> Items,
    [property: JsonPropertyName("totalCount")] int TotalCount,
    [property: JsonPropertyName("pageNumber")] int PageNumber,
    [property: JsonPropertyName("pageSize")] int PageSize
);

public class NotificationHubException : Exception
{
    public int StatusCode { get; }
    public string ResponseBody { get; }

    public NotificationHubException(int statusCode, string responseBody)
        : base($"NotificationHub API error {statusCode}: {responseBody}")
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }
}
