import csv
from functools import wraps
from io import StringIO
from time import time

import httpx
from django.contrib import messages
from django.db import DatabaseError
from django.db.models import Q
from django.http import HttpResponse, JsonResponse
from django.shortcuts import redirect, render
from django.utils import timezone

from .models import OperatorSavedView, OperatorSavedViewAuditEvent
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
SESSION_USERS_SAVED_VIEWS_KEY = "operator_users_saved_views"


def _saved_view_owner(request) -> str:
    profile = request.session.get(SESSION_ADMIN_PROFILE_KEY) or {}
    return (profile.get("email") or "operator@local").strip().lower()


def _load_saved_views(request) -> dict:
    owner = _saved_view_owner(request)
    try:
        rows = OperatorSavedView.objects.filter((Q(owner_email=owner) | Q(is_shared=True)) & Q(is_archived=False)).values(
            "owner_email",
            "name",
            "query",
            "is_shared",
        )
        result = {}
        for row in rows:
            key = f"{row['owner_email']}::{row['name']}"
            result[key] = {
                "name": row["name"],
                "owner_email": row["owner_email"],
                "is_shared": row["is_shared"],
                "query": row.get("query") or {},
            }
        return result
    except DatabaseError:
        return request.session.get(SESSION_USERS_SAVED_VIEWS_KEY) or {}


def _persist_saved_views_fallback(request, saved_views: dict):
    request.session[SESSION_USERS_SAVED_VIEWS_KEY] = saved_views


def _save_named_view(request, view_name: str, view_query: dict, is_shared: bool):
    owner = _saved_view_owner(request)
    try:
        OperatorSavedView.objects.update_or_create(
            owner_email=owner,
            name=view_name,
            defaults={"query": view_query, "is_shared": is_shared, "is_archived": False},
        )
        OperatorSavedViewAuditEvent.objects.create(
            actor_email=owner,
            owner_email=owner,
            view_name=view_name,
            action="save",
            metadata={"is_shared": is_shared},
        )
    except DatabaseError:
        saved_views = request.session.get(SESSION_USERS_SAVED_VIEWS_KEY) or {}
        saved_views[view_name] = view_query
        _persist_saved_views_fallback(request, saved_views)


def _delete_named_view(request, view_name: str):
    owner = _saved_view_owner(request)
    target_owner = owner
    target_name = view_name
    if "::" in view_name:
        target_owner, target_name = view_name.split("::", 1)
    try:
        deleted, _ = OperatorSavedView.objects.filter(owner_email=target_owner, name=target_name).delete()
        if deleted:
            OperatorSavedViewAuditEvent.objects.create(
                actor_email=owner,
                owner_email=target_owner,
                view_name=target_name,
                action="delete",
                metadata={},
            )
        return deleted > 0
    except DatabaseError:
        saved_views = request.session.get(SESSION_USERS_SAVED_VIEWS_KEY) or {}
        existed = target_name in saved_views
        if existed:
            saved_views.pop(target_name, None)
            _persist_saved_views_fallback(request, saved_views)
        return existed


def _archive_named_view(request, view_name: str):
    owner = _saved_view_owner(request)
    target_owner, target_name = view_name.split("::", 1) if "::" in view_name else (owner, view_name)
    try:
        updated = OperatorSavedView.objects.filter(owner_email=target_owner, name=target_name).update(is_archived=True)
        if updated:
            OperatorSavedViewAuditEvent.objects.create(
                actor_email=owner,
                owner_email=target_owner,
                view_name=target_name,
                action="archive",
                metadata={},
            )
        return updated > 0
    except DatabaseError:
        return False


def _transfer_named_view(request, view_name: str, new_owner_email: str):
    actor = _saved_view_owner(request)
    target_owner, target_name = view_name.split("::", 1) if "::" in view_name else (actor, view_name)
    try:
        updated = OperatorSavedView.objects.filter(owner_email=target_owner, name=target_name).update(owner_email=new_owner_email)
        if updated:
            OperatorSavedViewAuditEvent.objects.create(
                actor_email=actor,
                owner_email=new_owner_email,
                view_name=target_name,
                action="transfer",
                metadata={"from_owner": target_owner},
            )
        return updated > 0
    except DatabaseError:
        return False


def _to_csv_response(rows: list[dict], fieldnames: list[str], filename: str):
    buffer = StringIO()
    writer = csv.DictWriter(buffer, fieldnames=fieldnames)
    writer.writeheader()
    for row in rows:
        writer.writerow({field: row.get(field, "") for field in fieldnames})

    response = HttpResponse(buffer.getvalue(), status=200)
    response["Content-Type"] = "text/csv; charset=utf-8"
    response["Content-Disposition"] = f'attachment; filename="{filename}"'
    return response


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
def operator_users_view(request):
    access_token = request.session.get(SESSION_ACCESS_TOKEN_KEY)
    bulk_result = None
    saved_views = _load_saved_views(request)

    if request.method == "POST":
        action = (request.POST.get("action") or "").strip().lower()

        if action == "save_view":
            view_name = (request.POST.get("viewName") or "").strip()
            if not view_name:
                messages.error(request, "View name is required to save a users triage view.")
            else:
                view_query = {
                    "preset": request.POST.get("preset"),
                    "q": request.POST.get("q"),
                    "status": request.POST.get("status"),
                    "role": request.POST.get("role"),
                    "isVerified": request.POST.get("isVerified"),
                    "isBanned": request.POST.get("isBanned"),
                    "pageSize": request.POST.get("pageSize"),
                    "sortBy": request.POST.get("sortBy"),
                    "sortOrder": request.POST.get("sortOrder"),
                }
                is_shared = str(request.POST.get("isShared") or "").lower() in {"1", "true", "on", "yes"}
                _save_named_view(request, view_name, view_query, is_shared)
                saved_views = _load_saved_views(request)
                messages.success(request, f"Saved users view '{view_name}'.")
        elif action == "delete_view":
            view_name = (request.POST.get("viewName") or "").strip()
            owner_prefix = _saved_view_owner(request)
            if "::" in view_name and not view_name.startswith(f"{owner_prefix}::"):
                messages.error(request, "Only the owner can delete a shared saved view.")
            elif view_name and _delete_named_view(request, view_name):
                saved_views = _load_saved_views(request)
                messages.success(request, f"Deleted users view '{view_name}'.")
            else:
                messages.error(request, "Saved view not found.")
        elif action == "archive_view":
            view_name = (request.POST.get("viewName") or "").strip()
            owner_prefix = _saved_view_owner(request)
            if "::" in view_name and not view_name.startswith(f"{owner_prefix}::"):
                messages.error(request, "Only the owner can archive a shared saved view.")
            elif view_name and _archive_named_view(request, view_name):
                saved_views = _load_saved_views(request)
                messages.success(request, f"Archived users view '{view_name}'.")
            else:
                messages.error(request, "Saved view not found.")
        elif action == "transfer_view":
            view_name = (request.POST.get("viewName") or "").strip()
            owner_prefix = _saved_view_owner(request)
            new_owner = (request.POST.get("newOwnerEmail") or "").strip().lower()
            if "::" in view_name and not view_name.startswith(f"{owner_prefix}::"):
                messages.error(request, "Only the owner can transfer a shared saved view.")
            elif not new_owner:
                messages.error(request, "New owner email is required for transfer.")
            elif view_name and _transfer_named_view(request, view_name, new_owner):
                saved_views = _load_saved_views(request)
                messages.success(request, f"Transferred users view '{view_name}' to {new_owner}.")
            else:
                messages.error(request, "Saved view not found.")
        else:
            if not _has_permission(request, "users:write"):
                return JsonResponse({"code": "FORBIDDEN", "message": "Missing required permission: users:write"}, status=403)

            user_ids = [user_id.strip() for user_id in request.POST.getlist("userIds") if user_id.strip()]
            reason = (request.POST.get("reason") or "Operator bulk action").strip()
            dry_run = str(request.POST.get("dryRun") or "").lower() in {"1", "true", "on", "yes"}
            confirmed = (request.POST.get("confirm") or "").strip().upper() == "YES"

            if action not in {"ban", "unban"}:
                messages.error(request, "Bulk action must be 'ban' or 'unban'.")
            elif not user_ids:
                messages.error(request, "Select at least one user for bulk action.")
            elif not dry_run and not confirmed:
                messages.error(request, "Type YES in confirmation to execute a live bulk action, or use dry-run.")
            else:
                succeeded = []
                failed = []
                if dry_run:
                    succeeded = list(user_ids)
                else:
                    for user_id in user_ids:
                        try:
                            if action == "ban":
                                ban_admin_user(access_token, user_id, reason, None)
                            else:
                                unban_admin_user(access_token, user_id)
                            succeeded.append(user_id)
                        except httpx.HTTPError:
                            failed.append(user_id)

                bulk_result = {
                    "action": action,
                    "succeeded": succeeded,
                    "failed": failed,
                    "dry_run": dry_run,
                }
                if dry_run:
                    messages.warning(request, f"Dry-run: {len(succeeded)} users selected for bulk {action}.")
                elif failed:
                    messages.warning(request, f"Bulk {action} completed with {len(failed)} failures.")
                else:
                    messages.success(request, f"Bulk {action} succeeded for {len(succeeded)} users.")

    preset = (request.GET.get("preset") or "").strip()
    selected_saved_view = (request.GET.get("savedView") or "").strip()
    saved_entry = saved_views.get(selected_saved_view, {})
    saved_query = saved_entry.get("query", saved_entry if isinstance(saved_entry, dict) else {})
    try:
        page = max(1, int(request.GET.get("page", 1) or 1))
    except ValueError:
        page = 1

    try:
        page_size = max(1, int(request.GET.get("pageSize", 25) or 25))
    except ValueError:
        page_size = 25
    query = {
        "q": request.GET.get("q") or saved_query.get("q"),
        "status": request.GET.get("status") or saved_query.get("status"),
        "role": request.GET.get("role") or saved_query.get("role"),
        "isVerified": request.GET.get("isVerified") or saved_query.get("isVerified"),
        "isBanned": request.GET.get("isBanned") or saved_query.get("isBanned"),
        "page": page,
        "pageSize": request.GET.get("pageSize") or saved_query.get("pageSize") or page_size,
        "sortBy": request.GET.get("sortBy") or saved_query.get("sortBy") or "createdAt",
        "sortOrder": request.GET.get("sortOrder") or saved_query.get("sortOrder") or "desc",
    }
    if preset == "banned_recent":
        query["isBanned"] = "true"
        query["sortBy"] = "updatedAt"
        query["sortOrder"] = "desc"
    elif preset == "new_unverified":
        query["isVerified"] = "false"
        query["sortBy"] = "createdAt"
        query["sortOrder"] = "desc"
    elif preset == "admins":
        query["role"] = "admin"
        query["sortBy"] = "updatedAt"
        query["sortOrder"] = "desc"
    query = {k: v for k, v in query.items() if v not in (None, "")}

    context = {
        "query": query,
        "preset": preset,
        "items": [],
        "total": 0,
        "page": page,
        "page_size": page_size,
        "has_prev": page > 1,
        "has_next": False,
        "prev_page": max(1, page - 1),
        "next_page": page + 1,
        "admin_profile": request.session.get(SESSION_ADMIN_PROFILE_KEY),
        "can_write_users": _has_permission(request, "users:write"),
        "bulk_result": bulk_result,
        "saved_views": sorted(saved_views.keys()),
        "saved_view_entries": [{"key": key, **value} for key, value in sorted(saved_views.items()) if isinstance(value, dict)],
        "selected_saved_view": selected_saved_view,
    }

    try:
        payload = list_admin_users(access_token, query)
        context["items"] = payload.get("items", [])
        context["total"] = int(payload.get("total", len(context["items"])))
        context["page"] = int(payload.get("page", page))
        context["page_size"] = int(payload.get("pageSize", page_size))
        viewed = context["page"] * context["page_size"]
        context["has_next"] = context["total"] > viewed
        context["has_prev"] = context["page"] > 1
        context["prev_page"] = max(1, context["page"] - 1)
        context["next_page"] = context["page"] + 1
    except httpx.HTTPStatusError as ex:
        messages.error(request, f"Users lookup failed (HTTP {ex.response.status_code}).")
    except httpx.RequestError:
        messages.error(request, "Unable to reach backend admin users endpoint.")

    return render(request, "dashboard/users.html", context)


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
def operator_audit_security_view(request):
    access_token = request.session.get(SESSION_ACCESS_TOKEN_KEY)
    preset = (request.GET.get("preset") or "").strip()
    now = timezone.now()
    query = {
        "from": request.GET.get("from"),
        "to": request.GET.get("to"),
        "status": request.GET.get("status"),
        "page": request.GET.get("page", 1),
        "pageSize": request.GET.get("pageSize", 25),
    }
    if preset == "login_failures_today":
        query["status"] = "failure"
        query["from"] = now.replace(hour=0, minute=0, second=0, microsecond=0).isoformat()
    elif preset == "login_success_today":
        query["status"] = "success"
        query["from"] = now.replace(hour=0, minute=0, second=0, microsecond=0).isoformat()
    query = {k: v for k, v in query.items() if v not in (None, "")}

    context = {
        "query": query,
        "items": [],
        "page": 1,
        "total": 0,
        "preset": preset,
        "admin_profile": request.session.get(SESSION_ADMIN_PROFILE_KEY),
    }

    try:
        payload = get_security_audit(access_token, query)
        context["items"] = payload.get("items", [])
        context["page"] = payload.get("page", 1)
        context["total"] = payload.get("total", len(context["items"]))
        if request.GET.get("format") == "csv":
            return _to_csv_response(
                context["items"],
                ["event", "status", "actor", "ipAddress", "createdAtUtc"],
                "security_audit.csv",
            )
    except httpx.HTTPStatusError as ex:
        messages.error(request, f"Security audit lookup failed (HTTP {ex.response.status_code}).")
    except httpx.RequestError:
        messages.error(request, "Unable to reach backend security audit endpoint.")

    return render(request, "dashboard/audit_security.html", context)


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
def operator_moderation_logs_view(request):
    access_token = request.session.get(SESSION_ACCESS_TOKEN_KEY)
    preset = (request.GET.get("preset") or "").strip()
    query = {
        "playerId": request.GET.get("playerId"),
        "status": request.GET.get("status"),
        "page": request.GET.get("page", 1),
        "pageSize": request.GET.get("pageSize", 25),
    }
    if preset == "active":
        query["status"] = "1"
    elif preset == "suspended":
        query["status"] = "2"
    elif preset == "banned":
        query["status"] = "3"
    query = {k: v for k, v in query.items() if v not in (None, "")}

    context = {
        "query": query,
        "items": [],
        "page": 1,
        "total": 0,
        "preset": preset,
        "admin_profile": request.session.get(SESSION_ADMIN_PROFILE_KEY),
    }

    try:
        payload = get_moderation_logs(access_token, query)
        context["items"] = payload.get("items", [])
        context["page"] = payload.get("page", 1)
        context["total"] = payload.get("total", len(context["items"]))
        if request.GET.get("format") == "csv":
            return _to_csv_response(
                context["items"],
                ["playerId", "status", "reason", "appliedBy", "createdAtUtc"],
                "moderation_logs.csv",
            )
    except httpx.HTTPStatusError as ex:
        messages.error(request, f"Moderation logs lookup failed (HTTP {ex.response.status_code}).")
    except httpx.RequestError:
        messages.error(request, "Unable to reach backend moderation logs endpoint.")

    return render(request, "dashboard/moderation_logs.html", context)


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
@require_permission("questions:write")
def operator_media_intent_view(request):
    context = {
        "admin_profile": request.session.get(SESSION_ADMIN_PROFILE_KEY),
        "intent": None,
    }

    if request.method == "GET":
        return render(request, "dashboard/media_intent.html", context)

    file_name = request.POST.get("fileName")
    content_type = request.POST.get("contentType")
    size_bytes = request.POST.get("sizeBytes")

    if not file_name or not content_type or not size_bytes:
        messages.error(request, "fileName, contentType, and sizeBytes are required.")
        return render(request, "dashboard/media_intent.html", context, status=422)

    try:
        size_value = int(size_bytes)
    except (TypeError, ValueError):
        messages.error(request, "sizeBytes must be an integer.")
        return render(request, "dashboard/media_intent.html", context, status=422)

    access_token = request.session.get(SESSION_ACCESS_TOKEN_KEY)
    try:
        context["intent"] = create_upload_intent(access_token, file_name, content_type, size_value)
        return render(request, "dashboard/media_intent.html", context)
    except httpx.HTTPStatusError as ex:
        messages.error(request, f"Media intent failed with HTTP {ex.response.status_code}.")
        return render(request, "dashboard/media_intent.html", context, status=502)
    except httpx.RequestError:
        messages.error(request, "Unable to reach backend media intent endpoint.")
        return render(request, "dashboard/media_intent.html", context, status=503)


@operator_login_required
@require_permission("users:read")
def operator_minio_diagnostics(request):
    payload = get_minio_diagnostics()
    status_map = {"healthy": 200, "degraded": 200, "offline": 503}
    return JsonResponse(payload, status=status_map.get(payload.get("overallStatus", "degraded"), 200))


@operator_login_required
@require_permission("users:read")
def minio_diagnostics_view(request):
    diagnostics = get_minio_diagnostics()
    overall_status = diagnostics.get("overallStatus", "degraded")

    guidance_by_status = {
        "healthy": [
            "MinIO is reachable and ready to accept object operations.",
            "If uploads still fail, verify backend API intent generation and client content-type/size.",
        ],
        "degraded": [
            "One or more MinIO probes are degraded; inspect MinIO logs and recent infrastructure changes.",
            "Check bucket IAM policy and available disk capacity before retrying operator workflows.",
        ],
        "offline": [
            "MinIO appears offline. Confirm container/pod is running and service DNS resolves.",
            "Escalate to on-call if recovery exceeds your incident SLO and route uploads to fallback flows.",
        ],
    }

    context = {
        "diagnostics": diagnostics,
        "overall_status": overall_status,
        "guidance": guidance_by_status.get(overall_status, guidance_by_status["degraded"]),
        "admin_profile": request.session.get(SESSION_ADMIN_PROFILE_KEY),
    }
    return render(request, "dashboard/minio_diagnostics.html", context)


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
