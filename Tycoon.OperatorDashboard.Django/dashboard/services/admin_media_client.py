from typing import Any
from urllib.parse import urljoin

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


def get_storage_diagnostics(access_token: str) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/media/storage-diagnostics"
    response = httpx.get(
        url,
        headers=_headers(access_token),
        timeout=settings.API_REQUEST_TIMEOUT_SECONDS,
    )
    response.raise_for_status()
    return response.json()


def upload_file_to_intent(
    access_token: str,
    upload_url: str,
    uploaded_file,
    content_type: str,
) -> dict[str, Any]:
    uploaded_file.seek(0)
    if upload_url.startswith("http://") or upload_url.startswith("https://"):
        response = httpx.put(
            upload_url,
            content=uploaded_file.chunks(),
            headers={"Content-Type": content_type},
            timeout=settings.API_REQUEST_TIMEOUT_SECONDS,
        )
    else:
        url = urljoin(f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/", upload_url.lstrip("/"))
        response = httpx.post(
            url,
            files={"file": (uploaded_file.name, uploaded_file, content_type)},
            headers=_headers(access_token),
            timeout=settings.API_REQUEST_TIMEOUT_SECONDS,
        )
    response.raise_for_status()
    try:
        payload = response.json()
    except ValueError:
        payload = {}
    return {
        "statusCode": response.status_code,
        "ok": response.is_success,
        "payload": payload,
    }
