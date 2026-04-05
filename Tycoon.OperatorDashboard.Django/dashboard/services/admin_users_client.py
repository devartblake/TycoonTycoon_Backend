from typing import Any

import httpx
from django.conf import settings

from .admin_auth_client import _headers


def list_admin_users(access_token: str, query: dict[str, Any]) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/users"
    response = httpx.get(
        url,
        params=query,
        headers=_headers(access_token),
        timeout=settings.API_REQUEST_TIMEOUT_SECONDS,
    )
    response.raise_for_status()
    return response.json()
