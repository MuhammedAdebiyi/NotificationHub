# Webhooks

Get notified in real-time when events happen in NotificationHub — email delivered, opened, bounced, campaigns completed, and more.

## Quick Start

1. **Register a webhook** via the API or the dashboard (Settings → Webhooks)
2. **Implement a handler** — your server receives POST requests with JSON event payloads
3. **Verify signatures** — use the `X-NotificationHub-Signature` HMAC header to authenticate deliveries
4. **Test** — send a test event to confirm your endpoint is working

## API Reference

The full webhook management API — create, list, update, delete, and test webhooks — is documented in the **[Webhooks API Reference](/api/webhooks)**.

## Supported Events

| Event | Description |
|-------|-------------|
| `notification.sent` | Email delivered to the provider |
| `notification.delivered` | Email confirmed delivered |
| `notification.failed` | Delivery permanently failed |
| `notification.bounced` | Email bounced |
| `notification.retrying` | Automatic retry in progress |
| `campaign.completed` | Campaign finished sending |
| `campaign.paused` | Campaign was paused |

## Example Payload

```json
{
  "id": "evt_xxxxx",
  "event": "notification.delivered",
  "timestamp": "2026-09-13T10:30:00Z",
  "data": {
    "notificationId": "a1b2c3d4-...",
    "recipientEmail": "user@example.com",
    "provider": "resend",
    "providerMessageId": "re_abc123"
  }
}
```

## Signature Verification

Every delivery includes an `X-NotificationHub-Signature` HMAC-SHA256 header. Verify it with your webhook secret to ensure the payload is authentic. See the [API Reference → Signature Verification](/api/webhooks#signature-verification) for code examples.
