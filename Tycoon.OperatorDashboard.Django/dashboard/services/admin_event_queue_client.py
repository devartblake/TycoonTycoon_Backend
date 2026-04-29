from typing import Any

import httpx
from django.conf import settings

from .admin_auth_client import _headers


def reprocess_event_queue(access_token: str, scope: str, limit: int = 1000) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/event-queue/reprocess"
    response = httpx.post(
        url,
        json={"scope": scope, "limit": limit},
        headers=_headers(access_token),
        timeout=settings.API_REQUEST_TIMEOUT_SECONDS,
    )
    response.raise_for_status()
    return response.json()
