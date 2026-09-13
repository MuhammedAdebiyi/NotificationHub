---
title: C# SDK
---

# C# SDK

Official .NET SDK for [NotificationHub](https://notificationhub.space).

## Installation

```bash
dotnet add package NotificationHub.Client
```

## Quick Start

```csharp
using NotificationHub.Client;

var nh = new NotificationHubClient("nhub_live_your_key_here");

var result = await nh.SendAsync(new
{
    recipientEmail = "user@example.com",
    type = "transactional",
    channel = "email",
    payload = "{\"subject\":\"Welcome!\",\"html\":\"<h1>Hello!</h1><p>Thanks for signing up.</p>\"}"
});

Console.WriteLine($"Sent: {result.PublicId}");
```

## Setup

```csharp
// With custom base URL
var nh = new NotificationHubClient(
    "nhub_live_your_key",
    baseUrl: "https://custom-api.example.com"
);

// With cancellation token
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
var result = await nh.SendAsync(notification, cts.Token);
```

## API Reference

### SendNotification

Sends a notification to a recipient.

```csharp
var result = await nh.SendAsync(new
{
    recipientEmail = "user@example.com",
    type = "transactional",
    channel = "email",
    payload = "{\"subject\":\"Hello\",\"html\":\"<p>Hi there</p>\"}"
});
```

| Parameter | Type | Required | Description |
|---|---|---|---|
| `recipientEmail` | `string` | Yes | Recipient email address |
| `type` | `string` | Yes | `transactional`, `marketing`, or `system` |
| `channel` | `string` | Yes | `email`, `sms`, `push`, or `webhook` |
| `payload` | `string` | Yes | JSON-encoded message payload |

**Returns:** `SendResult` with `PublicId string`

### GetNotification

Retrieves a notification by its public ID.

```csharp
var notification = await nh.GetNotificationAsync("notif_abc123");
Console.WriteLine(notification.Status); // "sent", "pending", "failed", etc.
```

### ListNotifications

Lists notifications with pagination.

```csharp
var notifications = await nh.ListNotificationsAsync(page: 1, pageSize: 20);
foreach (var n in notifications.Items)
{
    Console.WriteLine($"{n.PublicId}: {n.Status}");
}
```

### RetryNotification

Retries a failed notification.

```csharp
await nh.RetryAsync("notif_abc123");
```

### CreateTemplate

Creates a new email template.

```csharp
var template = await nh.CreateTemplateAsync(
    "Welcome Email",
    "Welcome {{name}}!",
    "<h1>Hello {{name}}</h1><p>Your account is ready.</p>"
);
Console.WriteLine(template.Id);
```

### GetCampaignProgress

Gets real-time campaign delivery progress.

```csharp
var progress = await nh.GetCampaignProgressAsync("camp_xyz789");
Console.WriteLine($"{progress.ProgressPercent:F1}% sent ({progress.Sent}/{progress.TotalRecipients})");
```

## Error Handling

```csharp
try
{
    var result = await nh.SendAsync(notification);
}
catch (NotificationHubException ex)
{
    Console.WriteLine($"API error {ex.StatusCode}: {ex.ResponseBody}");
}
```

## Links

- [GitHub SDK](https://github.com/MuhammedAdebiyi/NotificationHub/tree/main/sdk/csharp)
- [API Reference](/api/notifications)
- [Swagger Explorer](https://api.notificationhub.space/docs)
