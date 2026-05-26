from typing import Any

import httpx
from django.conf import settings

from .admin_auth_client import _headers


def get_player_transaction_history(access_token: str, params: dict) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/player-transactions/history"
    r = httpx.get(
        url,
        params={k: v for k, v in params.items() if v is not None},
        headers=_headers(access_token),
        timeout=settings.API_REQUEST_TIMEOUT_SECONDS,
    )
    r.raise_for_status()
    return r.json()


def dispute_player_transaction(access_token: str, transaction_id: str, reason: str) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/player-transactions/dispute"
    r = httpx.post(
        url,
        json={"transactionId": transaction_id, "reason": reason},
        headers=_headers(access_token),
        timeout=settings.API_REQUEST_TIMEOUT_SECONDS,
    )
    r.raise_for_status()
    return r.json()


def reverse_player_transaction(access_token: str, transaction_id: str, reason: str) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/player-transactions/reverse"
    r = httpx.post(
        url,
        json={"transactionId": transaction_id, "reason": reason},
        headers=_headers(access_token),
        timeout=settings.API_REQUEST_TIMEOUT_SECONDS,
    )
    r.raise_for_status()
    return r.json()
