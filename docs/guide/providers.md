# Multi-Provider Fallback

NotificationHub supports multiple email providers with automatic failover.

## How it works

When you send an email:
1. The **default** provider is tried first
2. If it fails (timeout, API error, rate limit), the **next** provider is tried
3. This continues until one succeeds or all fail

```
Send email
  → Try Resend (default)
    → Failed: 503
  → Try SendByte (fallback)
    → Success!
```

## Setting up multiple providers

1. Go to **Settings → Email Providers**
2. Click **+ Add Provider**
3. Add your first provider — it becomes the default
4. Click **+ Add Provider** again to add fallbacks
5. Use **Set default** to change which provider is primary

## Supported Providers

| Provider | API Key Format | Domain Verification |
|----------|---------------|-------------------|
| **SendByte** | `sk_live_...` | Via SendByte dashboard |
| **Resend** | `re_...` | Via Resend dashboard |
| **SendGrid** | `SG...` | Via SendGrid dashboard |
| **Brevo** | `xkeysib-...` | Via Brevo dashboard |
| **SMTP** | `host|port|username|password` | N/A |

## Provider Health

The API tracks which providers are healthy. If a provider's API key expires or is revoked, it's automatically skipped during fallback.

You can check provider health in **Settings → Email Providers** or via the API:

```bash
curl https://api.notificationhub.space/api/v1/org/email-provider/domains \
  -H "Authorization: Bearer your_jwt_token"
```

Response includes `providerHealth` array with `isHealthy` status for each provider.

## SMTP Configuration

For custom SMTP servers, use the pipe-delimited format:

```
smtp.example.com|587|username|password
```

Set this as the API key when adding an SMTP provider.
