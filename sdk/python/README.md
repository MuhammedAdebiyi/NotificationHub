# NotificationHub Python SDK

Official Python SDK for NotificationHub — multi-provider email notification platform

## Installation

```bash
pip install notificationhub-sdk
```

## Quick Start

```python
from notificationhub import NotificationHubClient

client = NotificationHubClient(
    api_key="your-api-key",
    base_url="https://api.notificationhub.space",
)

result = client.notifications.send(
    to="user@example.com",
    template_id="welcome-email",
    variables={
        "name": "John Doe",
        "login_url": "https://app.example.com/login",
    },
)

print(f"Notification sent: {result.id}")
```

## Features

- Send notifications
- Retry failed notifications
- Create/list/delete templates
- Campaign progress & send

## Links

[API Docs](https://notificationhub.space/docs) | [Swagger Explorer](https://api.notificationhub.space/docs)
