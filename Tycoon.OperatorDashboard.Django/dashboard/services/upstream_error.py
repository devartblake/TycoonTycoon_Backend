from typing import Any

import httpx
from django.http import JsonResponse


def build_upstream_http_error_response(
    ex: httpx.HTTPStatusError,
    fallback_message: str,
    fallback_code: str = "UPSTREAM_ERROR",
) -> JsonResponse:
    status = ex.response.status_code

    payload: dict[str, Any]
    try:
        parsed = ex.response.json()
        if isinstance(parsed, dict):
            payload = parsed
        else:
            payload = {"code": fallback_code, "message": fallback_message}
    except ValueError:
        payload = {"code": fallback_code, "message": fallback_message}

    payload.setdefault("code", fallback_code)
    payload.setdefault("message", fallback_message)

    return JsonResponse(payload, status=status)


def build_upstream_unavailable_response(message: str) -> JsonResponse:
    return JsonResponse({"code": "UPSTREAM_UNAVAILABLE", "message": message}, status=503)
