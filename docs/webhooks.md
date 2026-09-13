# Webhooks

::: info Coming Soon
Webhooks are under development. This page describes the planned API.
:::

Webhooks let your application receive real-time notifications when events happen in NotificationHub (email delivered, opened, bounced, etc.).

## Planned Events

| Event | Description |
|-------|-------------|
| `notification.sent` | Email successfully delivered |
| `notification.failed` | Email permanently failed |
| `notification.opened` | Recipient opened the email |
| `notification.bounced` | Email bounced |
| `campaign.completed` | Campaign finished sending |
| `campaign.paused` | Campaign was paused |

## Webhook Payload

```json
{
  "event": "notification.sent",
  "timestamp": "2026-09-10T12:00:02Z",
  "data": {
    "notificationId": "a1b2c3d4-...",
    "recipientEmail": "user@example.com",
    "provider": "resend",
    "providerMessageId": "re_abc123"
  }
}
```

## Setup

Configure your webhook URL in **Settings → Webhooks** (coming soon).

Each webhook delivery includes:
- `X-NotificationHub-Signature` header for HMAC verification
- Automatic retries on failure (3 attempts with exponential backoff)
