# Notifications API

## Send a Notification

```
POST /api/v1/notifications
```

Sends a new notification (email, SMS, push, or in-app).

### Request Body

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `recipientEmail` | `string` | Yes | Recipient email address |
| `type` | `string` | Yes | Notification type (e.g. `"transactional"`, `"marketing"`, `"alert"`) |
| `channel` | `string` | Yes | `"email"`, `"sms"`, `"push"`, or `"inapp"` |
| `payload` | `string` | Yes | JSON string with content. For email: `{"subject": "...", "html": "..."}` |
| `replyTo` | `string` | No | Single reply-to address for this send. See [Reply-to resolution](#reply-to-resolution) |
| `senderEmail` | `string` | No | Sender address override for this send (must be on a verified domain) |
| `senderName` | `string` | No | Sender display name override for this send (max 100 characters) |
| `templateId` | `string (uuid)` | No | Template whose `replyTo`/`senderEmail`/`senderName` apply when this request sets none of its own |

### Headers

| Header | Required | Description |
|--------|----------|-------------|
| `X-Api-Key` | Yes | Your API key |
| `Idempotency-Key` | No | Unique key to prevent duplicate sends |

### Example

```bash
curl -X POST https://api.notificationhub.space/api/v1/notifications \
  -H "X-Api-Key: nhub_live_your_key" \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: order-12345-email" \
  -d '{
    "recipientEmail": "user@example.com",
    "type": "transactional",
    "channel": "email",
    "replyTo": "support@coursevault.com",
    "payload": "{\"subject\":\"Order Confirmed\",\"html\":\"<h1>Order #12345</h1><p>Thank you for your purchase!</p>\"}"
  }'
```

### Response

```json
{
  "publicId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890"
}
```

| Status | Description |
|--------|-------------|
| `200` | Notification queued successfully |
| `400` | Invalid reply-to/sender, unverified domain, or unknown `templateId` — see `code` |
| `409` | Duplicate request (same idempotency key) |

### Error responses

Failures carry a stable `code` so clients can branch on it instead of matching
message text:

```json
{
  "error": "Domain 'evil.example' is not verified with your email providers. Verified domains: example.com.",
  "code": "unverified_reply_to_domain"
}
```

Field-level validation failures return the messages array as well:

```json
{
  "error": "ReplyTo must be a single email address.",
  "errors": ["ReplyTo must be a single email address."],
  "code": "invalid_reply_to"
}
```

| Code | Status | Meaning |
|------|--------|---------|
| `invalid_reply_to` | `400` | `replyTo` is not a single email address (lists and display names are rejected) |
| `unverified_reply_to_domain` | `400` | `replyTo`'s domain is not verified with any configured provider — the message names the domain |
| `invalid_sender_email` | `400` | `senderEmail` is not a single email address |
| `unverified_sender_domain` | `400` | `senderEmail`'s domain is not verified with any configured provider |
| `invalid_sender_name` | `400` | `senderName` is longer than 100 characters |
| `template_not_found` | `400` | `templateId` does not belong to this organization |
| `organization_not_found` | `400` | The API key's organization no longer exists |
| — | `409` | Duplicate request (same `Idempotency-Key`) |

---

## Reply-to resolution

The effective reply-to **and** sender of a notification are decided **once, while
the notification is enqueued**, and stored on the notification row. Retries and
dead-letter replays re-read that stored value — changing Settings after a send
has been queued never rewrites it.

Precedence, highest first:

| # | `replyTo` comes from | `senderEmail` / `senderName` come from |
|---|----------------------|----------------------------------------|
| 1 | `replyTo` in the request | `senderEmail` / `senderName` in the request |
| 2 | `replyTo` on the template (when `templateId` is passed) | `senderEmail` / `senderName` on the template |
| 3 | Settings → **Reply-to email** | Settings → **Sender email** / **Sender name** |
| 4 | Settings → **Sender email** (replies fall back to the sender) | — |
| 5 | Provider default — no `Reply-To` header is sent | Provider default |

Notes:

- **One address only.** `replyTo` must be a single bare address — `"a@b.com, c@d.com"`
  and `"Support <a@b.com>"` are rejected with `invalid_reply_to`.
- **Verified domains.** Addresses supplied in the request must use a domain that
  is verified with at least one of your configured providers (the same rule
  Settings → Sender email uses). The check is skipped when no provider reports a
  verified domain (for example an SMTP-only setup), so sending is never locked out.
  Organization- and template-supplied values are owned by the dashboard and are
  not re-checked on send.
- **The resolved value travels to the provider.** Resend and SendByte receive
  `reply_to`, Brevo `replyTo: [{ "email": ... }]`, SendGrid `reply_to: { "email": ... }`,
  and SMTP sets the `Reply-To` header — identically on every attempt of the
  automatic provider fallback chain.
- **You can read it back.** `GET /api/v1/notifications/{publicId}` returns
  `replyTo`, `replyToSource`, `senderEmail`, `senderName`, `senderSource` and the
  `template` the values came from. `replyToSource` is one of `request`,
  `template`, `organization`, `sender`, `default`.

---

## List Notifications

```
GET /api/v1/notifications
```

### Query Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `page` | `int` | `1` | Page number |
| `pageSize` | `int` | `20` | Items per page (1-100) |
| `dateFrom` | `string` | — | ISO 8601 date filter |
| `dateTo` | `string` | — | ISO 8601 date filter |
| `status` | `string` | — | Filter: `Pending`, `Processing`, `Sent`, `Failed`, `Retrying`, `DeadLetter` |

### Response

```json
{
  "items": [
    {
      "publicId": "...",
      "recipientEmail": "user@example.com",
      "type": "transactional",
      "channel": "email",
      "status": "sent",
      "createdAt": "2026-09-10T12:00:00Z",
      "processedAt": "2026-09-10T12:00:02Z"
    }
  ],
  "totalCount": 150,
  "pageNumber": 1,
  "pageSize": 20
}
```

---

## Get Notification Detail

```
GET /api/v1/notifications/{publicId}
```

Returns full notification detail including delivery logs, the effective
reply-to/sender and where each value came from (see
[Reply-to resolution](#reply-to-resolution)):

```json
{
  "publicId": "a1b2c3d4-...",
  "recipientEmail": "user@example.com",
  "status": "Sent",
  "replyTo": "support@coursevault.com",
  "replyToSource": "request",
  "senderEmail": "news@coursevault.com",
  "senderName": "CourseVault",
  "senderSource": "organization",
  "templateId": null,
  "templateName": null
}
```

---

## Retry a Failed Notification

```
POST /api/v1/notifications/{publicId}/retry
```

Requeues a failed notification for delivery. Only works for `Failed` or `DeadLetter` status.

### Response

```json
{ "retried": true }
```

---

## Notification Logs

```
GET /api/v1/notifications/logs
```

Query notification delivery logs across all notifications in your organization. Logs record every attempt (success, failure, retry) and are retained for 30 days.

### Query Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `page` | `int` | `1` | Page number |
| `pageSize` | `int` | `20` | Items per page (1-100) |
| `dateFrom` | `string` | — | ISO 8601 start date filter |
| `dateTo` | `string` | — | ISO 8601 end date filter |
| `status` | `string` | — | Filter by status: `Pending`, `Processing`, `Sent`, `Delivered`, `Failed`, `Retrying`, `DeadLetter` |
| `notificationId` | `string` | — | Filter by notification public ID |
| `recipientEmail` | `string` | — | Filter by recipient email (exact match) |
| `provider` | `string` | — | Filter by provider name (e.g. `resend`, `sendgrid`) |

### Example

```bash
curl -X GET "https://api.notificationhub.space/api/v1/notifications/logs?status=Failed&dateFrom=2026-09-01T00:00:00Z&pageSize=50" \
  -H "X-Api-Key: nhub_live_your_key"
```

### Response

```json
{
  "items": [
    {
      "id": "log_xxxxx",
      "notificationId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "recipientEmail": "user@example.com",
      "status": "Failed",
      "provider": "resend",
      "providerMessageId": null,
      "errorCode": "invalid_email",
      "errorMessage": "The recipient address was rejected",
      "retryCount": 3,
      "createdAt": "2026-09-10T12:00:00Z",
      "processedAt": "2026-09-10T12:00:01Z",
      "deliveredAt": null,
      "failedAt": "2026-09-10T12:00:05Z",
      "isTest": false
    }
  ],
  "totalCount": 342,
  "pageNumber": 1,
  "pageSize": 50
}
```

### Log Entry Fields

| Field | Type | Description |
|-------|------|-------------|
| `id` | `string` | Unique log entry ID |
| `notificationId` | `string` | Parent notification ID |
| `recipientEmail` | `string` | Recipient address |
| `status` | `string` | Current status of this delivery attempt |
| `provider` | `string` | Provider that handled the attempt |
| `providerMessageId` | `string \| null` | Provider-side tracking ID |
| `errorCode` | `string \| null` | Provider error code (on failure) |
| `errorMessage` | `string \| null` | Human-readable failure reason |
| `retryCount` | `int` | Number of retry attempts |
| `createdAt` | `string` | ISO 8601 timestamp |
| `processedAt` | `string \| null` | When processing started |
| `deliveredAt` | `string \| null` | When delivery was confirmed |
| `failedAt` | `string \| null` | When the failure occurred |
| `isTest` | `bool` | `true` if sent with a test API key |

| Status | Description |
|--------|-------------|
| `200` | Logs returned successfully |
| `401` | Missing or invalid API key |
| `429` | Rate limit exceeded |
