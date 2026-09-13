# TypeScript / JavaScript SDK

Official SDK for [NotificationHub](https://notificationhub.space).

## Installation

```bash
npm install @notificationhub/sdk
```

## Setup

```typescript
import { NotificationHub } from '@notificationhub/sdk'

const nh = new NotificationHub({
  apiKey: 'nhub_live_your_key_here',
})
```

## Send an Email

```typescript
const { publicId } = await nh.send({
  recipientEmail: 'user@example.com',
  type: 'transactional',
  channel: 'email',
  payload: JSON.stringify({
    subject: 'Welcome!',
    html: '<h1>Hello!</h1><p>Thanks for signing up.</p>',
  }),
})
```

## Check Status

```typescript
const notification = await nh.getNotification(publicId)
console.log(notification.status) // "sent", "pending", "failed", etc.
```

## Create a Template

```typescript
const { id } = await nh.createTemplate(
  'Welcome Email',
  'Welcome {{name}}!',
  '<h1>Hello {{name}}</h1><p>Your account is ready.</p>',
)
```

## Run a Campaign

```typescript
// Create
const { id } = await nh.createCampaign({
  title: 'Product Launch',
  subject: 'New product announcement',
  body: '<h1>Big news!</h1>',
})

// Add recipients
await nh.addCampaignRecipients(id, ['alice@example.com', 'bob@example.com'])

// Send
await nh.sendCampaign(id)

// Track progress
const progress = await nh.getCampaignProgress(id)
console.log(`${progress.progressPercent}% sent`)
```

## Error Handling

```typescript
import { NotificationHub, NotificationHubError } from '@notificationhub/sdk'

try {
  await nh.send({ ... })
} catch (err) {
  if (err instanceof NotificationHubError) {
    console.error(`API error ${err.statusCode}: ${err.message}`)
  }
}
```

## Full API Reference

See the [TypeScript SDK README](https://github.com/MuhammedAdebiyi/NotificationHub/tree/main/sdk/typescript) for the complete API.
