from typing import Any

import httpx
from django.conf import settings

from .admin_auth_client import _headers


def seed_skills(access_token: str, skills: list[dict[str, Any]]) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/skills/seed"
    response = httpx.post(
        url,
        json=skills,
        headers=_headers(access_token),
        timeout=settings.API_REQUEST_TIMEOUT_SECONDS,
    )
    response.raise_for_status()
    return response.json()
