from typing import Any

import httpx
from django.conf import settings

from .admin_auth_client import _headers


def get_mongodb_diagnostics(access_token: str | None = None) -> dict[str, Any]:
    if not access_token:
        return {
            "overallStatus": "offline",
            "configured": False,
            "warnings": ["Operator session is missing an access token."],
            "collections": [],
        }

    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/mongodb/status"
    try:
        response = httpx.get(
            url,
            headers=_headers(access_token),
            timeout=settings.API_REQUEST_TIMEOUT_SECONDS,
        )
        response.raise_for_status()
        payload = response.json() if response.text else {}
        payload.setdefault("source", "backend")
        return payload
    except httpx.HTTPStatusError as ex:
        return {
            "overallStatus": "degraded",
            "configured": True,
            "warnings": [f"Backend returned HTTP {ex.response.status_code}."],
            "collections": [],
            "source": "backend-error",
        }
    except httpx.RequestError as ex:
        return {
            "overallStatus": "offline",
            "configured": True,
            "warnings": [str(ex)],
            "collections": [],
            "source": "backend-unreachable",
        }
