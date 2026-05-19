from typing import Any

import httpx
from django.conf import settings

from .admin_auth_client import _headers


def get_matches(access_token: str, params: dict | None = None) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/matches"
    response = httpx.get(
        url,
        params={k: v for k, v in (params or {}).items() if v not in (None, "")},
        headers=_headers(access_token),
        timeout=settings.API_REQUEST_TIMEOUT_SECONDS,
    )
    response.raise_for_status()
    return response.json()
