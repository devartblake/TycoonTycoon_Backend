from typing import Any

import httpx
from django.conf import settings

from .admin_auth_client import _headers


def list_seasons(access_token: str, params: dict | None = None) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/seasons"
    response = httpx.get(
        url,
        params={k: v for k, v in (params or {}).items() if v not in (None, "")},
        headers=_headers(access_token),
        timeout=settings.API_REQUEST_TIMEOUT_SECONDS,
    )
    response.raise_for_status()
    return response.json()


def activate_season(access_token: str, season_id: str) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/seasons/activate"
    response = httpx.post(
        url,
        json={"seasonId": season_id},
        headers=_headers(access_token),
        timeout=settings.API_REQUEST_TIMEOUT_SECONDS,
    )
    response.raise_for_status()
    return response.json()


def close_season(access_token: str, season_id: str) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/seasons/close"
    response = httpx.post(
        url,
        json={"seasonId": season_id},
        headers=_headers(access_token),
        timeout=settings.API_REQUEST_TIMEOUT_SECONDS,
    )
    response.raise_for_status()
    return response.json()


def recompute_season_tiers(access_token: str, season_id: str) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/seasons/{season_id}/recompute-tiers"
    response = httpx.post(url, headers=_headers(access_token), timeout=settings.API_REQUEST_TIMEOUT_SECONDS)
    response.raise_for_status()
    return response.json()


def get_season_leaderboard(access_token: str, season_id: str, params: dict | None = None) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/seasons/{season_id}/leaderboard"
    response = httpx.get(
        url,
        params={k: v for k, v in (params or {}).items() if v not in (None, "")},
        headers=_headers(access_token),
        timeout=settings.API_REQUEST_TIMEOUT_SECONDS,
    )
    response.raise_for_status()
    return response.json()


def get_reward_claims(access_token: str, params: dict | None = None) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/seasons/rewards/claims"
    response = httpx.get(
        url,
        params={k: v for k, v in (params or {}).items() if v not in (None, "")},
        headers=_headers(access_token),
        timeout=settings.API_REQUEST_TIMEOUT_SECONDS,
    )
    response.raise_for_status()
    return response.json()
