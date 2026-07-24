import time
from dataclasses import asdict, dataclass, field
from typing import Any

import httpx
from django.conf import settings

from .http_client_pool import get_http_client


@dataclass
class ServiceStatus:
    service_name: str
    base_url: str
    status: str
    detail: str
    payload: dict[str, Any] | None = None
    css_class: str = "status-unknown"
    latency_ms: int = 0
    slug: str = ""
    history: dict = field(default_factory=dict)

    def to_dict(self) -> dict[str, Any]:
        return asdict(self)


_SLUG_MAP = {
    ".NET API": "dotnet",
    "FastAPI Sidecar": "fastapi",
    "MinIO": "minio",
}


def _fetch_json(url: str) -> tuple[str, str, dict[str, Any] | None, int]:
    t0 = time.monotonic()
    try:
        client = get_http_client()
        response = client.get(url)
        latency_ms = int((time.monotonic() - t0) * 1000)
        response.raise_for_status()
        data = response.json() if response.text else {}
        return "healthy", "Request succeeded", data, latency_ms
    except httpx.HTTPStatusError as ex:
        latency_ms = int((time.monotonic() - t0) * 1000)
        return "degraded", f"HTTP {ex.response.status_code}", None, latency_ms
    except httpx.RequestError as ex:
        latency_ms = int((time.monotonic() - t0) * 1000)
        return "offline", str(ex), None, latency_ms
    except ValueError:
        latency_ms = int((time.monotonic() - t0) * 1000)
        return "degraded", "Non-JSON response", None, latency_ms


def _fetch_text_health(url: str) -> tuple[str, str, dict[str, Any] | None, int]:
    t0 = time.monotonic()
    try:
        client = get_http_client()
        response = client.get(url)
        latency_ms = int((time.monotonic() - t0) * 1000)
        response.raise_for_status()
        body = response.text.strip()
        return "healthy", "Request succeeded", {"response": body or "ok"}, latency_ms
    except httpx.HTTPStatusError as ex:
        latency_ms = int((time.monotonic() - t0) * 1000)
        return "degraded", f"HTTP {ex.response.status_code}", None, latency_ms
    except httpx.RequestError as ex:
        latency_ms = int((time.monotonic() - t0) * 1000)
        return "offline", str(ex), None, latency_ms


def get_dotnet_status() -> ServiceStatus:
    base_url = settings.DOTNET_API_BASE_URL.rstrip("/")
    status, detail, payload, latency_ms = _fetch_json(f"{base_url}/healthz")
    return ServiceStatus(
        service_name=".NET API",
        base_url=base_url,
        status=status,
        detail=detail,
        payload=payload,
        latency_ms=latency_ms,
        slug=_SLUG_MAP[".NET API"],
    )


def get_fastapi_status() -> ServiceStatus:
    base_url = settings.FASTAPI_BASE_URL.rstrip("/")
    status, detail, payload, latency_ms = _fetch_json(f"{base_url}/health")
    return ServiceStatus(
        service_name="FastAPI Sidecar",
        base_url=base_url,
        status=status,
        detail=detail,
        payload=payload,
        latency_ms=latency_ms,
        slug=_SLUG_MAP["FastAPI Sidecar"],
    )


def get_minio_status() -> ServiceStatus:
    base_url = settings.MINIO_BASE_URL.rstrip("/")
    status, detail, payload, latency_ms = _fetch_text_health(f"{base_url}/minio/health/live")
    return ServiceStatus(
        service_name="MinIO",
        base_url=base_url,
        status=status,
        detail=detail,
        payload=payload,
        latency_ms=latency_ms,
        slug=_SLUG_MAP["MinIO"],
    )


def list_service_statuses() -> list[ServiceStatus]:
    return [get_dotnet_status(), get_fastapi_status(), get_minio_status()]


def get_overall_status(services: list[ServiceStatus]) -> str:
    if any(service.status == "offline" for service in services):
        return "offline"

    if any(service.status == "degraded" for service in services):
        return "degraded"

    return "healthy"
