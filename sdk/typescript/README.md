# @notificationhub/sdk

Official TypeScript/JavaScript SDK for [NotificationHub](https://notificationhub.space) — multi-provider email notification platform.

## Installation

```bash
npm install @notificationhub/sdk
```

## Quick Start

```typescript
import { NotificationHub } from '@notificationhub/sdk'

const nh = new NotificationHub({
  apiKey: 'nhub_live_your_api_key_here',
})

// Send an email notification
const { publicId } = await nh.send({
  recipientEmail: 'user@example.com',
  type: 'transactional',
  channel: 'email',
  payload: JSON.stringify({
    subject: 'Welcome!',
    html: '<h1>Welcome to our app</h1><p>Thanks for signing up.</p>',
  }),
})

console.log(`Notification sent: ${publicId}`)

// Check delivery status
const notification = await nh.getNotification(publicId)
console.log(`Status: ${notification.status}`)
```

## API Reference

### `new NotificationHub(config)`

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `apiKey` | `string` | Yes | Your API key from Settings → API Keys |
| `baseUrl` | `string` | No | Default: `https://api.notificationhub.space` |

### Notifications

#### `nh.send(params)`

Send a notification. Returns `{ publicId }`.

```typescript
await nh.send({
  recipientEmail: 'user@example.com',
  type: 'transactional',
  channel: 'email',
  payload: JSON.stringify({ subject: 'Hello', html: '<p>Hello!</p>' }),
  idempotencyKey: 'unique-request-id-123', // optional, prevents duplicates
})
```

#### `nh.getNotification(publicId)`

Get full notification detail including delivery logs.

#### `nh.listNotifications(options?)`

List notifications with filtering.

```typescript
const { items, totalCount } = await nh.listNotifications({
  page: 1,
  pageSize: 50,
  status: 'sent',
  dateFrom: '2024-01-01',
  dateTo: '2024-12-31',
})
```

#### `nh.retryNotification(publicId)`

Retry a failed notification.

### Templates

#### `nh.createTemplate(name, subject, body)`

Create a reusable email template with `{{variable}}` placeholders.

```typescript
const { id } = await nh.createTemplate(
  'Welcome Email',
  'Welcome {{name}}!',
  '<h1>Hello {{name}}</h1><p>Your account is ready.</p>',
)
```

#### `nh.getTemplate(id)`, `nh.listTemplates()`, `nh.updateTemplate(...)`, `nh.deleteTemplate(id)`

### Campaigns

#### `nh.createCampaign(params)`

Create a bulk email campaign.

```typescript
const { id } = await nh.createCampaign({
  title: 'Product Launch',
  subject: 'Introducing our new product',
  body: '<h1>New Product</h1>',
})

// Add recipients
await nh.addCampaignRecipients(id, ['alice@example.com', 'bob@example.com'])

// Send immediately
await nh.sendCampaign(id)
```

#### `nh.getCampaignProgress(id)`

Get real-time send progress.

```typescript
const progress = await nh.getCampaignProgress(id)
console.log(`${progress.progressPercent}% sent (${progress.sent}/${progress.totalRecipients})`)
```

#### `nh.pauseCampaign(id)`, `nh.resumeCampaign(id)`, `nh.scheduleCampaign(id, iso)`, `nh.deleteCampaign(id)`

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

## License

MIT
