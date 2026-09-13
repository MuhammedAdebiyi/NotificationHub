# Email Templates

Templates let you design reusable email layouts with `{{variable}}` placeholders.

## Creating a Template

In the dashboard:
1. Go to **Templates**
2. Click **+ New Template**
3. Use the visual editor or write raw HTML
4. Save

Via API:

```bash
curl -X POST https://api.notificationhub.space/api/v1/templates \
  -H "X-Api-Key: nhub_live_your_key" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Welcome Email",
    "subject": "Welcome, {{firstName}}!",
    "body": "<h1>Hi {{firstName}}</h1><p>Your {{planName}} plan is active. Start building!</p>"
  }'
```

## Using Variables

Variables use `{{doubleCurlyBraces}}` syntax:

```html
<h1>Hello {{firstName}}!</h1>
<p>Your order #{{orderId}} is confirmed.</p>
<p>Total: ${{amount}}</p>
```

When sending, pass the variable values in the `payload`:

```json
{
  "subject": "Order Confirmed",
  "html": "<h1>Hello John!</h1><p>Your order #12345 is confirmed.</p><p>Total: $99.00</p>"
}
```

## Best Practices

- Keep subject lines under 50 characters
- Use responsive HTML with inline styles
- Include a plain-text fallback
- Test with multiple email clients
- Use preview text for better open rates
