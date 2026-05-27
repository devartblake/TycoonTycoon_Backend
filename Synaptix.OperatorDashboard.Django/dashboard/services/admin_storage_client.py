from __future__ import annotations

from typing import Any, BinaryIO

import httpx
from django.conf import settings

from .admin_auth_client import _headers


def _multipart_headers(access_token: str | None = None) -> dict[str, str]:
    headers = _headers(access_token)
    headers.pop("Content-Type", None)
    return headers


def list_storage_prefixes(access_token: str | None) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/storage/prefixes"
    response = httpx.get(url, headers=_headers(access_token), timeout=settings.API_REQUEST_TIMEOUT_SECONDS)
    response.raise_for_status()
    return response.json()


def list_storage_objects(access_token: str | None, query: dict[str, Any]) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/storage/objects"
    response = httpx.get(url, params=query, headers=_headers(access_token), timeout=settings.API_REQUEST_TIMEOUT_SECONDS)
    response.raise_for_status()
    return response.json()


def get_storage_object_metadata(access_token: str | None, key: str) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/storage/objects/metadata"
    response = httpx.get(url, params={"key": key}, headers=_headers(access_token), timeout=settings.API_REQUEST_TIMEOUT_SECONDS)
    response.raise_for_status()
    return response.json()


def upload_storage_object_proxy(
    access_token: str | None,
    *,
    key: str,
    file_name: str,
    content_type: str,
    file_obj: BinaryIO,
    overwrite: bool = False,
) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/storage/upload-proxy"
    files = {"file": (file_name, file_obj, content_type)}
    data = {"key": key, "overwrite": "true" if overwrite else "false"}
    response = httpx.post(
        url,
        data=data,
        files=files,
        headers=_multipart_headers(access_token),
        timeout=max(settings.API_REQUEST_TIMEOUT_SECONDS, 120),
    )
    response.raise_for_status()
    return response.json()
