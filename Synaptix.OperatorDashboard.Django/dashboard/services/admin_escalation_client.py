from typing import Any

import httpx
from django.conf import settings

from .admin_auth_client import _headers


def run_escalation(access_token: str, scope: str, dry_run: bool = True) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/moderation/escalation/run"
    r = httpx.post(
        url,
        json={"scope": scope, "dryRun": dry_run},
        headers=_headers(access_token),
        timeout=settings.API_REQUEST_TIMEOUT_SECONDS,
    )
    r.raise_for_status()
    return r.json()
