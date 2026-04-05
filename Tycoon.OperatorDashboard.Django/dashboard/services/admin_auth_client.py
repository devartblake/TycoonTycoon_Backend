from dataclasses import dataclass
from typing import Any

import httpx
from django.conf import settings


@dataclass
class AdminLoginResult:
    access_token: str
    refresh_token: str
    expires_in: int
    admin: dict[str, Any]


@dataclass
class AdminRefreshResult:
    access_token: str
    expires_in: int


def _headers(access_token: str | None = None) -> dict[str, str]:
    headers: dict[str, str] = {"Content-Type": "application/json"}

    if settings.ADMIN_OPS_KEY:
        headers["X-Admin-Ops-Key"] = settings.ADMIN_OPS_KEY

    if access_token:
        headers["Authorization"] = f"Bearer {access_token}"

    return headers


def admin_login(email: str, password: str) -> AdminLoginResult:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/auth/login"
    response = httpx.post(
        url,
        json={"email": email, "password": password},
        headers=_headers(),
        timeout=settings.API_REQUEST_TIMEOUT_SECONDS,
    )
    response.raise_for_status()
    payload = response.json()

    return AdminLoginResult(
        access_token=payload["accessToken"],
        refresh_token=payload["refreshToken"],
        expires_in=payload["expiresIn"],
        admin=payload["admin"],
    )


def admin_refresh(refresh_token: str) -> AdminRefreshResult:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/auth/refresh"
    response = httpx.post(
        url,
        json={"refreshToken": refresh_token},
        headers=_headers(),
        timeout=settings.API_REQUEST_TIMEOUT_SECONDS,
    )
    response.raise_for_status()
    payload = response.json()

    return AdminRefreshResult(
        access_token=payload["accessToken"],
        expires_in=payload["expiresIn"],
    )


def admin_me(access_token: str) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/auth/me"
    response = httpx.get(
        url,
        headers=_headers(access_token),
        timeout=settings.API_REQUEST_TIMEOUT_SECONDS,
    )
    response.raise_for_status()
    return response.json()
