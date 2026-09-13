# NotificationHub NestJS SDK

Official NestJS SDK for NotificationHub — multi-provider email notification platform

## Installation

```bash
npm install @notificationhub/nestjs-sdk
```

## Quick Start

```typescript
// app.module.ts
import { NotificationHubModule } from "@notificationhub/nestjs-sdk";

@Module({
  imports: [
    NotificationHubModule.forRoot({
      apiKey: "your-api-key",
      baseUrl: "https://api.notificationhub.space",
    }),
  ],
})
export class AppModule {}
```

```typescript
// notifications.service.ts
import { NotificationHubService } from "@notificationhub/nestjs-sdk";

@Injectable()
export class NotificationsService {
  constructor(private readonly nh: NotificationHubService) {}

  async sendWelcomeEmail(to: string, name: string) {
    return this.nh.notifications.send({
      to,
      templateId: "welcome-email",
      variables: { name, loginUrl: "https://app.example.com/login" },
    });
  }
}
```

## Features

- Send notifications
- Retry failed notifications
- Create/list/delete templates
- Campaign progress & send

## Links

[API Docs](https://notificationhub.space/docs) | [Swagger Explorer](https://api.notificationhub.space/docs)
