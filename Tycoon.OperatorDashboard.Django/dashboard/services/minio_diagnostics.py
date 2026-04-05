from time import perf_counter
from typing import Any

import httpx
from django.conf import settings


def get_minio_diagnostics() -> dict[str, Any]:
    base_url = settings.MINIO_BASE_URL.rstrip("/")
    result: dict[str, Any] = {
        "baseUrl": base_url,
        "checks": {},
    }

    for name, path in {
        "live": "/minio/health/live",
        "ready": "/minio/health/ready",
    }.items():
        started = perf_counter()
        url = f"{base_url}{path}"
        try:
            response = httpx.get(url, timeout=settings.API_REQUEST_TIMEOUT_SECONDS)
            elapsed_ms = round((perf_counter() - started) * 1000, 2)
            result["checks"][name] = {
                "status": "healthy" if response.is_success else "degraded",
                "httpStatus": response.status_code,
                "latencyMs": elapsed_ms,
                "body": (response.text or "").strip(),
            }
        except httpx.RequestError as ex:
            elapsed_ms = round((perf_counter() - started) * 1000, 2)
            result["checks"][name] = {
                "status": "offline",
                "httpStatus": None,
                "latencyMs": elapsed_ms,
                "error": str(ex),
            }

    statuses = {entry["status"] for entry in result["checks"].values()}
    if "offline" in statuses:
        result["overallStatus"] = "offline"
    elif "degraded" in statuses:
        result["overallStatus"] = "degraded"
    else:
        result["overallStatus"] = "healthy"

    return result
