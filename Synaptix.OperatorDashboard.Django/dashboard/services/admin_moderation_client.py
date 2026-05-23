from typing import Any

import httpx
from django.conf import settings

from .admin_auth_client import _headers


def get_moderation_profile(access_token: str, player_id: str) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/moderation/profile/{player_id}"
    response = httpx.get(
        url,
        headers=_headers(access_token),
        timeout=settings.API_REQUEST_TIMEOUT_SECONDS,
    )
    response.raise_for_status()
    return response.json()


def get_moderation_logs(access_token: str, query: dict[str, Any]) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/moderation/logs"
    response = httpx.get(
        url,
        params=query,
        headers=_headers(access_token),
        timeout=settings.API_REQUEST_TIMEOUT_SECONDS,
    )
    response.raise_for_status()
    return response.json()


def get_moderation_log(access_token: str, log_id: str) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/moderation/logs/{log_id}"
    response = httpx.get(
        url,
        headers=_headers(access_token),
        timeout=settings.API_REQUEST_TIMEOUT_SECONDS,
    )
    response.raise_for_status()
    return response.json()


def set_moderation_status(
    access_token: str,
    admin_user: str | None,
    payload: dict[str, Any],
) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/moderation/set-status"
    headers = _headers(access_token)
    if admin_user:
        headers["X-Admin-User"] = admin_user

    response = httpx.post(
        url,
        json=payload,
        headers=headers,
        timeout=settings.API_REQUEST_TIMEOUT_SECONDS,
    )
    response.raise_for_status()
    return response.json()
