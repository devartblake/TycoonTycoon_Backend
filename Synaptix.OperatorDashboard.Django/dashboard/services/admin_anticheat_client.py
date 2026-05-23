from typing import Any

import httpx
from django.conf import settings

from .admin_auth_client import _headers


def list_anticheat_flags(access_token: str, params: dict | None = None) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/anti-cheat/flags"
    response = httpx.get(
        url,
        params={k: v for k, v in (params or {}).items() if v not in (None, "")},
        headers=_headers(access_token),
        timeout=settings.API_REQUEST_TIMEOUT_SECONDS,
    )
    response.raise_for_status()
    return response.json()


def review_anticheat_flag(access_token: str, flag_id: str, reviewed_by: str, note: str) -> None:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/anti-cheat/flags/{flag_id}/review"
    response = httpx.put(
        url,
        json={"reviewedBy": reviewed_by, "note": note},
        headers=_headers(access_token),
        timeout=settings.API_REQUEST_TIMEOUT_SECONDS,
    )
    response.raise_for_status()


def get_anticheat_analytics_summary(access_token: str, window_hours: int = 24) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/anti-cheat/analytics/summary"
    response = httpx.get(
        url,
        params={"windowHours": window_hours},
        headers=_headers(access_token),
        timeout=settings.API_REQUEST_TIMEOUT_SECONDS,
    )
    response.raise_for_status()
    return response.json()


def list_party_anticheat_flags(access_token: str, params: dict | None = None) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/anti-cheat/party/flags"
    response = httpx.get(
        url,
        params={k: v for k, v in (params or {}).items() if v not in (None, "")},
        headers=_headers(access_token),
        timeout=settings.API_REQUEST_TIMEOUT_SECONDS,
    )
    response.raise_for_status()
    return response.json()


def review_party_anticheat_flag(access_token: str, flag_id: str, reviewed_by: str, note: str) -> None:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/anti-cheat/party/flags/{flag_id}/review"
    response = httpx.put(
        url,
        json={"reviewedBy": reviewed_by, "note": note},
        headers=_headers(access_token),
        timeout=settings.API_REQUEST_TIMEOUT_SECONDS,
    )
    response.raise_for_status()
