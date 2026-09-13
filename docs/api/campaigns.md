# Campaigns API

Campaigns let you send bulk emails to multiple recipients with scheduling, progress tracking, and pause/resume.

## Create a Campaign

```
POST /api/v1/campaigns
```

### Request Body

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `title` | `string` | Yes | Campaign name |
| `subject` | `string` | Yes | Email subject line |
| `body` | `string` | No | Email body HTML (or use `templateId`) |
| `templateId` | `string` | No | Use a template instead of inline body |
| `channel` | `string` | No | Default: `"email"` |
| `scheduledAt` | `string` | No | ISO 8601 datetime to schedule sending |

### Response

```json
{
  "id": "a1b2c3d4-...",
  "title": "Product Launch"
}
```

---

## Add Recipients

```
POST /api/v1/campaigns/{id}/recipients
```

### Request Body

```json
{
  "emails": ["alice@example.com", "bob@example.com"]
}
```

### Response

```json
{
  "added": 2,
  "skipped": 0
}
```

---

## Send Campaign

```
POST /api/v1/campaigns/{id}/send
```

Sends the campaign immediately to all recipients.

---

## Schedule Campaign

```
POST /api/v1/campaigns/{id}/schedule
```

### Request Body

```json
{
  "scheduledAt": "2026-09-15T10:00:00Z"
}
```

---

## Get Progress

```
GET /api/v1/campaigns/{id}/progress
```

### Response

```json
{
  "totalRecipients": 1000,
  "sent": 850,
  "pending": 50,
  "processing": 10,
  "retrying": 5,
  "failed": 85,
  "deadLetter": 0,
  "progressPercent": 85
}
```

---

## Pause / Resume

```
POST /api/v1/campaigns/{id}/pause
POST /api/v1/campaigns/{id}/resume
```

---

## Delete Campaign

```
DELETE /api/v1/campaigns/{id}
```
