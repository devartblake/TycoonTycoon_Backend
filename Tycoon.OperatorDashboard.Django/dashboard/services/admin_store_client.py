from typing import Any

import httpx
from django.conf import settings

from .admin_auth_client import _headers


def get_catalog(access_token: str, params: dict | None = None) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/store/catalog"
    response = httpx.get(
        url,
        params={k: v for k, v in (params or {}).items() if v not in (None, "")},
        headers=_headers(access_token),
        timeout=settings.API_REQUEST_TIMEOUT_SECONDS,
    )
    response.raise_for_status()
    return response.json()


def create_store_item(
    access_token: str,
    sku: str,
    name: str,
    description: str | None,
    item_type: str | None,
    price_coins: int,
    price_diamonds: int,
    grant_quantity: int,
    max_per_player: int,
    media_key: str | None,
    sort_order: int,
) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/store/catalog"
    response = httpx.post(
        url,
        json={
            "sku": sku,
            "name": name,
            "description": description,
            "itemType": item_type,
            "priceCoins": price_coins,
            "priceDiamonds": price_diamonds,
            "grantQuantity": grant_quantity,
            "maxPerPlayer": max_per_player,
            "mediaKey": media_key or None,
            "sortOrder": sort_order,
        },
        headers=_headers(access_token),
        timeout=settings.API_REQUEST_TIMEOUT_SECONDS,
    )
    response.raise_for_status()
    return response.json()


def update_store_item(access_token: str, item_id: str, **fields) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/store/catalog/{item_id}"
    body = {k: v for k, v in fields.items() if v is not None}
    response = httpx.patch(
        url,
        json=body,
        headers=_headers(access_token),
        timeout=settings.API_REQUEST_TIMEOUT_SECONDS,
    )
    response.raise_for_status()
    return response.json()


def delete_store_item(access_token: str, item_id: str) -> None:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/store/catalog/{item_id}"
    response = httpx.delete(url, headers=_headers(access_token), timeout=settings.API_REQUEST_TIMEOUT_SECONDS)
    response.raise_for_status()


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


def upsert_stock_policy(
    access_token: str,
    sku: str,
    max_quantity_per_user: int,
    reset_interval: str,
    is_active: bool | None = None,
) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/store/stock-policies/{sku.lower()}"
    body: dict[str, Any] = {"maxQuantityPerUser": max_quantity_per_user, "resetInterval": reset_interval}
    if is_active is not None:
        body["isActive"] = is_active
    response = httpx.put(
        url,
        json=body,
        headers=_headers(access_token),
        timeout=settings.API_REQUEST_TIMEOUT_SECONDS,
    )
    response.raise_for_status()
    return response.json()


def delete_stock_policy(access_token: str, sku: str) -> None:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/store/stock-policies/{sku.lower()}"
    response = httpx.delete(url, headers=_headers(access_token), timeout=settings.API_REQUEST_TIMEOUT_SECONDS)
    response.raise_for_status()


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


def get_flash_sales(access_token: str, show_all: bool = False) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/store/flash-sales"
    params = {"showAll": "true"} if show_all else {}
    response = httpx.get(
        url,
        params=params,
        headers=_headers(access_token),
        timeout=settings.API_REQUEST_TIMEOUT_SECONDS,
    )
    response.raise_for_status()
    return response.json()


def create_flash_sale(
    access_token: str,
    sku: str,
    discount_percent: int,
    starts_at_utc: str,
    ends_at_utc: str,
    reason: str | None = None,
) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/store/flash-sales"
    response = httpx.post(
        url,
        json={
            "sku": sku,
            "discountPercent": discount_percent,
            "startsAtUtc": starts_at_utc,
            "endsAtUtc": ends_at_utc,
            "reason": reason or None,
        },
        headers=_headers(access_token),
        timeout=settings.API_REQUEST_TIMEOUT_SECONDS,
    )
    response.raise_for_status()
    return response.json()


def update_flash_sale(
    access_token: str,
    sale_id: str,
    discount_percent: int,
    starts_at_utc: str,
    ends_at_utc: str,
    reason: str | None = None,
) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/store/flash-sales/{sale_id}"
    response = httpx.put(
        url,
        json={
            "discountPercent": discount_percent,
            "startsAtUtc": starts_at_utc,
            "endsAtUtc": ends_at_utc,
            "reason": reason or None,
        },
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


def get_reward_limits(access_token: str) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/store/reward-limits"
    response = httpx.get(url, headers=_headers(access_token), timeout=settings.API_REQUEST_TIMEOUT_SECONDS)
    response.raise_for_status()
    return response.json()


def get_reward_limit(access_token: str, reward_id: str) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/store/reward-limits/{reward_id}"
    response = httpx.get(url, headers=_headers(access_token), timeout=settings.API_REQUEST_TIMEOUT_SECONDS)
    response.raise_for_status()
    return response.json()


def upsert_reward_limit(
    access_token: str, reward_id: str, max_claims: int, reset_interval: str, is_active: bool | None = None
) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/store/reward-limits/{reward_id}"
    body: dict[str, Any] = {"maxClaimsPerInterval": max_claims, "resetInterval": reset_interval}
    if is_active is not None:
        body["isActive"] = is_active
    response = httpx.put(url, json=body, headers=_headers(access_token), timeout=settings.API_REQUEST_TIMEOUT_SECONDS)
    response.raise_for_status()
    return response.json()


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
