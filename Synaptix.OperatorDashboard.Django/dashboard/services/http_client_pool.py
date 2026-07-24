"""HTTP client pooling for connection reuse and KMS session caching."""

import time
from typing import Any

import httpx
from django.conf import settings

# Global HTTP client with connection pooling
_http_client: httpx.Client | None = None
_kms_session_cache: dict[str, Any] = {}
_kms_session_ttl = 300  # 5 minutes


def get_http_client() -> httpx.Client:
    """Get or create a persistent HTTP client with connection pooling.

    This reuses TCP connections across multiple requests, significantly
    reducing overhead for repeated calls to the same host.
    """
    global _http_client
    if _http_client is None:
        _http_client = httpx.Client(
            timeout=getattr(settings, "API_REQUEST_TIMEOUT_SECONDS", 10),
            limits=httpx.Limits(max_connections=20, max_keepalive_connections=10),
            http2=False,  # Stick with HTTP/1.1 for broader compatibility
        )
    return _http_client


def close_http_client() -> None:
    """Close the global HTTP client."""
    global _http_client
    if _http_client is not None:
        _http_client.close()
        _http_client = None


def get_kms_session() -> dict[str, Any] | None:
    """Get cached KMS session if still valid.

    Returns:
        Dict with sessionId and created_at if valid, None if expired or not cached.
    """
    cached = _kms_session_cache.get("default")
    if cached is None:
        return None

    # Check if session has expired
    age = time.time() - cached.get("created_at", 0)
    if age > _kms_session_ttl:
        _kms_session_cache.pop("default", None)
        return None

    return cached


def cache_kms_session(session_id: str) -> None:
    """Cache a KMS session ID with timestamp.

    Args:
        session_id: The session ID returned by KMS start_session endpoint.
    """
    _kms_session_cache["default"] = {
        "sessionId": session_id,
        "created_at": time.time(),
    }


def clear_kms_session() -> None:
    """Clear the cached KMS session."""
    _kms_session_cache.pop("default", None)
