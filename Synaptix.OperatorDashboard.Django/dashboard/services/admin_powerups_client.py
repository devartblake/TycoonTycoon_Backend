from typing import Any

import httpx
from django.conf import settings

from .admin_auth_client import _headers


def get_powerup_state(access_token: str, player_id: str) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/powerups/state/{player_id}"
    response = httpx.get(url, headers=_headers(access_token), timeout=settings.API_REQUEST_TIMEOUT_SECONDS)
    response.raise_for_status()
    return response.json()


def grant_powerup(access_token: str, player_id: str, powerup_key: str, quantity: int) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/powerups/grant"
    response = httpx.post(
        url,
        json={"playerId": player_id, "powerupKey": powerup_key, "quantity": quantity},
        headers=_headers(access_token),
        timeout=settings.API_REQUEST_TIMEOUT_SECONDS,
    )
    response.raise_for_status()
    return response.json()
