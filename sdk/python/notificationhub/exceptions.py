"""SDK exceptions."""


class NotificationHubError(Exception):
    """Base exception for NotificationHub API errors."""

    def __init__(self, message: str, status_code: int, body: dict | None = None):
        self.message = message
        self.status_code = status_code
        self.body = body
        super().__init__(self.message)


class AuthenticationError(NotificationHubError):
    """Raised when API key is invalid or missing (401)."""
    pass


class NotFoundError(NotificationHubError):
    """Raised when a resource is not found (404)."""
    pass


class ConflictError(NotificationHubError):
    """Raised on conflict, e.g. duplicate idempotency key (409)."""
    pass
