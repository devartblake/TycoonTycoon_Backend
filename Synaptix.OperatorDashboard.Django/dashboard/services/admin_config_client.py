from typing import Any

import httpx
from django.conf import settings

from .admin_auth_client import _headers


def get_admin_config(access_token: str) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/config"
    r = httpx.get(url, headers=_headers(access_token), timeout=settings.API_REQUEST_TIMEOUT_SECONDS)
    r.raise_for_status()
    return r.json()


def update_feature_flags(access_token: str, feature_flags: dict[str, bool]) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/config"
    r = httpx.patch(
        url,
        json={"featureFlags": feature_flags},
        headers=_headers(access_token),
        timeout=settings.API_REQUEST_TIMEOUT_SECONDS,
    )
    r.raise_for_status()
    return r.json()
