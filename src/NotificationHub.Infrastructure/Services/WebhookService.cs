using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NotificationHub.Application.Abstractions;
using NotificationHub.Domain.Entities;
using NotificationHub.Infrastructure.Persistence;

namespace NotificationHub.Infrastructure.Services;

public class WebhookService : IWebhookService
{
    private readonly AppDbContext _context;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WebhookService> _logger;

    public WebhookService(
        AppDbContext context,
        IHttpClientFactory httpClientFactory,
        ILogger<WebhookService> logger)
    {
        _context = context;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task DispatchAsync(string eventName, Guid organizationId, object payload)
    {
        var hooks = await _context.Webhooks
            .Where(w => w.OrganizationId == organizationId
                && w.IsActive
                && w.Events.Contains(eventName))
            .ToListAsync();

        if (hooks.Count == 0) return;

        var json = JsonSerializer.Serialize(new
        {
            event_type = eventName,
            timestamp = DateTime.UtcNow,
            data = payload,
        });

        var client = _httpClientFactory.CreateClient();
        var tasks = hooks.Select(h => DeliverAsync(client, h, eventName, json));
        await Task.WhenAll(tasks);
    }

    private async Task DeliverAsync(HttpClient client, Webhook hook, string eventName, string json)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(HttpMethod.Post, hook.Url) { Content = content };

            if (!string.IsNullOrEmpty(hook.Secret))
            {
                var signature = ComputeHmac(json, hook.Secret);
                request.Headers.Add("X-NotificationHub-Signature", signature);
                request.Headers.Add("X-Webhook-Signature", signature);
            }

            var response = await client.SendAsync(request);
            sw.Stop();

            _context.WebhookDeliveries.Add(new WebhookDelivery
            {
                WebhookId = hook.Id,
                Event = eventName,
                Payload = json,
                StatusCode = (int)response.StatusCode,
                IsSuccess = response.IsSuccessStatusCode,
                DurationMs = sw.Elapsed.TotalMilliseconds,
            });

            hook.LastTriggeredAt = DateTime.UtcNow;
            hook.LastError = response.IsSuccessStatusCode ? null : $"HTTP {(int)response.StatusCode}";
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "Webhook delivery failed for {HookId}", hook.Id);

            _context.WebhookDeliveries.Add(new WebhookDelivery
            {
                WebhookId = hook.Id,
                Event = eventName,
                Payload = json,
                StatusCode = 0,
                IsSuccess = false,
                Response = ex.Message,
                DurationMs = sw.Elapsed.TotalMilliseconds,
            });

            hook.LastError = ex.Message;
        }

        await _context.SaveChangesAsync();
    }

    private static string ComputeHmac(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
