"""NotificationHub Python SDK — send emails, manage templates, and run campaigns."""

from .client import NotificationHub
from .exceptions import NotificationHubError, AuthenticationError, NotFoundError, ConflictError

__version__ = "1.0.0"
__all__ = ["NotificationHub", "NotificationHubError", "AuthenticationError", "NotFoundError", "ConflictError"]
