"""API-key authentication. Key comes from the environment, never from code."""
import hmac
import os

from fastapi import Header

from .errors import ApiError


def require_api_key(x_api_key: str | None = Header(default=None)) -> None:
    expected = os.environ.get("CLIENT_API_KEY", "")
    if not expected:
        raise ApiError(500, "server_misconfigured", "Server API key is not configured")
    if not x_api_key or not hmac.compare_digest(x_api_key, expected):
        raise ApiError(401, "unauthorized", "Missing or invalid API key")
