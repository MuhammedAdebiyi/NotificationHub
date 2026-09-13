# Python SDK

Official SDK for [NotificationHub](https://notificationhub.space).

## Installation

```bash
pip install notificationhub
```

## Setup

```python
from notificationhub import NotificationHub

nh = NotificationHub(api_key="nhub_live_your_key_here")
```

## Send an Email

```python
result = nh.send(
    recipient_email="user@example.com",
    type="transactional",
    channel="email",
    payload={"subject": "Welcome!", "html": "<h1>Hello!</h1><p>Thanks for signing up.</p>"},
)
print(result["publicId"])
```

## Check Status

```python
notification = nh.get_notification(result["publicId"])
print(notification["status"])  # "sent", "pending", "failed", etc.
```

## Create a Template

```python
template = nh.create_template(
    name="Welcome Email",
    subject="Welcome {{name}}!",
    body="<h1>Hello {{name}}</h1><p>Your account is ready.</p>",
)
```

## Run a Campaign

```python
# Create
campaign = nh.create_campaign(
    title="Product Launch",
    subject="New product announcement",
    body="<h1>Big news!</h1>",
)

# Add recipients
nh.add_campaign_recipients(campaign["id"], ["alice@example.com", "bob@example.com"])

# Send
nh.send_campaign(campaign["id"])

# Track progress
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

## Full API Reference

See the [Python SDK README](https://github.com/MuhammedAdebiyi/NotificationHub/tree/main/sdk/python) for the complete API.
