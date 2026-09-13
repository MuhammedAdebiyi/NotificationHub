---
title: PHP SDK
---

# PHP SDK

Official PHP SDK for [NotificationHub](https://notificationhub.space).

## Installation

```bash
composer require notificationhub/sdk
```

## Quick Start

```php
<?php

use NotificationHub\SDK\Client;

$client = new Client('nhub_live_your_key_here');

$result = $client->send([
    'recipientEmail' => 'user@example.com',
    'type'           => 'transactional',
    'channel'        => 'email',
    'payload'        => [
        'subject' => 'Welcome!',
        'html'    => '<h1>Hello!</h1><p>Thanks for signing up.</p>',
    ],
]);

echo $result['publicId'];
```

## Configuration

```php
// Custom base URL and timeout
$client = new Client(
    apiKey:  'nhub_live_your_key',
    baseUrl: 'https://custom-api.example.com',
    timeout: 10.0
);
```

## API Reference

### SendNotification

Sends a notification to a recipient.

```php
$result = $client->send([
    'recipientEmail' => 'user@example.com',
    'type'           => 'transactional',
    'channel'        => 'email',
    'payload'        => [
        'subject' => 'Hello',
        'html'    => '<p>Hi there</p>',
    ],
]);
```

| Parameter | Type | Required | Description |
|---|---|---|---|
| `recipientEmail` | `string` | Yes | Recipient email address |
| `type` | `string` | Yes | `transactional`, `marketing`, or `system` |
| `channel` | `string` | Yes | `email`, `sms`, `push`, or `webhook` |
| `payload` | `string\|array` | Yes | Message payload (string or array) |

**Returns:** `array` with `publicId` key

### GetNotification

Retrieves a notification by its public ID.

```php
$notification = $client->getNotification('notif_abc123');
echo $notification['status']; // "sent", "pending", "failed", etc.
```

### ListNotifications

Lists notifications with pagination.

```php
$notifications = $client->listNotifications(page: 1, pageSize: 20);
foreach ($notifications['items'] as $n) {
    echo "{$n['publicId']}: {$n['status']}\n";
}
```

### RetryNotification

Retries a failed notification.

```php
$client->retryNotification('notif_abc123');
```

### CreateTemplate

Creates a new email template.

```php
$template = $client->createTemplate(
    'Welcome Email',
    'Welcome {{name}}!',
    '<h1>Hello {{name}}</h1><p>Your account is ready.</p>'
);
echo $template['id'];
```

### GetCampaignProgress

Gets real-time campaign delivery progress.

```php
$progress = $client->getCampaignProgress('camp_xyz789');
echo sprintf('%.1f%% sent (%d/%d)',
    $progress['progressPercent'],
    $progress['sent'],
    $progress['totalRecipients']
);
```

## Error Handling

```php
use NotificationHub\SDK\NotificationHubException;

try {
    $result = $client->send($params);
} catch (NotificationHubException $e) {
    echo "API error {$e->getCode()}: {$e->getMessage()}\n";
}
```

## Links

- [GitHub SDK](https://github.com/MuhammedAdebiyi/NotificationHub/tree/main/sdk/php)
- [API Reference](/api/notifications)
- [Swagger Explorer](https://api.notificationhub.space/docs)
