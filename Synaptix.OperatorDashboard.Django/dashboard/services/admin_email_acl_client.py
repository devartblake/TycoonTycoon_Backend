from typing import Any

import httpx
from django.conf import settings

from .admin_auth_client import _headers


def list_admin_email_acl(access_token: str, query: dict[str, Any]) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/email-acl/"
    response = httpx.get(url, params=query, headers=_headers(access_token), timeout=settings.API_REQUEST_TIMEOUT_SECONDS)
    response.raise_for_status()
    return response.json()


def create_admin_email_acl(access_token: str, payload: dict[str, Any]) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/email-acl/"
    response = httpx.post(url, json=payload, headers=_headers(access_token), timeout=settings.API_REQUEST_TIMEOUT_SECONDS)
    response.raise_for_status()
    return response.json()


def update_admin_email_acl(access_token: str, entry_id: str, payload: dict[str, Any]) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/email-acl/{entry_id}"
    response = httpx.patch(url, json=payload, headers=_headers(access_token), timeout=settings.API_REQUEST_TIMEOUT_SECONDS)
    response.raise_for_status()
    return response.json()


def delete_admin_email_acl(access_token: str, entry_id: str) -> None:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/email-acl/{entry_id}"
    response = httpx.delete(url, headers=_headers(access_token), timeout=settings.API_REQUEST_TIMEOUT_SECONDS)
    response.raise_for_status()
