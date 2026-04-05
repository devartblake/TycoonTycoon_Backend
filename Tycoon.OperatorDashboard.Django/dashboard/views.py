from functools import wraps
from time import time

import httpx
from django.contrib import messages
from django.http import JsonResponse
from django.shortcuts import redirect, render
from django.utils import timezone

from .services.admin_audit_client import get_security_audit
from .services.admin_auth_client import admin_login, admin_me, admin_refresh
from .services.admin_media_client import create_upload_intent
from .services.admin_moderation_client import get_moderation_logs, get_moderation_profile, set_moderation_status
from .services.admin_users_client import (
    ban_admin_user,
    get_admin_user,
    get_admin_user_activity,
    list_admin_users,
    unban_admin_user,
    update_admin_user,
)
from .services.api_clients import get_overall_status, list_service_statuses
from .services.minio_diagnostics import get_minio_diagnostics
from .services.upstream_error import build_upstream_http_error_response, build_upstream_unavailable_response

STATUS_CLASSES = {
    "healthy": "status-ok",
    "degraded": "status-warn",
    "offline": "status-bad",
}

SESSION_ACCESS_TOKEN_KEY = "operator_access_token"
SESSION_REFRESH_TOKEN_KEY = "operator_refresh_token"
SESSION_ADMIN_PROFILE_KEY = "operator_admin_profile"
SESSION_ACCESS_EXP_KEY = "operator_access_expires_at"


def _is_access_token_expired(request) -> bool:
    exp = request.session.get(SESSION_ACCESS_EXP_KEY)
    if not exp:
        return True
    return float(exp) <= time() + 15


def _try_refresh_session(request) -> bool:
    refresh_token = request.session.get(SESSION_REFRESH_TOKEN_KEY)
    if not refresh_token:
        return False

    try:
        refreshed = admin_refresh(refresh_token)
        request.session[SESSION_ACCESS_TOKEN_KEY] = refreshed.access_token
        request.session[SESSION_ACCESS_EXP_KEY] = time() + float(refreshed.expires_in)

        profile = admin_me(refreshed.access_token)
        request.session[SESSION_ADMIN_PROFILE_KEY] = profile
        return True
    except httpx.HTTPError:
        return False


def _has_permission(request, permission: str) -> bool:
    profile = request.session.get(SESSION_ADMIN_PROFILE_KEY) or {}
    permissions = set(profile.get("permissions") or [])
    return permission in permissions


def operator_login_required(view_func):
    @wraps(view_func)
    def _wrapped(request, *args, **kwargs):
        if not request.session.get(SESSION_ACCESS_TOKEN_KEY):
            return redirect("operator-login")

        if _is_access_token_expired(request) and not _try_refresh_session(request):
            request.session.flush()
            return redirect("operator-login")

        return view_func(request, *args, **kwargs)

    return _wrapped


def require_permission(permission: str):
    def decorator(view_func):
        @wraps(view_func)
        def _wrapped(request, *args, **kwargs):
            if not _has_permission(request, permission):
                return JsonResponse(
                    {"code": "FORBIDDEN", "message": f"Missing required permission: {permission}"},
                    status=403,
                )
            return view_func(request, *args, **kwargs)

        return _wrapped

    return decorator


@operator_login_required
def dashboard_home(request):
    services = list_service_statuses()

    for service in services:
        service.css_class = STATUS_CLASSES.get(service.status, "status-unknown")

    context = {
        "services": services,
        "overall_status": get_overall_status(services),
        "generated_at": timezone.now(),
        "admin_profile": request.session.get(SESSION_ADMIN_PROFILE_KEY),
    }
    return render(request, "dashboard/home.html", context)


@operator_login_required
def operator_health(request):
    services = list_service_statuses()
    return JsonResponse(
        {
            "status": get_overall_status(services),
            "services": [service.to_dict() for service in services],
            "generatedAt": timezone.now().isoformat(),
            "admin": request.session.get(SESSION_ADMIN_PROFILE_KEY),
        }
    )


@operator_login_required
@require_permission("users:read")
def operator_users(request):
    access_token = request.session.get(SESSION_ACCESS_TOKEN_KEY)

    query = {
        "q": request.GET.get("q"),
        "status": request.GET.get("status"),
        "role": request.GET.get("role"),
        "isVerified": request.GET.get("isVerified"),
        "isBanned": request.GET.get("isBanned"),
        "page": request.GET.get("page", 1),
        "pageSize": request.GET.get("pageSize", 25),
        "sortBy": request.GET.get("sortBy", "createdAt"),
        "sortOrder": request.GET.get("sortOrder", "desc"),
    }
    query = {k: v for k, v in query.items() if v not in (None, "")}

    try:
        payload = list_admin_users(access_token, query)
        return JsonResponse(payload)
    except httpx.HTTPStatusError as ex:
        return build_upstream_http_error_response(ex, "Admin users endpoint failed.")
    except httpx.RequestError:
        return build_upstream_unavailable_response("Unable to reach backend admin users endpoint.")


@operator_login_required
@require_permission("users:read")
def operator_user_detail(request, user_id: str):
    access_token = request.session.get(SESSION_ACCESS_TOKEN_KEY)
    try:
        payload = get_admin_user(access_token, user_id)
        return JsonResponse(payload)
    except httpx.HTTPStatusError as ex:
        return build_upstream_http_error_response(ex, "Admin user detail failed.")
    except httpx.RequestError:
        return build_upstream_unavailable_response("Unable to reach backend admin user detail endpoint.")


@operator_login_required
@require_permission("users:write")
def operator_user_ban(request, user_id: str):
    reason = (request.POST.get("reason") or request.GET.get("reason") or "Operator action").strip()
    until = request.POST.get("until") or request.GET.get("until")

    access_token = request.session.get(SESSION_ACCESS_TOKEN_KEY)
    try:
        payload = ban_admin_user(access_token, user_id, reason, until)
        return JsonResponse(payload)
    except httpx.HTTPStatusError as ex:
        return build_upstream_http_error_response(ex, "Ban endpoint failed.")
    except httpx.RequestError:
        return build_upstream_unavailable_response("Unable to reach backend ban endpoint.")


@operator_login_required
@require_permission("users:write")
def operator_user_unban(request, user_id: str):
    access_token = request.session.get(SESSION_ACCESS_TOKEN_KEY)
    try:
        payload = unban_admin_user(access_token, user_id)
        return JsonResponse(payload)
    except httpx.HTTPStatusError as ex:
        return build_upstream_http_error_response(ex, "Unban endpoint failed.")
    except httpx.RequestError:
        return build_upstream_unavailable_response("Unable to reach backend unban endpoint.")


@operator_login_required
@require_permission("users:read")
def operator_user_activity(request, user_id: str):
    access_token = request.session.get(SESSION_ACCESS_TOKEN_KEY)
    query = {
        "from": request.GET.get("from"),
        "to": request.GET.get("to"),
        "type": request.GET.get("type"),
        "page": request.GET.get("page", 1),
        "pageSize": request.GET.get("pageSize", 25),
    }
    query = {k: v for k, v in query.items() if v not in (None, "")}

    try:
        payload = get_admin_user_activity(access_token, user_id, query)
        return JsonResponse(payload)
    except httpx.HTTPStatusError as ex:
        return build_upstream_http_error_response(ex, "User activity endpoint failed.")
    except httpx.RequestError:
        return build_upstream_unavailable_response("Unable to reach backend user activity endpoint.")


@operator_login_required
@require_permission("users:write")
def operator_user_update(request, user_id: str):
    payload = {
        "username": request.POST.get("username") or request.GET.get("username"),
        "role": request.POST.get("role") or request.GET.get("role"),
        "isVerified": request.POST.get("isVerified") or request.GET.get("isVerified"),
    }

    # Normalize bool-ish input for isVerified if provided
    raw_verified = payload.get("isVerified")
    if raw_verified is not None:
        payload["isVerified"] = str(raw_verified).lower() in {"1", "true", "yes", "on"}

    payload = {k: v for k, v in payload.items() if v is not None and v != ""}

    if not payload:
        return JsonResponse({"code": "VALIDATION_ERROR", "message": "No updatable fields were provided."}, status=422)

    access_token = request.session.get(SESSION_ACCESS_TOKEN_KEY)
    try:
        response_payload = update_admin_user(access_token, user_id, payload)
        return JsonResponse(response_payload)
    except httpx.HTTPStatusError as ex:
        return build_upstream_http_error_response(ex, "User update endpoint failed.")
    except httpx.RequestError:
        return build_upstream_unavailable_response("Unable to reach backend user update endpoint.")


@operator_login_required
@require_permission("events:read")
def operator_audit_security(request):
    access_token = request.session.get(SESSION_ACCESS_TOKEN_KEY)
    query = {
        "from": request.GET.get("from"),
        "to": request.GET.get("to"),
        "status": request.GET.get("status"),
        "page": request.GET.get("page", 1),
        "pageSize": request.GET.get("pageSize", 25),
    }
    query = {k: v for k, v in query.items() if v not in (None, "")}

    try:
        payload = get_security_audit(access_token, query)
        return JsonResponse(payload)
    except httpx.HTTPStatusError as ex:
        return build_upstream_http_error_response(ex, "Security audit endpoint failed.")
    except httpx.RequestError:
        return build_upstream_unavailable_response("Unable to reach backend security audit endpoint.")


@operator_login_required
@require_permission("events:read")
def operator_moderation_profile(request, player_id: str):
    access_token = request.session.get(SESSION_ACCESS_TOKEN_KEY)
    try:
        payload = get_moderation_profile(access_token, player_id)
        return JsonResponse(payload)
    except httpx.HTTPStatusError as ex:
        return build_upstream_http_error_response(ex, "Moderation profile endpoint failed.")
    except httpx.RequestError:
        return build_upstream_unavailable_response("Unable to reach backend moderation profile endpoint.")


@operator_login_required
@require_permission("events:read")
def operator_moderation_logs(request):
    access_token = request.session.get(SESSION_ACCESS_TOKEN_KEY)
    query = {
        "page": request.GET.get("page", 1),
        "pageSize": request.GET.get("pageSize", 25),
        "playerId": request.GET.get("playerId"),
        "status": request.GET.get("status"),
    }
    query = {k: v for k, v in query.items() if v not in (None, "")}

    try:
        payload = get_moderation_logs(access_token, query)
        return JsonResponse(payload)
    except httpx.HTTPStatusError as ex:
        return build_upstream_http_error_response(ex, "Moderation logs endpoint failed.")
    except httpx.RequestError:
        return build_upstream_unavailable_response("Unable to reach backend moderation logs endpoint.")


@operator_login_required
@require_permission("events:write")
def operator_moderation_set_status(request):
    admin_profile = request.session.get(SESSION_ADMIN_PROFILE_KEY) or {}
    admin_user = admin_profile.get("email")

    payload = {
        "playerId": request.POST.get("playerId") or request.GET.get("playerId"),
        "status": request.POST.get("status") or request.GET.get("status"),
        "reason": request.POST.get("reason") or request.GET.get("reason"),
        "notes": request.POST.get("notes") or request.GET.get("notes"),
        "expiresAtUtc": request.POST.get("expiresAtUtc") or request.GET.get("expiresAtUtc"),
        "relatedFlagId": request.POST.get("relatedFlagId") or request.GET.get("relatedFlagId"),
    }
    payload = {k: v for k, v in payload.items() if v not in (None, "")}

    required = {"playerId", "status", "reason"}
    missing = sorted(required - set(payload.keys()))
    if missing:
        return JsonResponse({"code": "VALIDATION_ERROR", "message": f"Missing required fields: {', '.join(missing)}"}, status=422)

    access_token = request.session.get(SESSION_ACCESS_TOKEN_KEY)
    try:
        response_payload = set_moderation_status(access_token, admin_user, payload)
        return JsonResponse(response_payload)
    except httpx.HTTPStatusError as ex:
        return build_upstream_http_error_response(ex, "Moderation set-status endpoint failed.")
    except httpx.RequestError:
        return build_upstream_unavailable_response("Unable to reach backend moderation set-status endpoint.")


@operator_login_required
@require_permission("questions:write")
def operator_media_intent(request):
    file_name = request.POST.get("fileName") or request.GET.get("fileName")
    content_type = request.POST.get("contentType") or request.GET.get("contentType")
    size_bytes = request.POST.get("sizeBytes") or request.GET.get("sizeBytes")

    if not file_name or not content_type or not size_bytes:
        return JsonResponse({"code": "VALIDATION_ERROR", "message": "fileName, contentType, and sizeBytes are required."}, status=422)

    try:
        size_value = int(size_bytes)
    except (TypeError, ValueError):
        return JsonResponse({"code": "VALIDATION_ERROR", "message": "sizeBytes must be an integer."}, status=422)

    access_token = request.session.get(SESSION_ACCESS_TOKEN_KEY)
    try:
        payload = create_upload_intent(access_token, file_name, content_type, size_value)
        return JsonResponse(payload)
    except httpx.HTTPStatusError as ex:
        return build_upstream_http_error_response(ex, "Media intent endpoint failed.")
    except httpx.RequestError:
        return build_upstream_unavailable_response("Unable to reach backend media intent endpoint.")


@operator_login_required
@require_permission("users:read")
def operator_minio_diagnostics(request):
    payload = get_minio_diagnostics()
    status_map = {"healthy": 200, "degraded": 200, "offline": 503}
    return JsonResponse(payload, status=status_map.get(payload.get("overallStatus", "degraded"), 200))


def login_view(request):
    if request.method == "GET":
        if request.session.get(SESSION_ACCESS_TOKEN_KEY):
            return redirect("dashboard-home")
        return render(request, "dashboard/login.html")

    email = request.POST.get("email", "").strip()
    password = request.POST.get("password", "")

    if not email or not password:
        messages.error(request, "Email and password are required.")
        return render(request, "dashboard/login.html", status=400)

    try:
        login_result = admin_login(email, password)
        admin_profile = admin_me(login_result.access_token)

        request.session[SESSION_ACCESS_TOKEN_KEY] = login_result.access_token
        request.session[SESSION_REFRESH_TOKEN_KEY] = login_result.refresh_token
        request.session[SESSION_ADMIN_PROFILE_KEY] = admin_profile
        request.session[SESSION_ACCESS_EXP_KEY] = time() + float(login_result.expires_in)
        request.session.set_expiry(login_result.expires_in)

        return redirect("dashboard-home")
    except httpx.HTTPStatusError as ex:
        if ex.response.status_code in (401, 403):
            messages.error(request, "Invalid operator credentials or access not allowed.")
            return render(request, "dashboard/login.html", status=401)

        messages.error(request, f"Login failed with HTTP {ex.response.status_code}.")
        return render(request, "dashboard/login.html", status=502)
    except httpx.RequestError:
        messages.error(request, "Unable to reach backend auth service.")
        return render(request, "dashboard/login.html", status=503)


def logout_view(request):
    request.session.pop(SESSION_ACCESS_TOKEN_KEY, None)
    request.session.pop(SESSION_REFRESH_TOKEN_KEY, None)
    request.session.pop(SESSION_ADMIN_PROFILE_KEY, None)
    request.session.pop(SESSION_ACCESS_EXP_KEY, None)
    request.session.flush()
    return redirect("operator-login")


def healthz(request):
    return JsonResponse({"status": "ok"})
