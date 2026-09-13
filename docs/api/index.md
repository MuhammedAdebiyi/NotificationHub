# API Reference

Base URL: `https://api.notificationhub.space`

All endpoints use versioned routes: `api/v{version}/...` (current version: `v1`).

## Interactive Explorer

The full API is documented with an interactive Swagger UI:

**[Open API Explorer →](https://api.notificationhub.space/docs)**

You can try requests directly from the browser with your API key.

## Endpoints

### Notifications
| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/v1/notifications` | Send a notification |
| `GET` | `/api/v1/notifications` | List notifications (paginated) |
| `GET` | `/api/v1/notifications/{id}` | Get notification detail |
| `GET` | `/api/v1/notifications/{id}/logs` | Get delivery logs |
| `POST` | `/api/v1/notifications/{id}/retry` | Retry failed notification |

### Templates
| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/v1/templates` | Create a template |
| `GET` | `/api/v1/templates` | List templates |
| `GET` | `/api/v1/templates/{id}` | Get template detail |
| `PUT` | `/api/v1/templates/{id}` | Update a template |
| `DELETE` | `/api/v1/templates/{id}` | Delete a template |

### Campaigns
| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/v1/campaigns` | Create a campaign |
| `GET` | `/api/v1/campaigns` | List campaigns |
| `GET` | `/api/v1/campaigns/{id}` | Get campaign detail |
| `GET` | `/api/v1/campaigns/{id}/progress` | Get send progress |
| `POST` | `/api/v1/campaigns/{id}/recipients` | Add recipients |
| `POST` | `/api/v1/campaigns/{id}/send` | Send immediately |
| `POST` | `/api/v1/campaigns/{id}/schedule` | Schedule for later |
| `POST` | `/api/v1/campaigns/{id}/pause` | Pause sending |
| `POST` | `/api/v1/campaigns/{id}/resume` | Resume sending |
| `DELETE` | `/api/v1/campaigns/{id}` | Delete campaign |

## Response Format

All responses are JSON. Successful responses return the data directly. Errors return:

```json
{
  "error": "Description of what went wrong"
}
```

## Rate Limits

- **100 requests/second** per API key
- **1,000 notifications/minute** per organization
- **100,000 notifications/day** on the free plan
