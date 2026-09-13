---
title: NestJS SDK
---

# NestJS SDK

Official NestJS module for [NotificationHub](https://notificationhub.space).

## Installation

```bash
npm install @notificationhub/nestjs
```

## Quick Start

```typescript
// app.module.ts
import { NotificationHubModule } from '@notificationhub/nestjs';

@Module({
  imports: [
    NotificationHubModule.forRoot({
      apiKey: 'nhub_live_your_key_here',
    }),
  ],
})
export class AppModule {}
```

```typescript
// notification.service.ts
import { Injectable } from '@nestjs/common';
import { NotificationHubService } from '@notificationhub/nestjs';

@Injectable()
export class NotificationService {
  constructor(private readonly nh: NotificationHubService) {}

  async sendWelcome(email: string) {
    const result = await this.nh.send({
      recipientEmail: email,
      type: 'transactional',
      channel: 'email',
      payload: {
        subject: 'Welcome!',
        html: '<h1>Hello!</h1><p>Thanks for signing up.</p>',
      },
    });
    return result.publicId;
  }
}
```

## Configuration

### Static Configuration

```typescript
NotificationHubModule.forRoot({
  apiKey: 'nhub_live_your_key',
  baseUrl: 'https://custom-api.example.com',
  timeout: 10000,
})
```

### Async Configuration

```typescript
NotificationHubModule.forRootAsync({
  useFactory: async (config: ConfigService) => ({
    apiKey: config.get('NOTIFICATION_HUB_API_KEY'),
    timeout: config.get('NOTIFICATION_HUB_TIMEOUT', 30000),
  }),
  inject: [ConfigService],
})
```

## API Reference

### SendNotification

Sends a notification to a recipient.

```typescript
const result = await this.nh.send({
  recipientEmail: 'user@example.com',
  type: 'transactional',
  channel: 'email',
  payload: {
    subject: 'Hello',
    html: '<p>Hi there</p>',
  },
});
```

| Parameter | Type | Required | Description |
|---|---|---|---|
| `recipientEmail` | `string` | Yes | Recipient email address |
| `type` | `string` | Yes | `transactional`, `marketing`, or `system` |
| `channel` | `string` | Yes | `email`, `sms`, `push`, or `webhook` |
| `payload` | `string \| Record` | Yes | Message payload (string or object) |

**Returns:** `SendResult` with `publicId string`

### GetNotification

Retrieves a notification by its public ID.

```typescript
const notification = await this.nh.getNotification('notif_abc123');
console.log(notification.status); // "sent", "pending", "failed", etc.
```

### ListNotifications

Lists notifications with pagination.

```typescript
const notifications = await this.nh.listNotifications(1, 20);
for (const n of notifications.items) {
  console.log(`${n.publicId}: ${n.status}`);
}
```

### RetryNotification

Retries a failed notification.

```typescript
await this.nh.retryNotification('notif_abc123');
```

### CreateTemplate

Creates a new email template.

```typescript
const template = await this.nh.createTemplate(
  'Welcome Email',
  'Welcome {{name}}!',
  '<h1>Hello {{name}}</h1><p>Your account is ready.</p>',
);
console.log(template.id);
```

### GetCampaignProgress

Gets real-time campaign delivery progress.

```typescript
const progress = await this.nh.getCampaignProgress('camp_xyz789');
console.log(`${progress.progressPercent}% sent (${progress.sent}/${progress.totalRecipients})`);
```

## Error Handling

```typescript
try {
  await this.nh.send(params);
} catch (error) {
  console.error(`NotificationHub error: ${error.message}`);
}
```

## Links

- [GitHub SDK](https://github.com/MuhammedAdebiyi/NotificationHub/tree/main/sdk/nestjs)
- [API Reference](/api/notifications)
- [Swagger Explorer](https://api.notificationhub.space/docs)
