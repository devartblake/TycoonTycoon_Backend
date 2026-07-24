from __future__ import annotations

from typing import Any

import httpx
from django.conf import settings

from .admin_auth_client import _headers

VALID_SETUP_SECTIONS = {"status", "readiness", "services", "seeds", "validation", "history"}


def get_setup_diagnostics(access_token: str | None, section: str) -> dict[str, Any]:
    if section not in VALID_SETUP_SECTIONS:
        raise ValueError(f"Unsupported setup diagnostics section: {section}")

    path = "history?limit=20" if section == "history" else section
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/setup/{path}"
    response = httpx.get(url, headers=_headers(access_token), timeout=settings.API_REQUEST_TIMEOUT_SECONDS)
    response.raise_for_status()
    return response.json()
