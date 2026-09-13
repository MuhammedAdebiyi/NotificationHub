# Authentication

NotificationHub supports two authentication methods:

## API Key (recommended for integrations)

API keys are the simplest way to authenticate server-side requests.

### Creating an API key

1. Go to **Settings → API Keys** in the dashboard
2. Click **+ Create**
3. Enter a name (e.g. "Production Backend")
4. Copy the key — **it is shown only once**

Key format: `nhub_live_<32-char-random>`

### Using the API key

Pass it as the `X-Api-Key` header:

```bash
curl -H "X-Api-Key: nhub_live_your_key_here" \
  https://api.notificationhub.space/api/v1/notifications
```

::: warning
API keys are org-scoped. They have full access to your organization's notifications, templates, campaigns, and settings. Keep them secret.
:::

## JWT Bearer Token (for dashboard users)

The web dashboard uses JWT tokens for authentication. This is handled automatically when using the dashboard.

For programmatic access, use API keys instead.

## Permission Levels

| Auth Method | Role | Access |
|-------------|------|--------|
| API Key | `service` | Full access to notifications, templates, campaigns |
| JWT (owner) | `owner` | Full access + org settings + team management |
| JWT (admin) | `admin` | Full access + team management |
| JWT (member) | `member` | Send notifications, view campaigns |
