---
title: Resources
---

# Resources

Guides, tools, and references to help you get the most out of NotificationHub.

## Quick Links

| Resource | Description |
|----------|-------------|
| [Dashboard](https://notificationhub.space) | Manage notifications, templates, campaigns, and settings |
| [API Reference](/api/) | Full REST API documentation |
| [Swagger Explorer](https://api.notificationhub.space/docs) | Interactive API explorer |
| [GitHub](https://github.com/MuhammedAdebiyi/NotificationHub) | Source code, issues, and contributions |
| [Status Page](https://status.notificationhub.space) | Current system status and uptime |

## SDKs & Libraries

Official client libraries for your language:

| Language | Repository | Package |
|----------|------------|---------|
| TypeScript | [sdk/typescript](https://github.com/MuhammedAdebiyi/NotificationHub/tree/main/sdk/typescript) | `npm install @notificationhub/sdk` |
| Python | [sdk/python](https://github.com/MuhammedAdebiyi/NotificationHub/tree/main/sdk/python) | `pip install notificationhub` |
| Go | [sdk/go](https://github.com/MuhammedAdebiyi/NotificationHub/tree/main/sdk/go) | `go get github.com/MuhammedAdebiyi/NotificationHub/sdk/go` |
| C# | [sdk/csharp](https://github.com/MuhammedAdebiyi/NotificationHub/tree/main/sdk/csharp) | `dotnet add package NotificationHub.Client` |
| NestJS | [sdk/nestjs](https://github.com/MuhammedAdebiyi/NotificationHub/tree/main/sdk/nestjs) | `npm install @notificationhub/nestjs` |
| PHP | [sdk/php](https://github.com/MuhammedAdebiyi/NotificationHub/tree/main/sdk/php) | `composer require notificationhub/sdk` |

## Templates Library

Ready-to-use email templates you can copy and customize:

### Order Confirmation

```html
<h1>Hi {{firstName}}!</h1>
<p>Your order <strong>#{{orderId}}</strong> is confirmed.</p>
<p>Total: <strong>${{amount}}</strong></p>
<p>We'll send you a tracking number when your order ships.</p>
```

### Welcome Email

```html
<h1>Welcome to {{companyName}}, {{firstName}}!</h1>
<p>Your account is ready. Here's what you can do:</p>
<ul>
  <li>Set up your profile</li>
  <li>Connect your email provider</li>
  <li>Send your first notification</li>
</ul>
```

### Password Reset

```html
<h1>Password Reset</h1>
<p>Hi {{firstName}},</p>
<p>Click the link below to reset your password:</p>
<p><a href="{{resetUrl}}" style="background:#9333ea;color:white;padding:12px 24px;border-radius:8px;text-decoration:none;display:inline-block;">Reset Password</a></p>
<p>This link expires in 24 hours.</p>
```

### Campaign Newsletter

```html
<h1>{{month}} Update</h1>
<p>Hi {{firstName}},</p>
<p>Here's what happened this month:</p>
<h2>New Features</h2>
<p>{{features}}</p>
<h2>Coming Soon</h2>
<p>{{upcoming}}</p>
<p>Thanks for being a customer!</p>
```

## Integration Recipes

### Node.js + Express

```javascript
import { NotificationHub } from '@notificationhub/sdk';

const nh = new NotificationHub(process.env.NH_API_KEY);

app.post('/api/orders', async (req, res) => {
  const order = await createOrder(req.body);

  await nh.send({
    recipientEmail: req.body.email,
    type: 'transactional',
    channel: 'email',
    payload: JSON.stringify({
      subject: `Order #${order.id} confirmed`,
      html: `<h1>Thanks!</h1><p>Order #${order.id} is confirmed. Total: $${order.total}</p>`
    })
  });

  res.json(order);
});
```

### Python + FastAPI

```python
from notificationhub import NotificationHubClient

nh = NotificationHubClient(api_key="nhub_live_your_key")

@app.post("/api/orders")
async def create_order(order: Order):
    db_order = await save_order(order)

    nh.send({
        "recipientEmail": order.email,
        "type": "transactional",
        "channel": "email",
        "payload": {
            "subject": f"Order #{db_order.id} confirmed",
            "html": f"<h1>Thanks!</h1><p>Order #{db_order.id} is confirmed.</p>"
        }
    })

    return db_order
```

### Go + net/http

```go
package main

import (
    "context"
    nh "github.com/MuhammedAdebiyi/NotificationHub/sdk/go"
)

func main() {
    client := nh.NewClient("nhub_live_your_key")

    result, _ := client.Send(context.Background(), &nh.SendParams{
        RecipientEmail: "user@example.com",
        Type:           "transactional",
        Channel:        "email",
        Payload: map[string]interface{}{
            "subject": "Order confirmed",
            "html":    "<h1>Thanks!</h1><p>Your order is confirmed.</p>",
        },
    })

    fmt.Println("Sent:", result.PublicID)
}
```

### PHP + Laravel

```php
use NotificationHub\SDK\Client;

$client = new Client('nhub_live_your_key');

// In a controller
$client->send([
    'recipientEmail' => $request->email,
    'type'           => 'transactional',
    'channel'        => 'email',
    'payload'        => [
        'subject' => 'Order confirmed',
        'html'    => '<h1>Thanks!</h1><p>Your order is confirmed.</p>',
    ],
]);
```

## Environment Variables Reference

| Variable | Description | Required |
|----------|-------------|----------|
| `NhDb__ConnectionString` | PostgreSQL connection string | Yes |
| `NhRedis__Configuration` | Redis connection string | Yes |
| `NhJwt__Secret` | JWT signing secret | Yes |
| `NhSendByte__ApiKey` | SendByte API key | For SendByte provider |
| `NhResend__ApiKey` | Resend API key | For Resend provider |
| `NhSendGrid__ApiKey` | SendGrid API key | For SendGrid provider |
| `NhBrevo__ApiKey` | Brevo API key | For Brevo provider |

## Architecture

NotificationHub uses a Clean Architecture pattern:

```
NotificationHub.Api          → REST API controllers, middleware
NotificationHub.Application  → Business logic, interfaces
NotificationHub.Domain       → Entities, value objects
NotificationHub.Infrastructure → DB, email providers, caching
NotificationHub.Shared       → DTOs, constants, events
NotificationHub.Worker       → Background job processor
```

- **API** handles HTTP requests, authentication, validation
- **Worker** processes queued notifications, retries failed messages
- **Infrastructure** implements email providers, database access, Redis caching
- **Domain** defines core entities: Notification, Template, Campaign, EmailProviderConfig

## Community Projects

Open-source tools and integrations built by the community:

| Project | Description |
|---------|-------------|
| [notificationhub-python-callback](https://github.com/topics/notificationhub) | Webhook receiver example in Python |

Building something with NotificationHub? [Open a PR](https://github.com/MuhammedAdebiyi/NotificationHub) to add it here.

## Support

- **Documentation** — you're here
- **GitHub Issues** — [report bugs or request features](https://github.com/MuhammedAdebiyi/NotificationHub/issues)
- **Email** — support@notificationhub.space
