"""NotificationHub API client."""

from __future__ import annotations

import json
import urllib.request
import urllib.error
from typing import Any, Optional

from .exceptions import NotificationHubError, AuthenticationError, NotFoundError, ConflictError


class NotificationHub:
    """Official Python SDK for NotificationHub.

    Usage::

        from notificationhub import NotificationHub

        nh = NotificationHub(api_key="nhub_live_your_key_here")

        # Send an email
        result = nh.send(
            recipient_email="user@example.com",
            type="transactional",
            channel="email",
            payload={"subject": "Welcome!", "html": "<h1>Hello!</h1>"},
        )
        print(result["publicId"])

    Args:
        api_key: Your API key from Settings → API Keys.
        base_url: API base URL. Defaults to ``https://api.notificationhub.space``.
    """

    def __init__(self, api_key: str, base_url: str = "https://api.notificationhub.space"):
        if not api_key:
            raise ValueError("API key is required. Get one at https://notificationhub.space/settings")
        self.api_key = api_key
        self.base_url = base_url.rstrip("/")

    def _request(self, method: str, path: str, body: dict | None = None, headers: dict | None = None) -> Any:
        url = f"{self.base_url}{path}"
        req_headers = {
            "X-Api-Key": self.api_key,
            "Content-Type": "application/json",
        }
        if headers:
            req_headers.update(headers)

        data = json.dumps(body).encode() if body else None
        req = urllib.request.Request(url, data=data, headers=req_headers, method=method)

        try:
            with urllib.request.urlopen(req) as resp:
                return json.loads(resp.read().decode())
        except urllib.error.HTTPError as e:
            body_text = e.read().decode()
            try:
                error_body = json.loads(body_text)
            except json.JSONDecodeError:
                error_body = {"error": body_text}

            msg = error_body.get("error", f"HTTP {e.code}")

            if e.code == 401:
                raise AuthenticationError(msg, e.code, error_body)
            elif e.code == 404:
                raise NotFoundError(msg, e.code, error_body)
            elif e.code == 409:
                raise ConflictError(msg, e.code, error_body)
            else:
                raise NotificationHubError(msg, e.code, error_body)

    # ── Notifications ──────────────────────────────────────────

    def send(
        self,
        recipient_email: str,
        type: str,
        channel: str,
        payload: dict | str,
        idempotency_key: str | None = None,
    ) -> dict:
        """Send a notification.

        Args:
            recipient_email: Recipient email address.
            type: Notification type (e.g. "transactional", "marketing").
            channel: Delivery channel — "email", "sms", "push", or "inapp".
            payload: Email content as dict (``{"subject": "...", "html": "..."}``) or JSON string.
            idempotency_key: Optional key to prevent duplicate sends.

        Returns:
            ``{"publicId": "..."}``
        """
        if isinstance(payload, dict):
            payload = json.dumps(payload)

        headers = {}
        if idempotency_key:
            headers["Idempotency-Key"] = idempotency_key

        return self._request("POST", "/api/v1/notifications", {
            "recipientEmail": recipient_email,
            "type": type,
            "channel": channel,
            "payload": payload,
        }, headers=headers)

    def get_notification(self, public_id: str) -> dict:
        """Get notification detail by public ID."""
        return self._request("GET", f"/api/v1/notifications/{public_id}")

    def list_notifications(
        self,
        page: int = 1,
        page_size: int = 20,
        date_from: str | None = None,
        date_to: str | None = None,
        status: str | None = None,
    ) -> dict:
        """List notifications with optional filtering.

        Returns:
            ``{"items": [...], "totalCount": N, "pageNumber": N, "pageSize": N}``
        """
        params = []
        if page != 1:
            params.append(f"page={page}")
        if page_size != 20:
            params.append(f"pageSize={page_size}")
        if date_from:
            params.append(f"dateFrom={date_from}")
        if date_to:
            params.append(f"dateTo={date_to}")
        if status:
            params.append(f"status={status}")

        qs = "&".join(params)
        path = f"/api/v1/notifications{'?' + qs if qs else ''}"
        return self._request("GET", path)

    def retry_notification(self, public_id: str) -> dict:
        """Retry a failed notification."""
        return self._request("POST", f"/api/v1/notifications/{public_id}/retry")

    # ── Templates ──────────────────────────────────────────────

    def create_template(self, name: str, subject: str, body: str) -> dict:
        """Create a reusable email template.

        Supports ``{{variable}}`` placeholders in subject and body.

        Returns:
            ``{"id": "...", "name": "..."}``
        """
        return self._request("POST", "/api/v1/templates", {
            "name": name, "subject": subject, "body": body,
        })

    def get_template(self, template_id: str) -> dict:
        """Get a template by ID."""
        return self._request("GET", f"/api/v1/templates/{template_id}")

    def list_templates(self, page: int = 1, page_size: int = 20) -> dict:
        """List all templates."""
        return self._request("GET", f"/api/v1/templates?page={page}&pageSize={page_size}")

    def update_template(self, template_id: str, name: str, subject: str, body: str) -> dict:
        """Update an existing template."""
        return self._request("PUT", f"/api/v1/templates/{template_id}", {
            "name": name, "subject": subject, "body": body,
        })

    def delete_template(self, template_id: str) -> dict:
        """Delete a template."""
        return self._request("DELETE", f"/api/v1/templates/{template_id}")

    # ── Campaigns ──────────────────────────────────────────────

    def create_campaign(
        self,
        title: str,
        subject: str,
        body: str | None = None,
        template_id: str | None = None,
        channel: str = "email",
        scheduled_at: str | None = None,
    ) -> dict:
        """Create a bulk email campaign.

        Returns:
            ``{"id": "...", "title": "..."}``
        """
        payload: dict[str, Any] = {"title": title, "subject": subject, "channel": channel}
        if body:
            payload["body"] = body
        if template_id:
            payload["templateId"] = template_id
        if scheduled_at:
            payload["scheduledAt"] = scheduled_at
        return self._request("POST", "/api/v1/campaigns", payload)

    def get_campaign(self, campaign_id: str) -> dict:
        """Get campaign detail."""
        return self._request("GET", f"/api/v1/campaigns/{campaign_id}")

    def list_campaigns(self, page: int = 1, page_size: int = 20) -> dict:
        """List all campaigns."""
        return self._request("GET", f"/api/v1/campaigns?page={page}&pageSize={page_size}")

    def get_campaign_progress(self, campaign_id: str) -> dict:
        """Get real-time campaign send progress."""
        return self._request("GET", f"/api/v1/campaigns/{campaign_id}/progress")

    def add_campaign_recipients(self, campaign_id: str, emails: list[str]) -> dict:
        """Add email recipients to a campaign.

        Returns:
            ``{"added": N, "skipped": N}``
        """
        return self._request("POST", f"/api/v1/campaigns/{campaign_id}/recipients", {"emails": emails})

    def send_campaign(self, campaign_id: str) -> dict:
        """Send a campaign immediately."""
        return self._request("POST", f"/api/v1/campaigns/{campaign_id}/send")

    def schedule_campaign(self, campaign_id: str, scheduled_at: str) -> dict:
        """Schedule a campaign for future sending."""
        return self._request("POST", f"/api/v1/campaigns/{campaign_id}/schedule", {"scheduledAt": scheduled_at})

    def pause_campaign(self, campaign_id: str) -> dict:
        """Pause a running campaign."""
        return self._request("POST", f"/api/v1/campaigns/{campaign_id}/pause")

    def resume_campaign(self, campaign_id: str) -> dict:
        """Resume a paused campaign."""
        return self._request("POST", f"/api/v1/campaigns/{campaign_id}/resume")

    def delete_campaign(self, campaign_id: str) -> dict:
        """Delete a campaign."""
        return self._request("DELETE", f"/api/v1/campaigns/{campaign_id}")
