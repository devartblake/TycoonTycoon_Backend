from dataclasses import asdict, dataclass
from typing import Any

import httpx
from django.conf import settings


@dataclass
class ServiceStatus:
    service_name: str
    base_url: str
    status: str
    detail: str
    payload: dict[str, Any] | None = None
    css_class: str = "status-unknown"

    def to_dict(self) -> dict[str, Any]:
        return asdict(self)


def _fetch_json(url: str) -> tuple[str, str, dict[str, Any] | None]:
    try:
        response = httpx.get(url, timeout=settings.API_REQUEST_TIMEOUT_SECONDS)
        response.raise_for_status()
        data = response.json() if response.text else {}
        return "healthy", "Request succeeded", data
    except httpx.HTTPStatusError as ex:
        return "degraded", f"HTTP {ex.response.status_code}", None
    except httpx.RequestError as ex:
        return "offline", str(ex), None
    except ValueError:
        return "degraded", "Non-JSON response", None


def _fetch_text_health(url: str) -> tuple[str, str, dict[str, Any] | None]:
    try:
        response = httpx.get(url, timeout=settings.API_REQUEST_TIMEOUT_SECONDS)
        response.raise_for_status()
        body = response.text.strip()
        return "healthy", "Request succeeded", {"response": body or "ok"}
    except httpx.HTTPStatusError as ex:
        return "degraded", f"HTTP {ex.response.status_code}", None
    except httpx.RequestError as ex:
        return "offline", str(ex), None


def get_dotnet_status() -> ServiceStatus:
    base_url = settings.DOTNET_API_BASE_URL.rstrip("/")
    status, detail, payload = _fetch_json(f"{base_url}/health")
    return ServiceStatus(
        service_name=".NET API",
        base_url=base_url,
        status=status,
        detail=detail,
        payload=payload,
    )


def get_fastapi_status() -> ServiceStatus:
    base_url = settings.FASTAPI_BASE_URL.rstrip("/")
    status, detail, payload = _fetch_json(f"{base_url}/health")
    return ServiceStatus(
        service_name="FastAPI Sidecar",
        base_url=base_url,
        status=status,
        detail=detail,
        payload=payload,
    )


def get_minio_status() -> ServiceStatus:
    base_url = settings.MINIO_BASE_URL.rstrip("/")
    status, detail, payload = _fetch_text_health(f"{base_url}/minio/health/live")
    return ServiceStatus(
        service_name="MinIO",
        base_url=base_url,
        status=status,
        detail=detail,
        payload=payload,
    )


def list_service_statuses() -> list[ServiceStatus]:
    return [get_dotnet_status(), get_fastapi_status(), get_minio_status()]


def get_overall_status(services: list[ServiceStatus]) -> str:
    if any(service.status == "offline" for service in services):
        return "offline"

    if any(service.status == "degraded" for service in services):
        return "degraded"

    return "healthy"
