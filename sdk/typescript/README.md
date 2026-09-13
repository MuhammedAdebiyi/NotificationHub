# NotificationHub TypeScript SDK

Official TypeScript SDK for NotificationHub — multi-provider email notification platform

## Installation

```bash
npm install @notificationhub/sdk
```

## Quick Start

```typescript
import { NotificationHub } from "@notificationhub/sdk";

const client = new NotificationHub({
  apiKey: "your-api-key",
  baseUrl: "https://api.notificationhub.space",
});

const result = await client.notifications.send({
  to: "user@example.com",
  templateId: "welcome-email",
  variables: {
    name: "John Doe",
    loginUrl: "https://app.example.com/login",
  },
});

console.log("Notification sent:", result.id);
```

## Features

- Send notifications
- Retry failed notifications
- Create/list/delete templates
- Campaign progress & send

## Links

[API Docs](https://notificationhub.space/docs) | [Swagger Explorer](https://api.notificationhub.space/docs)
