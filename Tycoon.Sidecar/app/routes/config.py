"""
Config proxy — exposes the AdminAppConfig feature flags from the .NET API.

Routes:
  GET /config  — fetch current feature flags / game balance config
"""

import logging
from fastapi import APIRouter, Request, Response

router = APIRouter()
logger = logging.getLogger(__name__)

_ETAG_KEY = "_config_etag"
_CACHE_KEY = "_config_cache"


@router.get("")
async def get_config(request: Request) -> Response:
    """
    Fetches AdminAppConfig from the .NET backend and returns it.
    Uses ETag-based conditional caching: if the config hasn't changed,
    the cached copy is returned without a round-trip.
    """
    client = request.app.state.backend
    headers: dict[str, str] = {}

    cached_etag = getattr(request.app.state, _ETAG_KEY, None)
    cached_body = getattr(request.app.state, _CACHE_KEY, None)

    if cached_etag:
        headers["If-None-Match"] = cached_etag

    try:
        resp = await client.get("/admin/config", headers=headers)
    except Exception as exc:
        logger.warning("Config fetch failed: %s", exc)
        if cached_body:
            return Response(content=cached_body, status_code=200, media_type="application/json")
        return Response(content=b'{"error":"config unavailable"}', status_code=503, media_type="application/json")

    if resp.status_code == 304 and cached_body:
        return Response(content=cached_body, status_code=200, media_type="application/json")

    if resp.is_success:
        setattr(request.app.state, _CACHE_KEY, resp.content)
        etag = resp.headers.get("etag")
        if etag:
            setattr(request.app.state, _ETAG_KEY, etag)

    return Response(
        content=resp.content,
        status_code=resp.status_code,
        media_type=resp.headers.get("content-type", "application/json"),
    )
