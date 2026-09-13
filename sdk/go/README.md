# NotificationHub Go SDK

Official Go SDK for NotificationHub — multi-provider email notification platform

## Installation

```bash
go get github.com/notificationhub/sdk-go
```

## Quick Start

```go
package main

import (
    "fmt"
    nh "github.com/notificationhub/sdk-go"
)

func main() {
    client := nh.NewClient("your-api-key", "https://api.notificationhub.space")

    result, err := client.Notifications.Send(&nh.SendRequest{
        To:         "user@example.com",
        TemplateID: "welcome-email",
        Variables: map[string]string{
            "name":      "John Doe",
            "login_url": "https://app.example.com/login",
        },
    })
    if err != nil {
        panic(err)
    }

    fmt.Printf("Notification sent: %s\n", result.ID)
}
```

## Features

- Send notifications
- Retry failed notifications
- Create/list/delete templates
- Campaign progress & send

## Links

[API Docs](https://notificationhub.space/docs) | [Swagger Explorer](https://api.notificationhub.space/docs)
