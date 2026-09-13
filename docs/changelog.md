# Changelog

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
