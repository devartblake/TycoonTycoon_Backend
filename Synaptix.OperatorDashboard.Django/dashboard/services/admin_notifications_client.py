from typing import Any

import httpx
from django.conf import settings

from .admin_auth_client import _headers


def list_channels(access_token: str) -> list[dict[str, Any]]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/notifications/channels"
    response = httpx.get(url, headers=_headers(access_token), timeout=settings.API_REQUEST_TIMEOUT_SECONDS)
    response.raise_for_status()
    return response.json()


def get_notification_history(access_token: str, params: dict | None = None) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/notifications/history"
    response = httpx.get(
        url,
        params={k: v for k, v in (params or {}).items() if v not in (None, "")},
        headers=_headers(access_token),
        timeout=settings.API_REQUEST_TIMEOUT_SECONDS,
    )
    response.raise_for_status()
    return response.json()


def send_notification(access_token: str, payload: dict) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/notifications/send"
    response = httpx.post(
        url,
        json=payload,
        headers=_headers(access_token),
        timeout=settings.API_REQUEST_TIMEOUT_SECONDS,
    )
    response.raise_for_status()
    return response.json()


def upsert_channel(access_token: str, key: str, payload: dict[str, Any]) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/notifications/channels/{key}"
    response = httpx.put(
        url,
        json=payload,
        headers=_headers(access_token),
        timeout=settings.API_REQUEST_TIMEOUT_SECONDS,
    )
    response.raise_for_status()
    return response.json()


def schedule_notification(access_token: str, payload: dict[str, Any]) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/notifications/schedule"
    response = httpx.post(
        url,
        json=payload,
        headers=_headers(access_token),
        timeout=settings.API_REQUEST_TIMEOUT_SECONDS,
    )
    response.raise_for_status()
    return response.json()


def list_scheduled(access_token: str, params: dict | None = None) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/notifications/scheduled"
    response = httpx.get(
        url,
        params={k: v for k, v in (params or {}).items() if v not in (None, "")},
        headers=_headers(access_token),
        timeout=settings.API_REQUEST_TIMEOUT_SECONDS,
    )
    response.raise_for_status()
    return response.json()


def cancel_scheduled(access_token: str, schedule_id: str) -> None:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/notifications/scheduled/{schedule_id}"
    response = httpx.delete(url, headers=_headers(access_token), timeout=settings.API_REQUEST_TIMEOUT_SECONDS)
    response.raise_for_status()


def list_templates(access_token: str) -> list[dict[str, Any]]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/notifications/templates"
    response = httpx.get(url, headers=_headers(access_token), timeout=settings.API_REQUEST_TIMEOUT_SECONDS)
    response.raise_for_status()
    return response.json()


def create_template(access_token: str, payload: dict[str, Any]) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/notifications/templates"
    response = httpx.post(
        url,
        json=payload,
        headers=_headers(access_token),
        timeout=settings.API_REQUEST_TIMEOUT_SECONDS,
    )
    response.raise_for_status()
    return response.json()


def update_template(access_token: str, template_id: str, payload: dict[str, Any]) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/notifications/templates/{template_id}"
    response = httpx.patch(
        url,
        json=payload,
        headers=_headers(access_token),
        timeout=settings.API_REQUEST_TIMEOUT_SECONDS,
    )
    response.raise_for_status()
    return response.json()


def delete_template(access_token: str, template_id: str) -> None:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/notifications/templates/{template_id}"
    response = httpx.delete(url, headers=_headers(access_token), timeout=settings.API_REQUEST_TIMEOUT_SECONDS)
    response.raise_for_status()


def get_dead_letter(access_token: str, params: dict | None = None) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/notifications/dead-letter"
    response = httpx.get(
        url,
        params={k: v for k, v in (params or {}).items() if v not in (None, "")},
        headers=_headers(access_token),
        timeout=settings.API_REQUEST_TIMEOUT_SECONDS,
    )
    response.raise_for_status()
    return response.json()


def replay_dead_letter(access_token: str, schedule_id: str) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/notifications/dead-letter/{schedule_id}/replay"
    response = httpx.post(url, headers=_headers(access_token), timeout=settings.API_REQUEST_TIMEOUT_SECONDS)
    response.raise_for_status()
    return response.json()
