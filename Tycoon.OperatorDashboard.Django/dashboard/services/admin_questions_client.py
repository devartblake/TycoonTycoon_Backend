from typing import Any

import httpx
from django.conf import settings

from .admin_auth_client import _headers


def list_questions(access_token: str, params: dict | None = None) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/questions"
    response = httpx.get(
        url,
        params={k: v for k, v in (params or {}).items() if v not in (None, "")},
        headers=_headers(access_token),
        timeout=settings.API_REQUEST_TIMEOUT_SECONDS,
    )
    response.raise_for_status()
    return response.json()


def approve_question(access_token: str, question_id: str) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/questions/{question_id}/approve"
    response = httpx.post(url, headers=_headers(access_token), timeout=settings.API_REQUEST_TIMEOUT_SECONDS)
    response.raise_for_status()
    return response.json()


def reject_question(access_token: str, question_id: str) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/questions/{question_id}/reject"
    response = httpx.post(url, headers=_headers(access_token), timeout=settings.API_REQUEST_TIMEOUT_SECONDS)
    response.raise_for_status()
    return response.json()
