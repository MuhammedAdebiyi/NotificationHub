# NotificationHub PHP SDK

Official PHP SDK for NotificationHub — multi-provider email notification platform

## Installation

```bash
composer require notificationhub/sdk-php
```

## Quick Start

```php
<?php

use NotificationHub\SDK\Client;

$client = new Client(
    apiKey: 'your-api-key',
    baseUrl: 'https://api.notificationhub.space',
);

$result = $client->notifications()->send([
    'to' => 'user@example.com',
    'templateId' => 'welcome-email',
    'variables' => [
        'name' => 'John Doe',
        'login_url' => 'https://app.example.com/login',
    ],
]);

echo "Notification sent: {$result->id}";
```

## Features

- Send notifications
- Retry failed notifications
- Create/list/delete templates
- Campaign progress & send

## Links

[API Docs](https://notificationhub.space/docs) | [Swagger Explorer](https://api.notificationhub.space/docs)
