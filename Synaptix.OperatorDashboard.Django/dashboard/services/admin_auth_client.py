from dataclasses import dataclass
import base64
import json
import secrets
from typing import Any

import httpx
from django.conf import settings
from .http_client_pool import (
    get_http_client,
    get_kms_session,
    cache_kms_session,
    clear_kms_session,
)


@dataclass
class AdminLoginResult:
    access_token: str
    refresh_token: str
    expires_in: int
    admin: dict[str, Any]


@dataclass
class AdminRefreshResult:
    access_token: str
    expires_in: int


class AdminAuthConfigurationError(RuntimeError):
    pass


class KmsUnavailableError(RuntimeError):
    pass


def _headers(access_token: str | None = None) -> dict[str, str]:
    headers: dict[str, str] = {"Content-Type": "application/json"}

    if settings.ADMIN_OPS_KEY:
        headers[settings.ADMIN_OPS_HEADER] = settings.ADMIN_OPS_KEY

    if access_token:
        headers["Authorization"] = f"Bearer {access_token}"

    return headers


def _auth_transport() -> str:
    transport = getattr(settings, "ADMIN_AUTH_TRANSPORT", "auto") or "auto"
    transport = transport.strip().lower()
    if transport not in {"auto", "plain", "secure-channel"}:
        raise AdminAuthConfigurationError("ADMIN_AUTH_TRANSPORT must be one of: auto, plain, secure-channel.")
    if transport == "auto":
        if getattr(settings, "KMS_API_BASE_URL", "") and getattr(settings, "KMS_SERVICE_TOKEN", ""):
            return "secure-channel"
        return "plain"
    return transport


def _kms_headers() -> dict[str, str]:
    token = getattr(settings, "KMS_SERVICE_TOKEN", "")
    if not token:
        raise AdminAuthConfigurationError("KMS_SERVICE_TOKEN is required for secure-channel admin auth.")
    return {"Content-Type": "application/json", "X-Service-Token": token}


def _start_internal_session() -> str:
    cached = get_kms_session()
    if cached is not None:
        return cached["sessionId"]

    base_url = getattr(settings, "KMS_API_BASE_URL", "").rstrip("/")
    if not base_url:
        raise AdminAuthConfigurationError("KMS_API_BASE_URL is required for secure-channel admin auth.")

    client = get_http_client()
    response = client.post(
        f"{base_url}/internal/security/sessions/start",
        json={
            "subjectId": "operator-dashboard-django",
            "deviceId": "django-admin-auth",
            "supportedSuites": ["X25519-HKDF-SHA256-AES256GCM"],
        },
        headers=_kms_headers(),
    )
    response.raise_for_status()
    payload = response.json()
    session_id = payload["sessionId"]
    cache_kms_session(session_id)
    return session_id


def _secure_channel_aad(direction: str, method: str, path: str, session_id: str, sequence: int, subject_id: str = "") -> str:
    return "|".join(
        [
            "syn-sec-v1",
            direction,
            method.upper(),
            path,
            session_id.replace("-", ""),
            str(sequence),
            subject_id,
        ]
    )


def _kms_encrypt(session_id: str, payload: dict[str, Any], aad: str) -> dict[str, Any]:
    base_url = getattr(settings, "KMS_API_BASE_URL", "").rstrip("/")
    plaintext = json.dumps(payload).encode("utf-8")
    client = get_http_client()
    response = client.post(
        f"{base_url}/internal/security/encrypt",
        json={
            "sessionId": session_id,
            "plaintext": base64.b64encode(plaintext).decode("ascii"),
            "contentType": "application/json",
            "aad": aad,
            "direction": "client-to-server",
        },
        headers=_kms_headers(),
    )
    response.raise_for_status()
    return response.json()


def _kms_decrypt(session_id: str, envelope: dict[str, Any], sequence: int, replay_nonce: str, aad: str) -> dict[str, Any]:
    base_url = getattr(settings, "KMS_API_BASE_URL", "").rstrip("/")
    client = get_http_client()
    response = client.post(
        f"{base_url}/internal/security/decrypt",
        json={
            "sessionId": session_id,
            **envelope,
            "sequenceNumber": sequence,
            "replayNonce": replay_nonce,
            "aad": aad,
            "direction": "server-to-client",
            "enforceReplay": False,
        },
        headers=_kms_headers(),
    )
    response.raise_for_status()
    payload = response.json()
    plaintext = base64.b64decode(payload["plaintext"])
    return json.loads(plaintext.decode("utf-8"))


def _post_admin_auth(path: str, payload: dict[str, Any]) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}{path}"
    client = get_http_client()

    if _auth_transport() == "plain":
        response = client.post(
            url,
            json=payload,
            headers=_headers(),
        )
        response.raise_for_status()
        return response.json()

    try:
        session_id = _start_internal_session()
        sequence = 1
        request_nonce = secrets.token_urlsafe(18)
        response_nonce = secrets.token_urlsafe(18)
        request_aad = _secure_channel_aad("request", "POST", path, session_id, sequence)
        response_aad = _secure_channel_aad("response", "POST", path, session_id, sequence)
        encrypted = _kms_encrypt(session_id, payload, request_aad)
        headers = _headers()
        headers["X-Syn-Sec-Session"] = session_id
        headers["X-Syn-Sec-Seq"] = str(sequence)
        headers["X-Syn-Sec-Nonce"] = request_nonce
        response = client.post(
            url,
            json=encrypted,
            headers=headers,
        )
        response.raise_for_status()
        return _kms_decrypt(session_id, response.json(), sequence, response_nonce, response_aad)
    except httpx.RequestError as ex:
        raise KmsUnavailableError("Unable to complete secure-channel admin auth.") from ex


def admin_login(email: str, password: str) -> AdminLoginResult:
    payload = _post_admin_auth("/admin/auth/login", {"email": email, "password": password})

    return AdminLoginResult(
        access_token=payload["accessToken"],
        refresh_token=payload["refreshToken"],
        expires_in=payload["expiresIn"],
        admin=payload["admin"],
    )


def admin_refresh(refresh_token: str) -> AdminRefreshResult:
    payload = _post_admin_auth("/admin/auth/refresh", {"refreshToken": refresh_token})

    return AdminRefreshResult(
        access_token=payload["accessToken"],
        expires_in=payload["expiresIn"],
    )


def admin_me(access_token: str) -> dict[str, Any]:
    url = f"{settings.DOTNET_API_BASE_URL.rstrip('/')}/admin/auth/me"
    client = get_http_client()
    response = client.get(
        url,
        headers=_headers(access_token),
    )
    response.raise_for_status()
    return response.json()


def admin_forgot_password(email: str) -> dict[str, Any]:
    payload = _post_admin_auth("/admin/auth/forgot-password", {"email": email})
    return payload


def admin_reset_password(token: str, new_password: str, confirm_password: str) -> dict[str, Any]:
    payload = _post_admin_auth(
        "/admin/auth/reset-password",
        {
            "token": token,
            "newPassword": new_password,
            "confirmPassword": confirm_password,
        }
    )
    return payload


def admin_validate_reset_token(token: str) -> dict[str, Any]:
    payload = _post_admin_auth("/admin/auth/validate-reset-token", {"token": token})
    return payload
