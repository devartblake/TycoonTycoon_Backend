from typing import Any

import httpx
from django.conf import settings

from .admin_auth_client import _headers


def get_season_points_history(access_token: str, player_id: str, params: dict) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/season-points/history/{player_id}"
    r = httpx.get(
        url,
        params={k: v for k, v in params.items() if v is not None},
        headers=_headers(access_token),
        timeout=settings.API_REQUEST_TIMEOUT_SECONDS,
    )
    r.raise_for_status()
    return r.json()


def apply_season_points(access_token: str, player_id: str, delta: int, reason: str) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/season-points/transactions"
    r = httpx.post(
        url,
        json={"playerId": player_id, "delta": delta, "reason": reason},
        headers=_headers(access_token),
        timeout=settings.API_REQUEST_TIMEOUT_SECONDS,
    )
    r.raise_for_status()
    return r.json()
