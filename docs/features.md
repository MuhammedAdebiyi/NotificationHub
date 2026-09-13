---
title: Features
---

# Features

Everything NotificationHub offers out of the box.

## Multi-Provider Email Delivery

Send transactional and marketing emails through the provider that works best for you — with automatic failover.

| Provider | Type | Setup |
|----------|------|-------|
| **Resend** | Transactional | API key |
| **SendByte** | Transactional | API key |
| **SendGrid** | Transactional / Marketing | API key |
| **Brevo** | Transactional / Marketing | API key |
| **SMTP** | Any | Host, port, user, password |

When you configure multiple providers, NotificationHub tries the default first. If it fails (timeout, 5xx, rate limit), the next provider is tried automatically. No code changes required.

```json
{
  "providers": [
    { "type": "resend", "isDefault": true },
    { "type": "sendbyte", "isDefault": false },
    { "type": "smtp", "isDefault": false }
  ]
}
```

Each provider reports its own health status — you can see which providers are connected, verified, and healthy from the dashboard.

## Campaign Engine

Run bulk email campaigns to thousands of recipients with scheduling, pause/resume, and real-time progress tracking.

- **Schedule for later** — pick a date/time, campaigns send automatically
- **Pause and resume** — stop a campaign mid-send, resume when ready
- **Real-time progress** — track sent, pending, failed, and dead-letter counts
- **Import from database** — add recipients from your own data source
- **Automatic retry** — failed emails are retried with exponential backoff

```bash
# Create a campaign
curl -X POST https://api.notificationhub.space/api/v1/campaigns \
  -H "X-Api-Key: nhub_live_your_key" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "September Product Update",
    "subject": "What's new in September",
    "body": "<h1>Hello!</h1><p>Here's what we shipped this month.</p>"
  }'

# Add recipients
curl -X POST https://api.notificationhub.space/api/v1/campaigns/cmp_xxxxx/recipients \
  -H "X-Api-Key: nhub_live_your_key" \
  -d '{ "emails": ["alice@example.com", "bob@example.com"] }'

# Send it
curl -X POST https://api.notificationhub.space/api/v1/campaigns/cmp_xxxxx/send \
  -H "X-Api-Key: nhub_live_your_key"
```

## Email Templates

Reusable email templates with `{{variable}}` placeholders. Manage via API or the visual editor in the dashboard.

```json
{
  "name": "order-confirmation",
  "subject": "Order #{{orderId}} confirmed",
  "body": "<h1>Hi {{firstName}}!</h1><p>Your order #{{orderId}} is confirmed. Total: ${{amount}}</p>"
}
```

When sending, pass the variable values:

```json
{
  "recipientEmail": "user@example.com",
  "type": "transactional",
  "channel": "email",
  "payload": "{\"templateId\":\"tpl_xxxxx\",\"variables\":{\"firstName\":\"John\",\"orderId\":\"12345\",\"amount\":\"99.00\"}}"
}
```

## OpenAPI Specification

Every endpoint is documented with a full OpenAPI 3.0 spec. Use it to generate client SDKs, test endpoints, or integrate with your existing tools.

- **Swagger UI** — interactive explorer at [api.notificationhub.space/docs](https://api.notificationhub.space/docs)
- **OpenAPI JSON** — machine-readable spec at [api.notificationhub.space/openapi/v1.json](https://api.notificationhub.space/openapi/v1.json)
- **Code generation** — use `openapi-generator` to generate clients in 40+ languages

## Official SDKs

Pick your language and get started in minutes:

| Language | Package | Install |
|----------|---------|---------|
| **TypeScript / JavaScript** | `@notificationhub/sdk` | `npm install @notificationhub/sdk` |
| **Python** | `notificationhub` | `pip install notificationhub` |
| **Go** | `github.com/MuhammedAdebiyi/NotificationHub/sdk/go` | `go get` |
| **C#** | `NotificationHub.Client` | `dotnet add package` |
| **NestJS** | `@notificationhub/nestjs` | `npm install @notificationhub/nestjs` |
| **PHP** | `notificationhub/sdk` | `composer require notificationhub/sdk` |

All SDKs support: send notifications, retry failed messages, list/get templates, campaign progress, and campaign send.

## Authentication

Two authentication methods for different use cases:

- **JWT tokens** — for dashboard users. Short-lived, rotate with refresh tokens.
- **API keys** — for server-to-server. Long-lived, scoped to your organization. Pass via `X-Api-Key` header.

```bash
# API key authentication
curl -H "X-Api-Key: nhub_live_your_key" \
  https://api.notificationhub.space/api/v1/notifications
```

## Webhooks

Get notified in real-time when email status changes. Configure a webhook URL in your dashboard and NotificationHub will POST event payloads to your server.

**Supported events:**
- `notification.sent` — email delivered to provider
- `notification.delivered` — email confirmed delivered
- `notification.failed` — delivery failed
- `notification.bounced` — email bounced
- `notification.retrying` — automatic retry in progress

```json
{
  "event": "notification.sent",
  "notificationId": "nfn_xxxxx",
  "recipientEmail": "user@example.com",
  "provider": "resend",
  "timestamp": "2026-09-13T10:30:00Z"
}
```

## Rate Limiting

All API endpoints are rate-limited to protect your account:

| Tier | Requests / minute |
|------|-------------------|
| Free | 60 |
| Pro | 600 |
| Enterprise | 6,000 |

Rate limit headers are included in every response:

```
X-RateLimit-Limit: 60
X-RateLimit-Remaining: 58
X-RateLimit-Reset: 1694610060
```

## Dashboard

The web dashboard at [notificationhub.space](https://notificationhub.space) gives you:

- **Real-time analytics** — delivery rates, failure rates, provider breakdown
- **Notification history** — search, filter by status, retry failed messages
- **Template management** — create, edit, preview templates
- **Campaign builder** — schedule, send, track bulk campaigns
- **Provider settings** — connect multiple providers, set defaults, verify domains
- **Team management** — invite members, assign roles (Owner, Admin, Member, Viewer)
- **Webhook configuration** — set up event notifications
- **API key management** — create, rotate, revoke API keys
