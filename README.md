# NotificationHub

NotificationHub is a multi-tenant notification infrastructure platform built with ASP.NET Core, PostgreSQL, Redis and React.

It provides a centralized notification system that allows applications to send transactional notifications and large-scale campaigns without coupling business logic to email delivery.

Instead of every application communicating directly with an email provider, applications publish notifications to NotificationHub. NotificationHub validates the request, persists the notification, queues it for background processing and delivers it asynchronously through a provider.

The project was built primarily as a software engineering exercise focused on designing scalable backend systems rather than simply sending emails.

---

## Why NotificationHub Exists

Many applications begin with code similar to this:

```csharp
await emailService.SendAsync(...);
```

This works for small projects, but quickly introduces problems as the system grows.

- HTTP requests become dependent on external providers.
- Duplicate requests may send duplicate emails.
- Failed deliveries are difficult to retry safely.
- Bulk sends block application threads.
- Analytics become difficult to build.
- Every application implements notifications differently.

NotificationHub solves these problems by introducing a dedicated notification platform.

```
Application

        │

        ▼

NotificationHub API

        │

        ▼

PostgreSQL

        │

        ▼

Redis Queue

        │

        ▼

Notification Worker

        │

        ▼

Email Provider

        │

        ▼

Recipient
```

Applications return immediately after the notification is queued while background workers handle delivery independently.

---

# Goals

The project focuses on backend engineering concepts including:

- Multi-tenancy
- Clean Architecture
- Background processing
- Queue-based systems
- Idempotency
- Retry strategies
- Dead Letter Queues
- Operational dashboards
- Analytics
- Provider abstraction
- Dependency Injection
- Production-ready API design

---

# Features

## Authentication

- User registration
- Login
- JWT authentication
- Email verification
- Password reset
- Organization invitations

---

## Organizations

Every resource belongs to an organization.

```
Organization

├── Members

├── API Keys

├── Notifications

├── Templates

├── Campaigns

├── Notification Logs

└── Settings
```

Every request is scoped by `OrganizationId`.

Cross-organization data access is impossible by design.

---

## API Keys

Applications authenticate using organization API keys.

Improvements include:

- Prefix lookup
- BCrypt verification
- O(1) key lookup
- One-time plaintext display
- Last-used tracking
- Revocation support

---

## Notification Pipeline

```
POST Notification

↓

Authentication

↓

Organization Validation

↓

Idempotency Check

↓

Persist Notification

↓

Redis Queue

↓

Return HTTP 202

────────────────────────────

Background Worker

↓

Provider

↓

Success

or

Retry

↓

Dead Letter Queue
```

The HTTP request never waits for email delivery.

---

## Background Workers

Notification delivery is handled by dedicated workers.

Current implementation:

- BackgroundService
- Redis-backed queue
- Configurable concurrency
- SemaphoreSlim throttling
- Retry queue
- Dead Letter Queue
- Provider abstraction

Current concurrency model:

```
NotificationWorker

↓

SemaphoreSlim(50)

↓

Maximum 50 concurrent sends

↓

Peek Queue

↓

Dispatch Task

↓

finally

↓

Release Semaphore
```

This prevents worker exhaustion while maintaining high throughput.

---

## Retry Strategy

Transient failures are automatically retried.

Current retry schedule:

```
1 second

↓

5 seconds

↓

15 seconds

↓

30 seconds

↓

60 seconds
```

Notifications exceeding the retry limit are moved to the Dead Letter Queue.

---

## Dead Letter Queue

Notifications that permanently fail are preserved instead of discarded.

Operators can:

- Inspect failures
- View delivery history
- Retry manually
- Diagnose provider issues

---

## Templates

Organization-scoped templates support:

- Plain text editor
- HTML editor
- Rich text editor (TipTap)
- Live preview
- Variable placeholders
- CRUD operations

---

## Campaigns

Campaigns support large-scale notification delivery.

```
Campaign

↓

Recipients

↓

Notification Creation

↓

Redis Queue

↓

Worker Fleet

↓

Provider
```

Large campaigns are processed incrementally without blocking the API.

---

## Dashboard

The dashboard is designed as an operational control center rather than a reporting page.

It provides visibility into:

- Organization health
- Queue status
- Worker health
- Delivery success
- Campaign progress
- Activity feed
- Infrastructure status
- Usage metrics

---

## Analytics

Analytics are exposed through dedicated endpoints.

```
GET /analytics/overview

GET /analytics/health

GET /analytics/timeline

GET /analytics/queue

GET /analytics/activity

GET /analytics/failures

GET /analytics/campaigns

GET /analytics/infrastructure

GET /analytics/delivery-funnel

GET /analytics/providers

GET /analytics/usage
```

Analytics are intentionally separated from dashboard endpoints to keep responsibilities isolated.

---

# Architecture

The solution follows Clean Architecture.

```
NotificationHub.Api

NotificationHub.Application

NotificationHub.Domain

NotificationHub.Infrastructure
```

Responsibilities are divided as follows.

## Domain

Contains business entities.

No infrastructure dependencies.

---

## Application

Contains:

- Interfaces
- DTOs
- Business contracts
- Feature definitions

No database code.

---

## Infrastructure

Contains:

- EF Core
- Redis
- Email providers
- Services
- Repositories

Infrastructure implements application abstractions.

---

## API

Contains:

- Controllers
- Middleware
- Authentication
- Dependency Injection

Controllers intentionally remain thin.

Business logic is never implemented inside controllers.

---

# Dependency Direction

```
API

↓

Application

↓

Domain

↑

Infrastructure
```

Infrastructure depends on Application.

Application never depends on Infrastructure.

Domain depends on nothing.

---

# Engineering Principles

Several design rules guide the implementation.

## Thin Controllers

Controllers only:

- Validate requests
- Call services
- Return responses

No business logic.

No EF queries.

No Redis access.

---

## Business Logic Lives in Services

Application behavior belongs inside services.

Controllers remain transport layers.

---

## Organization Isolation

Every query is filtered using the authenticated organization.

Organization isolation is enforced throughout the application.

---

## Asynchronous Processing

Background work never blocks HTTP requests.

All notification delivery occurs asynchronously.

---

## Idempotency

Duplicate requests never create duplicate notifications.

Idempotency is enforced using unique keys stored in the database.

---

## Provider Abstraction

NotificationHub depends on abstractions rather than providers.

Current provider:

- SendByte

Future providers can be added without modifying business logic.

---

## Background Processing

Workers use scoped services correctly.

Each notification is processed independently.

---

## Dependency Injection

Services are registered through abstractions.

Concrete implementations remain hidden behind interfaces.

---

## Observability

Operational visibility is treated as a first-class concern.

The system exposes:

- Queue health
- Worker health
- Provider health
- Infrastructure status
- Delivery metrics

---

# Current Technology Stack

Backend

- ASP.NET Core
- Entity Framework Core
- PostgreSQL
- Redis

Frontend

- React
- TypeScript
- Tailwind CSS
- TipTap

Authentication

- JWT
- BCrypt

Infrastructure

- Docker
- Redis
- BackgroundService

---

# Current Project Status

| Area | Status |
|-------|--------|
| Authentication | Complete |
| Multi-tenancy | Complete |
| Notification API | Complete |
| Redis Queue | Complete |
| Background Worker | Complete |
| Retry Strategy | Complete |
| Dead Letter Queue | Complete |
| Templates | Complete |
| Dashboard | Complete |
| Analytics | Complete |
| Campaign UI | Complete |
| Team Management | In Progress |
| API Keys | In Progress |
| SignalR | Planned |
| Multiple Providers | Planned |
| BYOD Domains | Planned |

---

# Running Locally

Requirements

- .NET 10 SDK
- PostgreSQL
- Redis
- Node.js
- pnpm

Backend

```bash
cd src/NotificationHub.Api

dotnet restore

dotnet ef database update

dotnet run
```

Frontend

```bash
cd client

pnpm install

pnpm dev
```

---

# Future Work

Planned improvements include:

- SignalR live updates
- Multiple worker instances
- Distributed worker coordination
- Provider failover
- Prometheus metrics
- Grafana dashboards
- Custom sending domains
- SMS providers
- Push notifications
- Slack integration
- Discord integration
- Telegram integration
- Webhook delivery
- Scheduled campaigns
- Rate limiting
- Circuit breakers

---

# License

This project is intended as a software engineering learning project demonstrating scalable backend architecture, distributed processing, and production-oriented application design.
