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
| `409` | Duplicate request (same idempotency key) |

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

Returns full notification detail including delivery logs.

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
