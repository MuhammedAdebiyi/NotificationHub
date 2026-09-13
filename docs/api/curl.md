# cURL Examples

Every example uses `X-Api-Key` authentication.

## Send an email

```bash
curl -X POST https://api.notificationhub.space/api/v1/notifications \
  -H "X-Api-Key: nhub_live_your_key" \
  -H "Content-Type: application/json" \
  -d '{
    "recipientEmail": "user@example.com",
    "type": "transactional",
    "channel": "email",
    "payload": "{\"subject\":\"Hello\",\"html\":\"<p>Welcome!</p>\"}"
  }'
```

## Send with idempotency

```bash
curl -X POST https://api.notificationhub.space/api/v1/notifications \
  -H "X-Api-Key: nhub_live_your_key" \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: order-12345" \
  -d '{
    "recipientEmail": "user@example.com",
    "type": "transactional",
    "channel": "email",
    "payload": "{\"subject\":\"Order Confirmed\",\"html\":\"<p>Thanks!</p>\"}"
  }'
```

## List recent notifications

```bash
curl "https://api.notificationhub.space/api/v1/notifications?page=1&pageSize=10&status=sent" \
  -H "X-Api-Key: nhub_live_your_key"
```

## Get notification detail

```bash
curl "https://api.notificationhub.space/api/v1/notifications/a1b2c3d4-e5f6-7890-abcd-ef1234567890" \
  -H "X-Api-Key: nhub_live_your_key"
```

## Retry a failed notification

```bash
curl -X POST "https://api.notificationhub.space/api/v1/notifications/a1b2c3d4-.../retry" \
  -H "X-Api-Key: nhub_live_your_key"
```

## Create a template

```bash
curl -X POST https://api.notificationhub.space/api/v1/templates \
  -H "X-Api-Key: nhub_live_your_key" \
  -H "Content-Type: application/json" \
  -d '{"name":"Password Reset","subject":"Reset your password","body":"<p>Click <a href=\"{{resetUrl}}\">here</a> to reset.</p>"}'
```

## Create and send a campaign

```bash
# Create
CAMPAIGN_ID=$(curl -s -X POST https://api.notificationhub.space/api/v1/campaigns \
  -H "X-Api-Key: nhub_live_your_key" \
  -H "Content-Type: application/json" \
  -d '{"title":"Launch","subject":"We just launched!","body":"<h1>Big news</h1>"}' \
  | jq -r '.id')

# Add recipients
curl -X POST "https://api.notificationhub.space/api/v1/campaigns/$CAMPAIGN_ID/recipients" \
  -H "X-Api-Key: nhub_live_your_key" \
  -H "Content-Type: application/json" \
  -d '{"emails":["alice@example.com","bob@example.com"]}'

# Send
curl -X POST "https://api.notificationhub.space/api/v1/campaigns/$CAMPAIGN_ID/send" \
  -H "X-Api-Key: nhub_live_your_key"
```
