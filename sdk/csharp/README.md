# NotificationHub C# SDK

Official C# SDK for NotificationHub — multi-provider email notification platform

## Installation

```bash
dotnet add package NotificationHub.SDK
```

## Quick Start

```csharp
using NotificationHub.SDK;

var client = new NotificationHubClient(
    apiKey: "your-api-key",
    baseUrl: "https://api.notificationhub.space"
);

var result = await client.Notifications.SendAsync(new SendRequest
{
    To = "user@example.com",
    TemplateId = "welcome-email",
    Variables = new Dictionary<string, string>
    {
        { "name", "John Doe" },
        { "login_url", "https://app.example.com/login" },
    },
});

Console.WriteLine($"Notification sent: {result.Id}");
```

## Features

- Send notifications
- Retry failed notifications
- Create/list/delete templates
- Campaign progress & send

## Links

[API Docs](https://notificationhub.space/docs) | [Swagger Explorer](https://api.notificationhub.space/docs)
