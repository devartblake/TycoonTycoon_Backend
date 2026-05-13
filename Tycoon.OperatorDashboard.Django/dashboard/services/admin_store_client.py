from typing import Any

import httpx
from django.conf import settings

from .admin_auth_client import _headers


def get_stock_policies(access_token: str, params: dict | None = None) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/store/stock-policies"
    response = httpx.get(
        url,
        params={k: v for k, v in (params or {}).items() if v not in (None, "")},
        headers=_headers(access_token),
        timeout=settings.API_REQUEST_TIMEOUT_SECONDS,
    )
    response.raise_for_status()
    return response.json()


def bulk_reset_stock(access_token: str, skus: list[str], reason: str | None = None) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/store/stock-policies/bulk-reset"
    response = httpx.post(
        url,
        json={"skus": skus, "reason": reason or None},
        headers=_headers(access_token),
        timeout=settings.API_REQUEST_TIMEOUT_SECONDS,
    )
    response.raise_for_status()
    return response.json()


def get_player_stock(access_token: str, player_id: str) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/store/player-stock/{player_id}"
    response = httpx.get(
        url,
        headers=_headers(access_token),
        timeout=settings.API_REQUEST_TIMEOUT_SECONDS,
    )
    response.raise_for_status()
    return response.json()


def override_player_stock(
    access_token: str,
    player_id: str,
    sku: str,
    effective_max_quantity: int | None,
    reason: str | None = None,
) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/store/player-stock/{player_id}/override"
    response = httpx.post(
        url,
        json={
            "sku": sku,
            "effectiveMaxQuantity": effective_max_quantity,
            "reason": reason or None,
        },
        headers=_headers(access_token),
        timeout=settings.API_REQUEST_TIMEOUT_SECONDS,
    )
    response.raise_for_status()
    return response.json()


def get_flash_sales(access_token: str) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/store/flash-sales"
    response = httpx.get(
        url,
        headers=_headers(access_token),
        timeout=settings.API_REQUEST_TIMEOUT_SECONDS,
    )
    response.raise_for_status()
    return response.json()


def cancel_flash_sale(access_token: str, sale_id: str) -> None:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/store/flash-sales/{sale_id}"
    response = httpx.delete(
        url,
        headers=_headers(access_token),
        timeout=settings.API_REQUEST_TIMEOUT_SECONDS,
    )
    response.raise_for_status()


def get_purchase_analytics(access_token: str, params: dict | None = None) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/store/analytics/purchases"
    response = httpx.get(
        url,
        params={k: v for k, v in (params or {}).items() if v not in (None, "")},
        headers=_headers(access_token),
        timeout=settings.API_REQUEST_TIMEOUT_SECONDS,
    )
    response.raise_for_status()
    return response.json()
