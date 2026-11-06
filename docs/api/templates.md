# Templates API

Templates are reusable email designs with `{{variable}}` placeholders. They can
also carry a reply-to and sender, which apply to any notification created from
the template — see [Reply-to resolution](/api/notifications#reply-to-resolution)
for where template values sit in the precedence order.

## Create a Template

```
POST /api/v1/templates
```

### Request Body

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `name` | `string` | Yes | Template name (e.g. "Welcome Email") |
| `subject` | `string` | Yes | Email subject line. Supports `{{variables}}` |
| `body` | `string` | Yes | Email body HTML. Supports `{{variables}}` |
| `replyTo` | `string` | No | Single reply-to address for notifications sent from this template |
| `senderEmail` | `string` | No | Sender address for notifications sent from this template |
| `senderName` | `string` | No | Sender display name (max 100 characters) |

`replyTo` and `senderEmail` must each be a single bare address; otherwise the
request fails with `400` and `code: "invalid_reply_to"` /
`"invalid_sender_email"`.

### Example

```bash
curl -X POST https://api.notificationhub.space/api/v1/templates \
  -H "X-Api-Key: nhub_live_your_key" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Welcome Email",
    "subject": "Welcome {{name}}!",
    "body": "<h1>Hello {{name}}</h1><p>Your account is ready. Plan: {{plan}}</p>",
    "replyTo": "support@coursevault.com"
  }'
```

### Response

```json
{
  "id": "a1b2c3d4-...",
  "name": "Welcome Email"
}
```

---

## List Templates

```
GET /api/v1/templates?page=1&pageSize=20
```

---

## Get Template

```
GET /api/v1/templates/{id}
```

Returns full template including body content and its addressing fields
(`replyTo`, `senderEmail`, `senderName`).

---

## Update Template

```
PUT /api/v1/templates/{id}
```

### Request Body

Same as create: `name`, `subject`, `body`, plus the optional `replyTo`,
`senderEmail` and `senderName`. The PUT replaces the whole template — send the
addressing fields you want to keep, or omit them to clear them.

---

## Delete Template

```
DELETE /api/v1/templates/{id}
```
