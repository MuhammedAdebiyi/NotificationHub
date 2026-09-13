# NotificationHub Python SDK

Official Python SDK for [NotificationHub](https://notificationhub.space) — multi-provider email notification platform.

## Installation

```bash
pip install notificationhub
```

## Quick Start

```python
from notificationhub import NotificationHub

nh = NotificationHub(api_key="nhub_live_your_api_key_here")

# Send an email notification
result = nh.send(
    recipient_email="user@example.com",
    type="transactional",
    channel="email",
    payload={"subject": "Welcome!", "html": "<h1>Welcome to our app</h1>"},
)
print(f"Notification sent: {result['publicId']}")

# Check delivery status
notification = nh.get_notification(result["publicId"])
print(f"Status: {notification['status']}")
```

## API Reference

### Notifications

#### `nh.send(...)`

```python
result = nh.send(
    recipient_email="user@example.com",
    type="transactional",
    channel="email",
    payload={"subject": "Hello", "html": "<p>Hello!</p>"},
    idempotency_key="unique-id-123",  # optional, prevents duplicates
)
```

#### `nh.get_notification(public_id)`

Get full notification detail including delivery logs.

#### `nh.list_notifications(page=1, page_size=20, status="sent", date_from="2024-01-01")`

List notifications with filtering.

#### `nh.retry_notification(public_id)`

Retry a failed notification.

### Templates

```python
# Create a template with {{variable}} placeholders
template = nh.create_template(
    name="Welcome Email",
    subject="Welcome {{name}}!",
    body="<h1>Hello {{name}}</h1><p>Your account is ready.</p>",
)

# Use the template ID in notifications
nh.send(
    recipient_email="user@example.com",
    type="transactional",
    channel="email",
    payload=template["id"],  # reference template by ID
)
```

### Campaigns

```python
# Create and send a bulk campaign
campaign = nh.create_campaign(
    title="Product Launch",
    subject="Introducing our new product",
    body="<h1>New Product</h1>",
)

# Add recipients
nh.add_campaign_recipients(campaign["id"], [
    "alice@example.com",
    "bob@example.com",
])

# Send immediately
nh.send_campaign(campaign["id"])

# Or check progress
progress = nh.get_campaign_progress(campaign["id"])
print(f"{progress['progressPercent']}% sent")
```

## Error Handling

```python
from notificationhub import NotificationHub, NotificationHubError

try:
    nh.send(...)
except NotificationHubError as e:
    print(f"API error {e.status_code}: {e.message}")
```

## License

MIT
