---
title: NotificationHub
---

# NotificationHub

Production-grade notification platform — Email, SMS, Push, and In-App delivery across a queued, async worker pipeline with retry backoff, dead letter queue, and idempotency. Built to understand how Uber and Stripe handle notifications internally.

## Links

| | |
|---|---|
| **Live** | [notificationhub.space](https://notificationhub.space) |
| **API** | [api.notificationhub.space](https://api.notificationhub.space) |
| **Docs** | [docs.notificationhub.space](https://docs.notificationhub.space) |
| **Swagger** | [api.notificationhub.space/docs](https://api.notificationhub.space/docs) |
| **GitHub** | [github.com/MuhammedAdebiyi/NotificationHub](https://github.com/MuhammedAdebiyi/NotificationHub) |

## Tech Stack

| Layer | Technology |
|-------|-----------|
| API | C# / .NET 10, ASP.NET Core |
| Worker | .NET BackgroundService |
| Database | PostgreSQL (Neon) |
| Cache / Queue | Redis (Upstash) |
| Frontend | React 19, Vite 8, TypeScript 6, Tailwind v4 |
| Email | SendByte, Resend, SendGrid, Brevo, SMTP |
| Docs | VitePress (CDN Swagger UI) |
| CI/CD | GitHub Actions → Docker blue-green deploy |
| Hosting | VPS (Contabo), Vercel (frontend + docs) |

## Architecture

```
┌─────────────┐     ┌─────────────┐     ┌──────────────────┐
│   React UI  │────▶│  .NET API   │────▶│    PostgreSQL     │
│  (Vercel)   │     │  (Docker)   │     │     (Neon)        │
└─────────────┘     └──────┬──────┘     └──────────────────┘
                           │
                    ┌──────▼──────┐
                    │  Redis Queue │◀─── Write outbox
                    │  (Upstash)   │
                    └──────┬──────┘
                           │
                    ┌──────▼──────┐     ┌──────────────────┐
                    │   Worker    │────▶│  Email Providers  │
                    │  (Docker)   │     │ SendByte/Resend/  │
                    └─────────────┘     │ SendGrid/Brevo    │
                                        └──────────────────┘
```

**Clean Architecture** — Domain → Application → Infrastructure → Api / Worker. Business logic never depends on frameworks or infrastructure.

**Transactional Outbox Pattern** — API writes to the outbox table in the same DB transaction as the business row. The worker polls the outbox, sends the email, then marks it sent. This guarantees at-least-once delivery without distributed transactions.

## Core Features

### Queued, Async Pipeline

Every API call returns in <20ms. The notification is written to the outbox table and pushed to a Redis queue. A background worker dequeues and processes messages independently — your app never blocks on email delivery.

```bash
curl -X POST https://api.notificationhub.space/api/v1/notifications \
  -H "X-Api-Key: nhub_live_your_key" \
  -H "Content-Type: application/json" \
  -d '{
    "recipientEmail": "user@example.com",
    "type": "transactional",
    "channel": "email",
    "payload": {
      "subject": "Welcome!",
      "html": "<h1>Hello!</h1><p>Your account is ready.</p>"
    }
  }'
# 201 — queued in 12ms
```

### 5-Attempt Retry with Exponential Backoff

Failed sends are retried with increasing delays: 1s → 5s → 15s → 30s → 60s. After 5 failures, the notification moves to a Dead Letter Queue for manual inspection and replay.

The retry schedule is stored in a Redis sorted set (score = unix timestamp). The worker atomically promotes due retries back into the working queue with a Lua script — no double-processing across worker instances.

### Dead Letter Queue

Notifications that exhaust all retries land in the DLQ. You can replay them from the dashboard or API with one click — no data lost.

```bash
# Replay from DLQ
curl -X POST https://api.notificationhub.space/api/v1/notifications/{id}/retry \
  -H "X-Api-Key: nhub_live_your_key"
```

### Idempotency Keys

Pass an `Idempotency-Key` header and we guarantee the message is sent exactly once — enforced at the database level with a unique index. No duplicate sends, even if your client retries.

```bash
curl -X POST https://api.notificationhub.space/api/v1/notifications \
  -H "X-Api-Key: nhub_live_your_key" \
  -H "Idempotency-Key: order-12345-confirmation" \
  -d '{ ... }'
```

### Multi-Provider Fallback

Connect multiple email providers. Set a default. If it fails (timeout, 5xx, rate limit), the next provider is tried automatically — zero code changes.

| Provider | Type | Setup |
|----------|------|-------|
| Resend | Transactional | API key |
| SendByte | Transactional | API key |
| SendGrid | Transactional + Marketing | API key |
| Brevo | Transactional + Marketing | API key |
| SMTP | Any | Host, port, user, password |

Each provider reports its own health status with per-provider error messages visible in the dashboard.

### Campaign Engine

Bulk email campaigns with scheduling, pause/resume, real-time progress tracking, and recipient import from external databases.

```bash
# Create campaign
curl -X POST https://api.notificationhub.space/api/v1/campaigns \
  -H "X-Api-Key: nhub_live_your_key" \
  -d '{"title": "September Update", "subject": "What is new"}'

# Add recipients
curl -X POST https://api.notificationhub.space/api/v1/campaigns/{id}/recipients \
  -H "X-Api-Key: nhub_live_your_key" \
  -d '{"emails": ["alice@co.com", "bob@co.com"]}'

# Send
curl -X POST https://api.notificationhub.space/api/v1/campaigns/{id}/send \
  -H "X-Api-Key: nhub_live_your_key"
```

### Email Templates

Reusable templates with `{{variable}}` placeholders. Manage via API or the visual editor in the dashboard.

```json
{
  "name": "order-confirmation",
  "subject": "Order #{{orderId}} confirmed",
  "body": "<h1>Hi {{firstName}}!</h1><p>Your order #{{orderId}} is confirmed. Total: ${{amount}}</p>"
}
```

### Webhooks

Get notified in real-time when email status changes. Configure a webhook URL and we POST event payloads to your server.

**Events:** `notification.sent`, `notification.delivered`, `notification.failed`, `notification.bounced`, `notification.retrying`

### Multi-Tenant by Design

Every organization gets isolated data, API keys, delivery stats, and provider configs. One platform serves many clients with full data isolation.

### Full OpenAPI Spec

Every endpoint is documented with a complete OpenAPI 3.0 spec. Use it to generate client SDKs, test endpoints, or integrate with your existing tools.

- **Swagger UI** — [api.notificationhub.space/docs](https://api.notificationhub.space/docs)
- **OpenAPI JSON** — [api.notificationhub.space/openapi/v1.json](https://api.notificationhub.space/openapi/v1.json)

### Official SDKs

Pick your language and get started in minutes:

| Language | Package | Install |
|----------|---------|---------|
| TypeScript | `@notificationhub/sdk` | `npm install @notificationhub/sdk` |
| Python | `notificationhub` | `pip install notificationhub` |
| Go | `github.com/MuhammedAdebiyi/NotificationHub/sdk/go` | `go get` |
| C# | `NotificationHub.Client` | `dotnet add package` |
| NestJS | `@notificationhub/nestjs` | `npm install @notificationhub/nestjs` |
| PHP | `notificationhub/sdk` | `composer require notificationhub/sdk` |

## Dashboard Features

- **Real-time analytics** — delivery rates, failure rates, provider breakdown, queue depth
- **Notification history** — search, filter by status, retry failed messages
- **Template management** — create, edit, preview templates with live variables
- **Campaign builder** — schedule, send, track bulk campaigns with progress bars
- **Provider settings** — connect multiple providers, set defaults, verify domains with health status
- **Team management** — invite members, assign roles (Owner, Admin, Member, Viewer)
- **Webhook configuration** — set up event notifications
- **API key management** — create, rotate, revoke API keys

## Infrastructure Decisions

| Decision | Rationale |
|----------|-----------|
| Transactional Outbox | Guarantees at-least-once delivery without distributed transactions |
| Redis Sorted Set for retries | Atomic promote-with-score via Lua script, no double-processing |
| DLQ with manual replay | visibility into failures, no silent data loss |
| Idempotency key unique index | Duplicate prevention at DB level, not application level |
| Blue-green Docker deploy | Zero-downtime deploys via Caddy reverse proxy swap |
| Per-provider health checks | Each provider reports own status, dashboard shows granular errors |
| CDN-based Swagger UI | Serves docs from API origin, no separate docs deployment |

## By the Numbers

| Metric | Value |
|--------|-------|
| API response time | <20ms |
| Queue latency | <2s |
| Retry backoff | 1s → 5s → 15s → 30s → 60s |
| Max retries | 5 |
| Concurrent workers | 50 per instance |
| Supported providers | 5 (Resend, SendByte, SendGrid, Brevo, SMTP) |
| Official SDKs | 6 (TS, Python, Go, C#, NestJS, PHP) |
