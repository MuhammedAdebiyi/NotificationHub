# Changelog

## v1.1.0 — November 2026

### Reply-to resolution
- **Per-send `replyTo`, `senderEmail` and `senderName` on `POST /api/v1/notifications`** — resolved once when the notification is enqueued and stored on it, so retries and dead-letter replays keep the exact value the request was accepted with
- **Five-level precedence** — request → template → organization settings → sender address → provider default. The effective value and its source (`request`, `template`, `organization`, `sender`, `default`) are returned by the notification detail API and shown in the dashboard
- **Templates carry addressing** — `replyTo`, `senderEmail` and `senderName` on the Templates API, applied when a send passes `templateId`
- **Fixed: Settings → Reply-to email now applies** — it was editable but never read by any send path, so API notifications ignored it; notifications now honour it (campaign sends keep their existing behaviour)
- **Verified-domain enforcement** — request-supplied reply-to/sender addresses must use a domain verified with a configured provider, failing with `400 invalid_reply_to` / `unverified_reply_to_domain` (and the sender equivalents) and naming the domain
- **Identical across providers** — Reply-To is mapped to `reply_to` (Resend, SendByte), `replyTo: [{email}]` (Brevo), `reply_to: {email}` (SendGrid) and the SMTP `Reply-To` header, including on every attempt of the automatic provider fallback

### Testing
- New xUnit suite (`tests/NotificationHub.Tests`) covering the precedence order, per-adapter provider payloads, provider fallback preserving the reply-to, retry-after-settings-change, and validation error codes

## v1.0.0 — September 2026

### Initial Release
- **Notifications API** — Send transactional emails with multi-provider fallback
- **Templates** — Reusable email designs with `{{variable}}` placeholders
- **Campaigns** — Bulk email campaigns with scheduling, pause/resume, and progress tracking
- **Multi-Provider Support** — Resend, SendByte, SendGrid, Brevo, SMTP with automatic failover
- **OpenAPI Documentation** — Interactive Swagger UI at `api.notificationhub.space/docs`
- **TypeScript SDK** — `@notificationhub/sdk` npm package
- **Python SDK** — `notificationhub` pip package
- **Dashboard** — React-based web UI with analytics, team management, and data source imports

### Providers
- Resend adapter with domain verification
- SendByte adapter with domain verification
- SendGrid adapter with domain verification
- Brevo adapter with domain verification
- SMTP adapter for any email server

### Infrastructure
- .NET 10 backend with Clean Architecture
- PostgreSQL with Entity Framework Core
- Redis for queue management
- Worker service for async email processing
- GitHub Actions CI/CD with Docker blue-green deployment
