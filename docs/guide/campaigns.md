# Campaigns Guide

Campaigns let you send bulk emails to hundreds or thousands of recipients with scheduling and progress tracking.

## Workflow

```
Create Campaign → Add Recipients → Send (or Schedule) → Track Progress
```

## Step 1: Create a Campaign

```bash
curl -X POST https://api.notificationhub.space/api/v1/campaigns \
  -H "X-Api-Key: nhub_live_your_key" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "September Newsletter",
    "subject": "What's new in September",
    "body": "<h1>September Update</h1><p>Here is what happened this month...</p>"
  }'
```

## Step 2: Add Recipients

```bash
curl -X POST https://api.notificationhub.space/api/v1/campaigns/{id}/recipients \
  -H "X-Api-Key: nhub_live_your_key" \
  -H "Content-Type: application/json" \
  -d '{"emails": ["user1@example.com", "user2@example.com"]}'
```

## Step 3: Send

**Immediate:**
```bash
curl -X POST https://api.notificationhub.space/api/v1/campaigns/{id}/send \
  -H "X-Api-Key: nhub_live_your_key"
```

**Scheduled:**
```bash
curl -X POST https://api.notificationhub.space/api/v1/campaigns/{id}/schedule \
  -H "X-Api-Key: nhub_live_your_key" \
  -H "Content-Type: application/json" \
  -d '{"scheduledAt": "2026-09-15T10:00:00Z"}'
```

## Step 4: Track Progress

```bash
curl https://api.notificationhub.space/api/v1/campaigns/{id}/progress \
  -H "X-Api-Key: nhub_live_your_key"
```

```json
{
  "totalRecipients": 5000,
  "sent": 3200,
  "progressPercent": 64
}
```

## Pause and Resume

If you need to stop a running campaign:

```bash
# Pause
curl -X POST https://api.notificationhub.space/api/v1/campaigns/{id}/pause \
  -H "X-Api-Key: nhub_live_your_key"

# Resume later
curl -X POST https://api.notificationhub.space/api/v1/campaigns/{id}/resume \
  -H "X-Api-Key: nhub_live_your_key"
```
