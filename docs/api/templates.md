# Templates API

Templates are reusable email designs with `{{variable}}` placeholders.

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

### Example

```bash
curl -X POST https://api.notificationhub.space/api/v1/templates \
  -H "X-Api-Key: nhub_live_your_key" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Welcome Email",
    "subject": "Welcome {{name}}!",
    "body": "<h1>Hello {{name}}</h1><p>Your account is ready. Plan: {{plan}}</p>"
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

Returns full template including body content.

---

## Update Template

```
PUT /api/v1/templates/{id}
```

### Request Body

Same as create: `name`, `subject`, `body`.

---

## Delete Template

```
DELETE /api/v1/templates/{id}
```
