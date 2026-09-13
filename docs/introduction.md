# Introduction

NotificationHub is a multi-provider email notification platform with a REST API, campaign engine, and analytics dashboard.

## What you can do

- **Send transactional emails** — password resets, welcome emails, alerts
- **Run bulk campaigns** — product announcements, newsletters, marketing emails
- **Use templates** — reusable email designs with `{{variable}}` placeholders
- **Multi-provider fallback** — Resend, SendByte, SendGrid, Brevo, or SMTP
- **Track delivery** — open tracking, delivery logs, retry with exponential backoff
- **Connect external databases** — import recipients from PostgreSQL, MySQL, etc.

## Architecture

```
Your App → NotificationHub API → Provider Fallback Chain → Recipient
                                    ├─ Resend
                                    ├─ SendByte
                                    ├─ SendGrid
                                    ├─ Brevo
                                    └─ SMTP
```

## Base URL

All API requests use this base URL:

```
https://api.notificationhub.space
```

## Next steps

- [Quick Start](/quickstart) — send your first email in 2 minutes
- [Authentication](/authentication) — get your API key
- [API Reference](/api/) — full endpoint documentation
