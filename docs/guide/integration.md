# Integrating with Your App

This guide shows exactly how to integrate NotificationHub into your existing codebase — what files to create, where to put the code, and how it scales.

## The Problem (Before NotificationHub)

Most apps have email code scattered across the codebase:

```csharp
// ❌ OLD WAY: Templates hardcoded in C#
public static class EmailTemplateCatalog
{
    public static readonly EmailTemplate VerificationCode = new(
        Key: "verification-code",
        SubjectTemplate = "Verify your account",
        HtmlBodyTemplate = """<html>...{{code}}...</html>""",
        TextBodyTemplate = "Your code is {{code}}");
}

// Sending scattered across controllers
await SmtpClient.SendAsync(email, subject, html);  // in AuthController
await SendGrid.SendAsync(email, subject, html);     // in PaymentController
await Brevo.SendAsync(email, subject, html);        // in CampaignController
```

**Problems:**
- Templates mixed with business logic
- No delivery tracking or retry
- Can't change email design without redeploying
- No analytics on opens/clicks
- Switching providers = rewrite everything

## The Solution (With NotificationHub)

### Step 1: Create a NotificationHub Account

1. Sign up at [notificationhub.space](https://notificationhub.space/signup)
2. Go to **Settings → Email Providers** and connect your provider (Resend, SendGrid, Brevo, etc.)
3. Go to **Settings → API Keys** and create a key

### Step 2: Create Your Templates in the Dashboard

Instead of hardcoding HTML in C#, create templates in the NotificationHub dashboard:

1. Go to **Templates** → **+ New Template**
2. Name it `verification-code`
3. Design your email with the visual editor
4. Use `{{firstName}}`, `{{code}}`, etc. as placeholders
5. Save

Now your HTML lives in NotificationHub, not in your codebase.

### Step 3: Install the SDK

::: code-group
```bash [.NET]
dotnet add package NotificationHub.Client
```

```bash [Node.js / NestJS]
npm install @notificationhub/sdk
```

```bash [Go]
go get github.com/MuhammedAdebiyi/NotificationHub/sdk/go
```

```bash [Python]
pip install notificationhub
```

```bash [PHP]
composer require notificationhub/sdk
```
:::

### Step 4: Create a Notification Service File

Create one file that wraps the SDK. This is the **only file** your app touches:

::: code-group
```csharp [C# / .NET]
// Services/NotificationService.cs
using NotificationHub.Client;

namespace CourseVault.Services;

public class NotificationService
{
    private readonly NotificationHubClient _nh;

    public NotificationService(IConfiguration config)
    {
        _nh = new NotificationHubClient(
            config["NotificationHub:ApiKey"]!);
    }

    public async Task SendVerificationCode(
        string email, string firstName, string code)
    {
        await _nh.SendAsync(new
        {
            recipientEmail = email,
            type = "transactional",
            channel = "email",
            payload = JsonSerializer.Serialize(new
            {
                templateKey = "verification-code",
                firstName,
                code,
                expiresInMinutes = 10
            })
        });
    }

    public async Task SendPasswordReset(
        string email, string firstName, string resetUrl)
    {
        await _nh.SendAsync(new
        {
            recipientEmail = email,
            type = "transactional",
            channel = "email",
            payload = JsonSerializer.Serialize(new
            {
                templateKey = "password-reset",
                firstName,
                resetUrl,
                expiresInMinutes = 30
            })
        });
    }

    public async Task SendWelcome(
        string email, string firstName)
    {
        await _nh.SendAsync(new
        {
            recipientEmail = email,
            type = "transactional",
            channel = "email",
            payload = JsonSerializer.Serialize(new
            {
                templateKey = "welcome",
                firstName
            })
        });
    }
}
```

```typescript [TypeScript / NestJS]
// src/notification.service.ts
import { NotificationHub } from '@notificationhub/sdk'

export class NotificationService {
  private nh: NotificationHub

  constructor() {
    this.nh = new NotificationHub({
      apiKey: process.env.NOTIFICATIONHUB_API_KEY!,
    })
  }

  async sendVerificationCode(
    email: string, firstName: string, code: string
  ) {
    return this.nh.send({
      recipientEmail: email,
      type: 'transactional',
      channel: 'email',
      payload: JSON.stringify({
        templateKey: 'verification-code',
        firstName,
        code,
        expiresInMinutes: 10,
      }),
    })
  }

  async sendPasswordReset(
    email: string, firstName: string, resetUrl: string
  ) {
    return this.nh.send({
      recipientEmail: email,
      type: 'transactional',
      channel: 'email',
      payload: JSON.stringify({
        templateKey: 'password-reset',
        firstName,
        resetUrl,
        expiresInMinutes: 30,
      }),
    })
  }

  async sendWelcome(email: string, firstName: string) {
    return this.nh.send({
      recipientEmail: email,
      type: 'transactional',
      channel: 'email',
      payload: JSON.stringify({
        templateKey: 'welcome',
        firstName,
      }),
    })
  }
}
```

```go [Go]
// services/notification.go
package services

import (
    "context"
    nh "github.com/MuhammedAdebiyi/NotificationHub/sdk/go"
)

type NotificationService struct {
    client *nh.Client
}

func NewNotificationService(apiKey string) *NotificationService {
    return &NotificationService{
        client: nh.NewClient(apiKey),
    }
}

func (s *NotificationService) SendVerificationCode(
    ctx context.Context, email, firstName, code string,
) error {
    _, err := s.client.Send(ctx, &nh.SendParams{
        RecipientEmail: email,
        Type:           "transactional",
        Channel:        "email",
        Payload: map[string]interface{}{
            "templateKey":       "verification-code",
            "firstName":         firstName,
            "code":              code,
            "expiresInMinutes":  10,
        },
    })
    return err
}
```

```python [Python]
# services/notification.py
from notificationhub import NotificationHub

class NotificationService:
    def __init__(self):
        self.nh = NotificationHub(
            api_key=os.environ["NOTIFICATIONHUB_API_KEY"]
        )

    def send_verification_code(self, email, first_name, code):
        self.nh.send(
            recipient_email=email,
            type="transactional",
            channel="email",
            payload={
                "templateKey": "verification-code",
                "firstName": first_name,
                "code": code,
                "expiresInMinutes": 10,
            },
        )

    def send_password_reset(self, email, first_name, reset_url):
        self.nh.send(
            recipient_email=email,
            type="transactional",
            channel="email",
            payload={
                "templateKey": "password-reset",
                "firstName": first_name,
                "resetUrl": reset_url,
                "expiresInMinutes": 30,
            },
        )
```

```php [PHP]
// src/Service/NotificationService.php
use NotificationHub\SDK;

class NotificationService
{
    private SDK\Client $nh;

    public function __construct(string $apiKey)
    {
        $this->nh = new SDK\Client($apiKey);
    }

    public function sendVerificationCode(
        string $email, string $firstName, string $code
    ): void {
        $this->nh->send([
            'recipientEmail' => $email,
            'type' => 'transactional',
            'channel' => 'email',
            'payload' => json_encode([
                'templateKey' => 'verification-code',
                'firstName' => $firstName,
                'code' => $code,
                'expiresInMinutes' => 10,
            ]),
        ]);
    }
}
```
:::

### Step 5: Replace Your Old Email Calls

Find every place in your code that sends email and replace it:

::: code-group
```csharp [C# Before → After]
// ❌ BEFORE: Direct SMTP/Provider call
await SmtpClient.SendAsync(email,
    "Verify your account",
    EmailTemplateCatalog.VerificationCode.HtmlBodyTemplate
        .Replace("{{code}}", code)
        .Replace("{{firstName}}", firstName));

// ✅ AFTER: One line
await _notificationService.SendVerificationCode(email, firstName, code);
```

```typescript [TypeScript Before → After]
// ❌ BEFORE
await transporter.sendMail({
  to: email,
  subject: 'Verify your account',
  html: verificationTemplate({ firstName, code }),
})

// ✅ AFTER
await notificationService.sendVerificationCode(email, firstName, code)
```
:::

### Step 6: Add to Environment Variables

```bash
# .env
NOTIFICATIONHUB_API_KEY=nhub_live_your_key_here
```

## Where Each Piece Lives

```
YourApp/
├── Services/
│   └── NotificationService.ts    ← ONE file, wraps the SDK
├── Controllers/
│   └── AuthController.ts         ← calls NotificationService
├── .env                          ← API key lives here
└── ...
```

**You only touch `NotificationService.ts`** when adding new email types. Controllers just call simple methods like `sendVerificationCode()`.

## Does It Scale?

**Yes.** Here's why:

| Concern | How NotificationHub handles it |
|---------|-------------------------------|
| **10,000+ emails/day** | Queued async — API responds in <50ms, worker processes in background |
| **Provider downtime** | Automatic fallback — Resend fails → tries SendByte → tries SendGrid |
| **Failed emails** | Auto-retry with exponential backoff (5 attempts) |
| **Duplicate sends** | Idempotency key prevents duplicates |
| **Team growth** | API keys per team member, role-based access |
| **Template changes** | Update in dashboard, no code deploy needed |
| **Analytics** | Open tracking, delivery rates, failure reasons — all in dashboard |

### Performance

```
Your app → POST /api/v1/notifications → 200 OK (12ms)
                  ↓
            Redis Queue
                  ↓
            Worker picks up → sends via provider → done
```

Your app never waits for email delivery. The API call is instant.

### Cost

- **Free tier**: 10,000 notifications/month
- **Pro**: Unlimited
- You only pay for your email provider (Resend, SendGrid, etc.) — NotificationHub is free on the platform plan

## Comparison: Before vs After

| | Before (DIY) | After (NotificationHub) |
|---|---|---|
| **Templates** | Hardcoded in C# | Visual editor in dashboard |
| **Send email** | `SmtpClient.SendAsync(...)` | `nh.send(...)` |
| **Retry logic** | Write it yourself | Built-in exponential backoff |
| **Fallback** | Pick one provider | Multi-provider automatic |
| **Delivery tracking** | Build it yourself | Open tracking, logs, analytics |
| **Template updates** | Redeploy your app | Edit in dashboard, instant |
| **New email types** | Add to EmailTemplateCatalog + render + send | Add template in dashboard, call API |
