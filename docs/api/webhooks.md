# Webhooks API

Manage webhook endpoints that receive real-time event notifications when emails are sent, delivered, failed, or bounced.

## Register a Webhook

```
POST /api/v1/webhooks
```

Creates a new webhook endpoint for your organization.

### Request Body

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `url` | `string` | Yes | HTTPS URL to receive webhook payloads |
| `events` | `string[]` | No | Events to subscribe to. Omit or pass `["*"]` for all events. |

### Events

| Event | Description |
|-------|-------------|
| `notification.sent` | Email delivered to the provider |
| `notification.delivered` | Email confirmed delivered to recipient |
| `notification.failed` | Delivery permanently failed |
| `notification.bounced` | Email bounced (hard or soft) |
| `notification.retrying` | Automatic retry in progress |
| `campaign.completed` | Campaign finished sending |
| `campaign.paused` | Campaign was paused |

### Headers

| Header | Required | Description |
|--------|----------|-------------|
| `X-Api-Key` | Yes | Your API key |
| `Content-Type` | Yes | `application/json` |

### Example

```bash
curl -X POST https://api.notificationhub.space/api/v1/webhooks \
  -H "X-Api-Key: nhub_live_your_key" \
  -H "Content-Type: application/json" \
  -d '{
    "url": "https://yourapp.com/webhooks/notificationhub",
    "events": ["notification.delivered", "notification.failed", "notification.bounced"]
  }'
```

### Response

```json
{
  "id": "whk_xxxxx",
  "url": "https://yourapp.com/webhooks/notificationhub",
  "events": ["notification.delivered", "notification.failed", "notification.bounced"],
  "secret": "whsec_xxxxxxxxxxxxxxxx",
  "isActive": true,
  "createdAt": "2026-09-13T10:00:00Z"
}
```

| Status | Description |
|--------|-------------|
| `201` | Webhook created |
| `400` | Invalid URL or payload |
| `409` | URL already registered |

::: warning
The `secret` is shown **only once** at creation time. Store it securely — you need it to verify incoming payloads.
:::

---

## List Webhooks

```
GET /api/v1/webhooks
```

Returns all registered webhooks for your organization.

### Example

```bash
curl -X GET https://api.notificationhub.space/api/v1/webhooks \
  -H "X-Api-Key: nhub_live_your_key"
```

### Response

```json
{
  "items": [
    {
      "id": "whk_xxxxx",
      "url": "https://yourapp.com/webhooks/notificationhub",
      "events": ["notification.delivered", "notification.failed"],
      "isActive": true,
      "lastTriggeredAt": "2026-09-13T10:30:00Z",
      "failureCount": 0,
      "createdAt": "2026-09-10T08:00:00Z"
    }
  ],
  "totalCount": 2
}
```

---

## Get Webhook Details

```
GET /api/v1/webhooks/{webhookId}
```

Returns full details for a single webhook, including recent delivery history.

### Example

```bash
curl -X GET https://api.notificationhub.space/api/v1/webhooks/whk_xxxxx \
  -H "X-Api-Key: nhub_live_your_key"
```

### Response

```json
{
  "id": "whk_xxxxx",
  "url": "https://yourapp.com/webhooks/notificationhub",
  "events": ["notification.delivered", "notification.failed"],
  "isActive": true,
  "secret": "whsec_xxxxxxxxxxxxxxxx",
  "lastTriggeredAt": "2026-09-13T10:30:00Z",
  "failureCount": 0,
  "recentDeliveries": [
    {
      "id": "dlv_xxxxx",
      "event": "notification.delivered",
      "statusCode": 200,
      "deliveredAt": "2026-09-13T10:30:00Z"
    }
  ],
  "createdAt": "2026-09-10T08:00:00Z"
}
```

| Status | Description |
|--------|-------------|
| `200` | Webhook found |
| `404` | Webhook not found |

---

## Update a Webhook

```
PUT /api/v1/webhooks/{webhookId}
```

Updates the URL or event subscriptions for an existing webhook.

### Request Body

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `url` | `string` | No | New HTTPS URL |
| `events` | `string[]` | No | New event subscription list |
| `isActive` | `bool` | No | Enable or disable the webhook |

### Example

```bash
curl -X PUT https://api.notificationhub.space/api/v1/webhooks/whk_xxxxx \
  -H "X-Api-Key: nhub_live_your_key" \
  -H "Content-Type: application/json" \
  -d '{
    "events": ["notification.delivered", "notification.failed", "notification.bounced", "campaign.completed"]
  }'
```

### Response

```json
{
  "id": "whk_xxxxx",
  "url": "https://yourapp.com/webhooks/notificationhub",
  "events": ["notification.delivered", "notification.failed", "notification.bounced", "campaign.completed"],
  "isActive": true,
  "updatedAt": "2026-09-13T11:00:00Z"
}
```

| Status | Description |
|--------|-------------|
| `200` | Webhook updated |
| `404` | Webhook not found |
| `400` | Invalid URL or events |

---

## Delete a Webhook

```
DELETE /api/v1/webhooks/{webhookId}
```

Permanently removes a webhook. No further events will be delivered to this URL.

### Example

```bash
curl -X DELETE https://api.notificationhub.space/api/v1/webhooks/whk_xxxxx \
  -H "X-Api-Key: nhub_live_your_key"
```

| Status | Description |
|--------|-------------|
| `204` | Webhook deleted |
| `404` | Webhook not found |

---

## Test a Webhook

```
POST /api/v1/webhooks/{webhookId}/test
```

Sends a test event payload to the webhook URL. Use this to verify your endpoint is receiving and processing events correctly.

### Example

```bash
curl -X POST https://api.notificationhub.space/api/v1/webhooks/whk_xxxxx/test \
  -H "X-Api-Key: nhub_live_your_key"
```

### Response

```json
{
  "delivered": true,
  "statusCode": 200,
  "responseTimeMs": 142
}
```

| Status | Description |
|--------|-------------|
| `200` | Test delivery succeeded |
| `502` | Target URL returned an error or timed out |

---

## Webhook Payload

Every event delivery sends a POST request to your registered URL with this structure:

```json
{
  "id": "evt_xxxxx",
  "event": "notification.delivered",
  "timestamp": "2026-09-13T10:30:00Z",
  "data": {
    "notificationId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "recipientEmail": "user@example.com",
    "provider": "resend",
    "providerMessageId": "re_abc123"
  }
}
```

## Signature Verification

Every delivery includes an `X-NotificationHub-Signature` header containing an HMAC-SHA256 signature of the raw request body, signed with your webhook secret.

```
X-NotificationHub-Signature: sha256=abc123...
```

To verify:

```typescript
import crypto from "crypto";

function verifyWebhookSignature(
  payload: string,
  signature: string,
  secret: string
): boolean {
  const expected = crypto
    .createHmac("sha256", secret)
    .update(payload)
    .digest("hex");
  return signature === `sha256=${expected}`;
}
```

## Delivery & Retries

- Event payloads are delivered within **5 seconds** of the event occurring.
- If your endpoint returns a non-2xx status or times out (10s), the delivery is retried **3 times** with exponential backoff (30s, 2m, 10m).
- After 3 failed attempts, the event is logged as a failed delivery and no further attempts are made.
- You can view delivery history for each webhook via `GET /api/v1/webhooks/{webhookId}`.

## Rate Limits

- Webhook deliveries do not count against your API rate limit.
- Maximum of **5 webhooks** per organization.
- Each webhook can subscribe to up to **10 specific events** (or `*` for all).
