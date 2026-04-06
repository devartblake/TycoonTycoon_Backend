from typing import Any

import httpx
from django.conf import settings

from .admin_auth_client import _headers


def create_upload_intent(
    access_token: str,
    file_name: str,
    content_type: str,
    size_bytes: int,
) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/media/intent"
    response = httpx.post(
        url,
        json={"fileName": file_name, "contentType": content_type, "sizeBytes": size_bytes},
        headers=_headers(access_token),
        timeout=settings.API_REQUEST_TIMEOUT_SECONDS,
    )
    response.raise_for_status()
    return response.json()
