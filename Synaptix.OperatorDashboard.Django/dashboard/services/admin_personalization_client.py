from typing import Any

import httpx
from django.conf import settings

from .admin_auth_client import _headers


def get_personalization_summary(access_token: str) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/personalization/summary"
    response = httpx.get(url, headers=_headers(access_token), timeout=settings.API_REQUEST_TIMEOUT_SECONDS)
    response.raise_for_status()
    return response.json()


def get_personalization_archetypes(access_token: str) -> list[dict[str, Any]]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/personalization/archetypes"
    response = httpx.get(url, headers=_headers(access_token), timeout=settings.API_REQUEST_TIMEOUT_SECONDS)
    response.raise_for_status()
    return response.json()


def get_recommendation_performance(access_token: str) -> list[dict[str, Any]]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/personalization/recommendations/performance"
    response = httpx.get(url, headers=_headers(access_token), timeout=settings.API_REQUEST_TIMEOUT_SECONDS)
    response.raise_for_status()
    return response.json()


def get_player_profile(access_token: str, player_id: str) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/personalization/player/{player_id}"
    response = httpx.get(url, headers=_headers(access_token), timeout=settings.API_REQUEST_TIMEOUT_SECONDS)
    response.raise_for_status()
    return response.json()


def get_player_debug(access_token: str, player_id: str) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/personalization/debug/{player_id}"
    response = httpx.get(url, headers=_headers(access_token), timeout=settings.API_REQUEST_TIMEOUT_SECONDS)
    response.raise_for_status()
    return response.json()


def recalculate_player(access_token: str, player_id: str) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/personalization/player/{player_id}/recalculate"
    response = httpx.post(url, headers=_headers(access_token), timeout=settings.API_REQUEST_TIMEOUT_SECONDS)
    response.raise_for_status()
    return response.json()


def reset_player(access_token: str, player_id: str) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/personalization/player/{player_id}/reset"
    response = httpx.post(url, headers=_headers(access_token), timeout=settings.API_REQUEST_TIMEOUT_SECONDS)
    response.raise_for_status()
    return response.json()


def list_rules(access_token: str) -> list[dict[str, Any]]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/personalization/rules"
    response = httpx.get(url, headers=_headers(access_token), timeout=settings.API_REQUEST_TIMEOUT_SECONDS)
    response.raise_for_status()
    return response.json()


def upsert_rule(access_token: str, rule_key: str, payload: dict[str, Any]) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/personalization/rules/{rule_key}"
    response = httpx.put(
        url,
        json=payload,
        headers=_headers(access_token),
        timeout=settings.API_REQUEST_TIMEOUT_SECONDS,
    )
    response.raise_for_status()
    return response.json()
