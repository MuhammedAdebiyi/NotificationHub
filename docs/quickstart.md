# Quick Start

Send your first email in 2 minutes.

## 1. Get an API key

1. Sign up at [notificationhub.space](https://notificationhub.space/signup)
2. Go to **Settings → API Keys**
3. Click **+ Create**, name your key, and copy it (shown once)

## 2. Connect an email provider

Before sending, connect at least one email provider:

1. Go to **Settings → Email Providers**
2. Click **+ Add Provider**
3. Choose a provider (Resend, SendByte, SendGrid, Brevo, or SMTP)
4. Enter your API key from that provider

::: tip
You can connect multiple providers. If one fails, the next is tried automatically.
:::

## 3. Send your first email

::: code-group
```bash [cURL]
curl -X POST https://api.notificationhub.space/api/v1/notifications \
  -H "X-Api-Key: nhub_live_your_key_here" \
  -H "Content-Type: application/json" \
  -d '{
    "recipientEmail": "user@example.com",
    "type": "transactional",
    "channel": "email",
    "payload": "{\"subject\":\"Welcome!\",\"html\":\"<h1>Hello!</h1><p>Welcome to our platform.</p>\"}"
  }'
```

```typescript [TypeScript]
import { NotificationHub } from '@notificationhub/sdk'

const nh = new NotificationHub({ apiKey: 'nhub_live_your_key_here' })

const { publicId } = await nh.send({
  recipientEmail: 'user@example.com',
  type: 'transactional',
  channel: 'email',
  payload: JSON.stringify({
    subject: 'Welcome!',
    html: '<h1>Hello!</h1><p>Welcome to our platform.</p>',
  }),
})

console.log(`Sent: ${publicId}`)
```

```python [Python]
from notificationhub import NotificationHub

nh = NotificationHub(api_key="nhub_live_your_key_here")

result = nh.send(
    recipient_email="user@example.com",
    type="transactional",
    channel="email",
    payload={"subject": "Welcome!", "html": "<h1>Hello!</h1><p>Welcome to our platform.</p>"},
)
print(f"Sent: {result['publicId']}")
```
:::

## 4. Check delivery status

```bash
curl https://api.notificationhub.space/api/v1/notifications/{publicId} \
  -H "X-Api-Key: nhub_live_your_key_here"
```

The response includes `status` (`pending`, `processing`, `sent`, `failed`, `retrying`, `deadLetter`) and delivery logs.

## What's next?

- [Email Templates](/guide/templates) — create reusable designs
- [Campaigns](/guide/campaigns) — send bulk emails
- [Multi-Provider Fallback](/guide/providers) — set up automatic failover
