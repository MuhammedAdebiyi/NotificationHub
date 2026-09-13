---
title: Go SDK
---

# Go SDK

Official Go SDK for [NotificationHub](https://notificationhub.space).

## Installation

```bash
go get github.com/MuhammedAdebiyi/NotificationHub/sdk/go
```

## Quick Start

```go
package main

import (
	"context"
	"fmt"
	"log"

	nh "github.com/MuhammedAdebiyi/NotificationHub/sdk/go"
)

func main() {
	client := nh.NewClient("nhub_live_your_key_here")

	result, err := client.Send(context.Background(), &nh.SendParams{
		RecipientEmail: "user@example.com",
		Type:           "transactional",
		Channel:        "email",
		Payload: map[string]interface{}{
			"subject": "Welcome!",
			"html":    "<h1>Hello!</h1><p>Thanks for signing up.</p>",
		},
	})
	if err != nil {
		log.Fatal(err)
	}
	fmt.Println("Sent:", result.PublicID)
}
```

## Configuration

```go
client := nh.NewClient(
	"nhub_live_your_key",
	nh.WithBaseURL("https://custom-api.example.com"),
	nh.WithHTTPClient(&http.Client{Timeout: 10 * time.Second}),
)
```

## API Reference

### SendNotification

Sends a notification to a recipient.

```go
result, err := client.Send(ctx, &nh.SendParams{
	RecipientEmail: "user@example.com",
	Type:           "transactional",
	Channel:        "email",
	Payload: map[string]interface{}{
		"subject": "Hello",
		"html":    "<p>Hi there</p>",
	},
})
```

| Parameter | Type | Required | Description |
|---|---|---|---|
| `RecipientEmail` | `string` | Yes | Recipient email address |
| `Type` | `string` | Yes | `transactional`, `marketing`, or `system` |
| `Channel` | `string` | Yes | `email`, `sms`, `push`, or `webhook` |
| `Payload` | `interface{}` | Yes | Message payload (string or map) |

**Returns:** `*SendResult` with `PublicID string`

### GetNotification

Retrieves a notification by its public ID.

```go
notification, err := client.GetNotification(ctx, "notif_abc123")
fmt.Println(notification.Status) // "sent", "pending", "failed", etc.
```

### ListNotifications

Lists notifications with pagination.

```go
notifications, err := client.ListNotifications(ctx, 1, 20)
for _, n := range notifications.Items {
	fmt.Println(n.PublicID, n.Status)
}
```

### RetryNotification

Retries a failed notification.

```go
err := client.RetryNotification(ctx, "notif_abc123")
```

### CreateTemplate

Creates a new email template.

```go
template, err := client.CreateTemplate(ctx,
	"Welcome Email",
	"Welcome {{name}}!",
	"<h1>Hello {{name}}</h1><p>Your account is ready.</p>",
)
fmt.Println(template.ID)
```

### GetCampaignProgress

Gets real-time campaign delivery progress.

```go
progress, err := client.GetCampaignProgress(ctx, "camp_xyz789")
fmt.Printf("%.1f%% sent (%d/%d)\n",
	progress.ProgressPercent, progress.Sent, progress.TotalRecipients)
```

## Error Handling

```go
result, err := client.Send(ctx, params)
if err != nil {
	fmt.Printf("API error: %v\n", err)
}
```

## Links

- [GitHub SDK](https://github.com/MuhammedAdebiyi/NotificationHub/tree/main/sdk/go)
- [API Reference](/api/notifications)
- [Swagger Explorer](https://api.notificationhub.space/docs)
