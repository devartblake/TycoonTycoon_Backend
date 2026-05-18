import csv
import json
import re
import uuid
from functools import wraps
from io import StringIO
from time import time

import httpx
from django.contrib import messages
from django.db import DatabaseError
from django.db.models import Q
from django.http import HttpResponse, JsonResponse
from django.shortcuts import redirect, render
from django.urls import reverse
from django.utils.dateparse import parse_datetime
from django.utils.http import url_has_allowed_host_and_scheme
from django.utils import timezone

from .models import OperatorSavedView, OperatorSavedViewAuditEvent
from .services.admin_audit_client import get_security_audit, get_security_audit_event
from .services.admin_auth_client import AdminAuthConfigurationError, KmsUnavailableError, admin_login, admin_me, admin_refresh
from .services.admin_media_client import create_upload_intent
from .services.admin_moderation_client import get_moderation_log, get_moderation_logs, get_moderation_profile, set_moderation_status
from .services.admin_economy_client import create_economy_transaction, get_economy_history
from .services.admin_questions_client import approve_question, delete_question, get_question, list_questions, reject_question, update_question
from .services.admin_store_client import (
    bulk_reset_stock,
    cancel_flash_sale,
    get_flash_sales,
    get_player_stock,
    get_purchase_analytics,
    get_stock_policies,
    override_player_stock,
)
from .services.admin_game_events_client import (
    close_game_event,
    list_game_events,
    open_game_event,
    start_game_event,
)
from .services.admin_anticheat_client import list_anticheat_flags, review_anticheat_flag
from .services.admin_seasons_client import (
    activate_season,
    close_season,
    get_season_leaderboard,
    list_seasons,
    recompute_season_tiers,
)
from .services.admin_notifications_client import (
    cancel_scheduled,
    create_template,
    delete_template,
    get_dead_letter,
    get_notification_history,
    list_scheduled,
    list_channels,
    list_templates,
    replay_dead_letter,
    send_notification,
    schedule_notification,
    update_template,
    upsert_channel,
)
from .services.admin_personalization_client import (
    get_personalization_archetypes,
    get_personalization_summary,
    get_player_debug,
    get_player_profile,
    get_recommendation_performance,
    list_rules,
    recalculate_player,
    reset_player,
    upsert_rule,
)
from .services.admin_event_queue_client import reprocess_event_queue
from .services.admin_users_client import (
    ban_admin_user,
    get_admin_user,
    get_admin_user_activity,
    list_admin_users,
    unban_admin_user,
    update_admin_user,
)
from .services.api_clients import get_overall_status, list_service_statuses
from .services.charting import (
    archetype_distribution_chart,
    plotly_runtime_script,
    recommendation_performance_chart,
    top_skus_chart,
)
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


def _safe_return_to(request, fallback: str) -> str:
    raw = (request.GET.get("returnTo") or request.POST.get("returnTo") or "").strip()
    if (
        raw.startswith("/")
        and not raw.startswith("//")
        and url_has_allowed_host_and_scheme(raw, allowed_hosts={request.get_host()}, require_https=request.is_secure())
    ):
        return raw
    return fallback


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


def _to_pretty_json(value) -> str:
    return json.dumps(value or {}, indent=2, sort_keys=True)


def _normalize_personalization_audit_rows(rows: list[dict] | None) -> list[dict]:
    normalized = []
    for row in rows or []:
        normalized.append(
            {
                **row,
                "decisionDisplay": row.get("finalDecisionJson") or row.get("finalDecision") or row.get("decision") or "-",
                "candidateDisplay": row.get("candidateType") or row.get("recommendationType") or row.get("type") or "-",
                "allowedDisplay": row.get("allowed") if row.get("allowed") is not None else "-",
            }
        )
    return normalized


def _normalize_user_activity_rows(rows: list[dict] | None) -> list[dict]:
    normalized = []
    for row in rows or []:
        normalized.append(
            {
                **row,
                "typeDisplay": row.get("type") or row.get("eventType") or "-",
                "statusDisplay": row.get("status") or "-",
                "ipDisplay": row.get("ipAddress") or row.get("ip") or "-",
                "createdDisplay": row.get("createdAt") or row.get("createdAtUtc") or row.get("occurredAt") or "-",
                "detailDisplay": row.get("detail") or row.get("message") or row.get("description") or "-",
            }
        )
    return normalized


def _normalize_economy_history_rows(rows: list[dict] | None) -> list[dict]:
    normalized = []
    for row in rows or []:
        normalized.append(
            {
                **row,
                "typeDisplay": row.get("type") or row.get("transactionType") or "-",
                "amountDisplay": row.get("amount") if row.get("amount") is not None else row.get("delta", "-"),
                "reasonDisplay": row.get("reason") or row.get("description") or "-",
                "createdDisplay": row.get("createdAt") or row.get("createdAtUtc") or row.get("occurredAt") or "-",
            }
        )
    return normalized


def _normalize_audit_rows(rows: list[dict] | None) -> list[dict]:
    return [
        {
            **row,
            "eventDisplay": row.get("event") or row.get("title") or "-",
            "createdDisplay": row.get("createdAtUtc") or row.get("createdAt") or "-",
        }
        for row in rows or []
    ]


def _normalize_moderation_rows(rows: list[dict] | None) -> list[dict]:
    return [
        {
            **row,
            "statusDisplay": row.get("status") if row.get("status") is not None else row.get("newStatus", "-"),
            "setByDisplay": row.get("appliedBy") or row.get("setByAdmin") or "-",
        }
        for row in rows or []
    ]


def _detail_section(label: str, permission: str | None = None) -> dict:
    return {
        "label": label,
        "permission": permission,
        "available": True,
        "skipped": False,
        "error": "",
    }


def _load_optional_investigation_section(request, label: str, permission: str, loader):
    section = _detail_section(label, permission)
    if not _has_permission(request, permission):
        section["available"] = False
        section["skipped"] = True
        section["error"] = f"Missing permission: {permission}"
        return None, section

    try:
        return loader(), section
    except httpx.HTTPStatusError as ex:
        section["error"] = f"{label} lookup failed (HTTP {ex.response.status_code})."
    except httpx.RequestError:
        section["error"] = f"Unable to reach backend {label.lower()} endpoint."
    return None, section


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
    except (httpx.HTTPError, AdminAuthConfigurationError, KmsUnavailableError):
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

    healthy_services = sum(1 for service in services if service.status == "healthy")
    degraded_services = sum(1 for service in services if service.status == "degraded")
    offline_services = sum(1 for service in services if service.status == "offline")
    workflow_cards = [
        {
            "label": "Users triage",
            "description": "Filter accounts, open investigation workbenches, and run guarded bulk actions.",
            "href": "operator-users-view",
            "badge": "Triage",
        },
        {
            "label": "Anti-cheat queue",
            "description": "Review flagged sessions and annotate outcomes for audit review.",
            "href": "anticheat-flags-view",
            "badge": "Security",
        },
        {
            "label": "Personalization",
            "description": "Inspect risk signals, archetypes, recommendation performance, and guardrails.",
            "href": "personalization-overview-view",
            "badge": "Risk",
        },
        {
            "label": "Event queue",
            "description": "Reprocess failed or stuck domain events by scope with operator confirmation.",
            "href": "event-queue-view",
            "badge": "Ops",
        },
        {
            "label": "Player stock",
            "description": "Inspect stock usage, set overrides, or bulk reset limited SKU rows.",
            "href": "store-player-stock-view",
            "badge": "Store",
        },
        {
            "label": "Notifications",
            "description": "Send notifications and replay dead-letter failures from one surface.",
            "href": "notifications-view",
            "badge": "Comms",
        },
    ]
    readiness_items = [
        {"label": "Django canonical UI", "status": "Complete", "tone": "ok"},
        {"label": "Migration/seed bootstrap", "status": "Complete", "tone": "ok"},
        {"label": "Operational drilldowns", "status": "Started", "tone": "warn"},
        {"label": "Staging parallel-run", "status": "External gate", "tone": "warn"},
        {"label": "Operator sign-off", "status": "External gate", "tone": "warn"},
        {"label": "Blazor decommission", "status": "After rollback window", "tone": "neutral"},
    ]
    endpoint_surfaces = [
        {"method": "GET", "path": "/api/operator/health", "description": "Aggregated dashboard health payload."},
        {"method": "GET", "path": "/api/operator/users", "description": "Users triage and investigation entrypoint."},
        {"method": "GET", "path": "/api/operator/audit/security", "description": "Security audit timeline and CSV export."},
        {"method": "POST", "path": "/operations/event-queue/reprocess", "description": "Django POST action for event queue recovery."},
    ]

    context = {
        "services": services,
        "overall_status": get_overall_status(services),
        "healthy_services": healthy_services,
        "degraded_services": degraded_services,
        "offline_services": offline_services,
        "operator_route_count": 20,
        "workflow_cards": workflow_cards,
        "readiness_items": readiness_items,
        "endpoint_surfaces": endpoint_surfaces,
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
    governance_query = {
        "from": (request.GET.get("savedViewAuditFrom") or "").strip(),
        "to": (request.GET.get("savedViewAuditTo") or "").strip(),
        "action": (request.GET.get("savedViewAuditAction") or "").strip(),
        "actorEmail": (request.GET.get("savedViewAuditActorEmail") or "").strip().lower(),
        "ownerEmail": (request.GET.get("savedViewAuditOwnerEmail") or "").strip().lower(),
        "viewName": (request.GET.get("savedViewAuditViewName") or "").strip(),
    }
    governance_rows: list[dict] = []

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

    audit_filters = Q()
    if governance_query["action"]:
        audit_filters &= Q(action=governance_query["action"])
    if governance_query["actorEmail"]:
        audit_filters &= Q(actor_email__iexact=governance_query["actorEmail"])
    if governance_query["ownerEmail"]:
        audit_filters &= Q(owner_email__iexact=governance_query["ownerEmail"])
    if governance_query["viewName"]:
        audit_filters &= Q(view_name__icontains=governance_query["viewName"])

    audit_from = parse_datetime(governance_query["from"]) if governance_query["from"] else None
    if audit_from is not None and timezone.is_naive(audit_from):
        audit_from = timezone.make_aware(audit_from, timezone.get_current_timezone())
    if audit_from is not None:
        audit_filters &= Q(created_at__gte=audit_from)

    audit_to = parse_datetime(governance_query["to"]) if governance_query["to"] else None
    if audit_to is not None and timezone.is_naive(audit_to):
        audit_to = timezone.make_aware(audit_to, timezone.get_current_timezone())
    if audit_to is not None:
        audit_filters &= Q(created_at__lte=audit_to)

    try:
        governance_events = OperatorSavedViewAuditEvent.objects.filter(audit_filters).order_by("-created_at")[:100]
        for event in governance_events:
            governance_rows.append(
                {
                    "createdAtUtc": event.created_at.isoformat(),
                    "actorEmail": event.actor_email,
                    "ownerEmail": event.owner_email,
                    "viewName": event.view_name,
                    "action": event.action,
                    "metadata": json.dumps(event.metadata or {}, sort_keys=True),
                }
            )
        if request.GET.get("savedViewAuditFormat") == "csv":
            return _to_csv_response(
                governance_rows,
                ["createdAtUtc", "actorEmail", "ownerEmail", "viewName", "action", "metadata"],
                "saved_view_governance_audit.csv",
            )
    except DatabaseError:
        messages.error(request, "Saved-view governance audit history is unavailable right now.")

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
        "saved_view_audit_query": governance_query,
        "saved_view_audit_rows": governance_rows,
        "user_preset_links": [
            {"href": "?preset=banned_recent", "label": "Banned recent"},
            {"href": "?preset=new_unverified", "label": "New unverified"},
            {"href": "?preset=admins", "label": "Admins"},
        ],
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
def operator_user_detail_view(request, user_id: str):
    access_token = request.session.get(SESSION_ACCESS_TOKEN_KEY)

    if request.method == "POST":
        if not _has_permission(request, "users:write"):
            return JsonResponse({"code": "FORBIDDEN", "message": "Missing required permission: users:write"}, status=403)

        action = (request.POST.get("action") or "").strip().lower()
        try:
            if action == "update":
                username = (request.POST.get("username") or "").strip()
                if not username:
                    messages.error(request, "Username is required.")
                else:
                    update_admin_user(access_token, user_id, {"username": username})
                    messages.success(request, "User profile updated.")
            elif action == "ban":
                reason = (request.POST.get("reason") or "Operator action").strip()
                until = (request.POST.get("until") or "").strip() or None
                ban_admin_user(access_token, user_id, reason, until)
                messages.success(request, "User banned.")
            elif action == "unban":
                unban_admin_user(access_token, user_id)
                messages.success(request, "User unbanned.")
            else:
                messages.error(request, "Unknown user detail action.")
        except httpx.HTTPStatusError as ex:
            messages.error(request, f"User action failed (HTTP {ex.response.status_code}).")
        except httpx.RequestError:
            messages.error(request, "Unable to reach backend user endpoint.")
        return redirect("operator-user-detail-view", user_id=user_id)

    query = {
        "page": request.GET.get("activityPage", 1),
        "pageSize": request.GET.get("activityPageSize", 25),
        "type": request.GET.get("activityType"),
    }
    query = {k: v for k, v in query.items() if v not in (None, "")}
    context = {
        "user_id": user_id,
        "user_detail": None,
        "activity": None,
        "activity_query": query,
        "investigation_return_to": request.get_full_path(),
        "user_detail_json": "{}",
        "can_write_users": _has_permission(request, "users:write"),
        "admin_profile": request.session.get(SESSION_ADMIN_PROFILE_KEY),
    }
    try:
        detail = get_admin_user(access_token, user_id)
        context["user_detail"] = detail
        context["user_detail_json"] = _to_pretty_json(detail)
    except httpx.HTTPStatusError as ex:
        messages.error(request, f"User lookup failed (HTTP {ex.response.status_code}).")
    except httpx.RequestError:
        messages.error(request, "Unable to reach backend user detail endpoint.")

    try:
        context["activity"] = get_admin_user_activity(access_token, user_id, query)
    except httpx.HTTPStatusError as ex:
        messages.error(request, f"User activity lookup failed (HTTP {ex.response.status_code}).")
    except httpx.RequestError:
        messages.error(request, "Unable to reach backend user activity endpoint.")

    return render(request, "dashboard/user_detail.html", context)


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
@require_permission("users:read")
def operator_user_investigation_view(request, user_id: str):
    access_token = request.session.get(SESSION_ACCESS_TOKEN_KEY)
    player_id = (request.GET.get("playerId") or user_id or "").strip()
    user_detail_url = reverse("operator-user-detail-view", kwargs={"user_id": user_id})
    users_url = reverse("operator-users-view")
    return_to = _safe_return_to(request, user_detail_url)
    activity_query = {
        "page": request.GET.get("activityPage", 1),
        "pageSize": request.GET.get("activityPageSize", 25),
    }
    context = {
        "user_id": user_id,
        "player_id": player_id,
        "user_detail": None,
        "user_detail_json": "{}",
        "activity": None,
        "activity_query": activity_query,
        "moderation_profile": None,
        "moderation_profile_json": "{}",
        "economy_history": None,
        "personalization_profile": None,
        "personalization_debug": None,
        "personalization_profile_json": "{}",
        "player_stock": None,
        "sections": {},
        "user_detail_url": user_detail_url,
        "users_url": users_url,
        "return_to": return_to,
        "return_to_query": request.GET.get("returnTo") or return_to,
        "admin_profile": request.session.get(SESSION_ADMIN_PROFILE_KEY),
    }

    try:
        user_detail = get_admin_user(access_token, user_id)
        context["user_detail"] = user_detail
        context["user_detail_json"] = _to_pretty_json(user_detail)
    except httpx.HTTPStatusError as ex:
        context["sections"]["user"] = {
            **_detail_section("User profile", "users:read"),
            "error": f"User profile lookup failed (HTTP {ex.response.status_code}).",
        }
    except httpx.RequestError:
        context["sections"]["user"] = {
            **_detail_section("User profile", "users:read"),
            "error": "Unable to reach backend user profile endpoint.",
        }

    try:
        context["activity"] = get_admin_user_activity(access_token, user_id, activity_query)
        context["activity"]["items"] = _normalize_user_activity_rows(context["activity"].get("items"))
    except httpx.HTTPStatusError as ex:
        context["sections"]["activity"] = {
            **_detail_section("User activity", "users:read"),
            "error": f"User activity lookup failed (HTTP {ex.response.status_code}).",
        }
    except httpx.RequestError:
        context["sections"]["activity"] = {
            **_detail_section("User activity", "users:read"),
            "error": "Unable to reach backend user activity endpoint.",
        }

    moderation_profile, moderation_section = _load_optional_investigation_section(
        request,
        "Moderation profile",
        "moderation:read",
        lambda: get_moderation_profile(access_token, player_id),
    )
    context["moderation_profile"] = moderation_profile
    context["moderation_profile_json"] = _to_pretty_json(moderation_profile)
    context["sections"]["moderation"] = moderation_section

    economy_history, economy_section = _load_optional_investigation_section(
        request,
        "Economy history",
        "economy:read",
        lambda: get_economy_history(access_token, player_id, {"page": 1, "pageSize": 10}),
    )
    context["economy_history"] = economy_history
    if context["economy_history"]:
        context["economy_history"]["items"] = _normalize_economy_history_rows(context["economy_history"].get("items"))
    context["sections"]["economy"] = economy_section

    personalization_profile, personalization_section = _load_optional_investigation_section(
        request,
        "Personalization profile",
        "personalization:read",
        lambda: get_player_profile(access_token, player_id),
    )
    context["personalization_profile"] = personalization_profile
    context["personalization_profile_json"] = _to_pretty_json(personalization_profile)
    context["sections"]["personalization"] = personalization_section

    personalization_debug, personalization_debug_section = _load_optional_investigation_section(
        request,
        "Personalization debug",
        "personalization:read",
        lambda: get_player_debug(access_token, player_id),
    )
    if personalization_debug:
        personalization_debug["recentAudit"] = _normalize_personalization_audit_rows(personalization_debug.get("recentAudit"))
    context["personalization_debug"] = personalization_debug
    context["sections"]["personalization_debug"] = personalization_debug_section

    if _is_valid_uuid(player_id):
        player_stock, stock_section = _load_optional_investigation_section(
            request,
            "Player stock",
            "store:read",
            lambda: get_player_stock(access_token, player_id),
        )
    else:
        player_stock = None
        stock_section = {
            **_detail_section("Player stock", "store:read"),
            "available": False,
            "skipped": True,
            "error": "Player stock requires a player UUID. Pass ?playerId=<uuid> if the user ID is not the player UUID.",
        }
    context["player_stock"] = player_stock
    context["sections"]["stock"] = stock_section

    context["last_refreshed_at"] = timezone.now()
    return render(request, "dashboard/user_investigation.html", context)


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
        "audit_preset_links": [
            {"href": "?preset=login_failures_today", "label": "Login failures today"},
            {"href": "?preset=login_success_today", "label": "Login successes today"},
        ],
    }

    try:
        payload = get_security_audit(access_token, query)
        context["items"] = _normalize_audit_rows(payload.get("items", []))
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
def operator_audit_security_detail_view(request, event_id: str):
    access_token = request.session.get(SESSION_ACCESS_TOKEN_KEY)
    context = {
        "event_id": event_id,
        "event": None,
        "metadata_json": "{}",
        "admin_profile": request.session.get(SESSION_ADMIN_PROFILE_KEY),
    }
    try:
        event = get_security_audit_event(access_token, event_id)
        event["eventDisplay"] = event.get("event") or event.get("title") or "-"
        event["createdDisplay"] = event.get("createdAt") or event.get("createdAtUtc") or "-"
        context["event"] = event
        context["metadata_json"] = _to_pretty_json(event.get("metadata"))
    except httpx.HTTPStatusError as ex:
        messages.error(request, f"Security audit event lookup failed (HTTP {ex.response.status_code}).")
    except httpx.RequestError:
        messages.error(request, "Unable to reach backend security audit endpoint.")
    return render(request, "dashboard/audit_security_detail.html", context)


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
        "moderation_preset_links": [
            {"href": "?preset=active", "label": "Active"},
            {"href": "?preset=suspended", "label": "Suspended"},
            {"href": "?preset=banned", "label": "Banned"},
        ],
    }

    try:
        payload = get_moderation_logs(access_token, query)
        context["items"] = _normalize_moderation_rows(payload.get("items", []))
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
def operator_moderation_player_view(request, player_id: str):
    access_token = request.session.get(SESSION_ACCESS_TOKEN_KEY)

    if request.method == "POST":
        if not _has_permission(request, "events:write"):
            return JsonResponse({"code": "FORBIDDEN", "message": "Missing required permission: events:write"}, status=403)
        admin_profile = request.session.get(SESSION_ADMIN_PROFILE_KEY) or {}
        payload = {
            "playerId": player_id,
            "status": request.POST.get("status"),
            "reason": request.POST.get("reason"),
            "notes": request.POST.get("notes"),
            "expiresAtUtc": request.POST.get("expiresAtUtc"),
            "relatedFlagId": request.POST.get("relatedFlagId"),
        }
        payload = {k: v for k, v in payload.items() if v not in (None, "")}
        if not payload.get("status") or not payload.get("reason"):
            messages.error(request, "Status and reason are required.")
        else:
            try:
                set_moderation_status(access_token, admin_profile.get("email"), payload)
                messages.success(request, "Moderation status updated.")
            except httpx.HTTPStatusError as ex:
                messages.error(request, f"Status update failed (HTTP {ex.response.status_code}).")
            except httpx.RequestError:
                messages.error(request, "Unable to reach backend moderation endpoint.")
        return redirect("operator-moderation-player-view", player_id=player_id)

    context = {
        "player_id": player_id,
        "profile": None,
        "logs": None,
        "can_write_moderation": _has_permission(request, "events:write"),
        "admin_profile": request.session.get(SESSION_ADMIN_PROFILE_KEY),
    }
    try:
        context["profile"] = get_moderation_profile(access_token, player_id)
    except httpx.HTTPStatusError as ex:
        messages.error(request, f"Moderation profile lookup failed (HTTP {ex.response.status_code}).")
    except httpx.RequestError:
        messages.error(request, "Unable to reach backend moderation profile endpoint.")

    try:
        logs = get_moderation_logs(access_token, {"playerId": player_id, "page": 1, "pageSize": 25})
        logs["items"] = _normalize_moderation_rows(logs.get("items", []))
        context["logs"] = logs
    except httpx.HTTPStatusError as ex:
        messages.error(request, f"Moderation logs lookup failed (HTTP {ex.response.status_code}).")
    except httpx.RequestError:
        messages.error(request, "Unable to reach backend moderation logs endpoint.")

    return render(request, "dashboard/moderation_player.html", context)


@operator_login_required
@require_permission("events:read")
def operator_moderation_log_detail_view(request, log_id: str):
    access_token = request.session.get(SESSION_ACCESS_TOKEN_KEY)
    context = {
        "log_id": log_id,
        "log": None,
        "log_json": "{}",
        "admin_profile": request.session.get(SESSION_ADMIN_PROFILE_KEY),
    }
    try:
        log = get_moderation_log(access_token, log_id)
        log["statusDisplay"] = log.get("status") if log.get("status") is not None else log.get("newStatus", "-")
        log["setByDisplay"] = log.get("appliedBy") or log.get("setByAdmin") or "-"
        context["log"] = log
        context["log_json"] = _to_pretty_json(log)
    except httpx.HTTPStatusError as ex:
        messages.error(request, f"Moderation log lookup failed (HTTP {ex.response.status_code}).")
    except httpx.RequestError:
        messages.error(request, "Unable to reach backend moderation log endpoint.")
    return render(request, "dashboard/moderation_log_detail.html", context)


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
        if ex.response.status_code == 400 and "secure_session_required" in ex.response.text:
            messages.error(request, "Backend admin auth requires secure-channel. Configure KMS for Django or enable the trusted BFF auth path.")
            return render(request, "dashboard/login.html", status=502)
        if ex.response.status_code == 503 and "AdminOps key not configured" in ex.response.text:
            messages.error(request, "Backend admin ops key is not configured.")
            return render(request, "dashboard/login.html", status=503)

        messages.error(request, f"Login failed with HTTP {ex.response.status_code}.")
        return render(request, "dashboard/login.html", status=502)
    except AdminAuthConfigurationError as ex:
        messages.error(request, str(ex))
        return render(request, "dashboard/login.html", status=500)
    except KmsUnavailableError:
        messages.error(request, "Unable to complete secure-channel login through KMS.")
        return render(request, "dashboard/login.html", status=503)
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


def _is_valid_uuid(value: str | None) -> bool:
    if not value:
        return False
    try:
        uuid.UUID(str(value))
    except ValueError:
        return False
    return True


def _split_skus(raw: str | None) -> list[str]:
    if not raw:
        return []
    parts = re.split(r"[\s,]+", raw)
    return list(dict.fromkeys(part.strip().lower() for part in parts if part.strip()))


# ---------------------------------------------------------------------------
# Store — Flash Sales
# ---------------------------------------------------------------------------

@operator_login_required
@require_permission("store:read")
def store_flash_sales_view(request):
    access_token = request.session.get(SESSION_ACCESS_TOKEN_KEY)
    context = {
        "sales": [],
        "admin_profile": request.session.get(SESSION_ADMIN_PROFILE_KEY),
    }
    try:
        payload = get_flash_sales(access_token)
        context["sales"] = payload.get("sales", [])
    except httpx.HTTPStatusError as ex:
        messages.error(request, f"Flash sales lookup failed (HTTP {ex.response.status_code}).")
    except httpx.RequestError:
        messages.error(request, "Unable to reach backend flash sales endpoint.")
    return render(request, "dashboard/flash_sales.html", context)


@operator_login_required
@require_permission("store:write")
def store_flash_sale_cancel(request, sale_id: str):
    if request.method != "POST":
        return JsonResponse({"code": "METHOD_NOT_ALLOWED"}, status=405)
    access_token = request.session.get(SESSION_ACCESS_TOKEN_KEY)
    try:
        cancel_flash_sale(access_token, sale_id)
        messages.success(request, f"Flash sale {sale_id} cancelled.")
    except httpx.HTTPStatusError as ex:
        messages.error(request, f"Cancel failed (HTTP {ex.response.status_code}).")
    except httpx.RequestError:
        messages.error(request, "Unable to reach backend to cancel flash sale.")
    return redirect("store-flash-sales-view")


# ---------------------------------------------------------------------------
# Store — Stock Policies
# ---------------------------------------------------------------------------

@operator_login_required
@require_permission("store:read")
def store_stock_policies_view(request):
    access_token = request.session.get(SESSION_ACCESS_TOKEN_KEY)
    params = {
        "activeOnly": request.GET.get("activeOnly"),
        "sku": request.GET.get("sku"),
    }
    context = {
        "policies": [],
        "query": {k: v for k, v in params.items() if v not in (None, "")},
        "admin_profile": request.session.get(SESSION_ADMIN_PROFILE_KEY),
    }
    try:
        payload = get_stock_policies(access_token, params)
        context["policies"] = payload.get("policies", [])
    except httpx.HTTPStatusError as ex:
        messages.error(request, f"Stock policies lookup failed (HTTP {ex.response.status_code}).")
    except httpx.RequestError:
        messages.error(request, "Unable to reach backend stock policies endpoint.")
    return render(request, "dashboard/stock_policies.html", context)


@operator_login_required
@require_permission("store:write")
def store_stock_policies_bulk_reset(request):
    if request.method != "POST":
        return JsonResponse({"code": "METHOD_NOT_ALLOWED"}, status=405)
    access_token = request.session.get(SESSION_ACCESS_TOKEN_KEY)
    skus = _split_skus(request.POST.get("skus"))
    reason = (request.POST.get("reason") or "").strip()
    if not skus:
        messages.error(request, "Enter at least one SKU to bulk reset.")
        return redirect("store-player-stock-view")

    try:
        payload = bulk_reset_stock(access_token, skus, reason)
        players_affected = payload.get("playersAffected", 0)
        messages.success(request, f"Bulk reset queued for {len(skus)} SKU(s); {players_affected} player stock row(s) affected.")
    except httpx.HTTPStatusError as ex:
        messages.error(request, f"Bulk reset failed (HTTP {ex.response.status_code}).")
    except httpx.RequestError:
        messages.error(request, "Unable to reach backend stock reset endpoint.")
    return redirect("store-player-stock-view")


# ---------------------------------------------------------------------------
# Store — Player Stock
# ---------------------------------------------------------------------------

@operator_login_required
@require_permission("store:read")
def store_player_stock_view(request):
    access_token = request.session.get(SESSION_ACCESS_TOKEN_KEY)
    player_id = (request.GET.get("playerId") or "").strip()
    context = {
        "player_id": player_id,
        "items": [],
        "has_lookup": bool(player_id),
        "admin_profile": request.session.get(SESSION_ADMIN_PROFILE_KEY),
    }
    if not player_id:
        return render(request, "dashboard/player_stock.html", context)
    if not _is_valid_uuid(player_id):
        messages.error(request, "Enter a valid player UUID.")
        return render(request, "dashboard/player_stock.html", context)

    try:
        payload = get_player_stock(access_token, player_id)
        context["player_id"] = payload.get("playerId", player_id)
        context["items"] = payload.get("items", [])
    except httpx.HTTPStatusError as ex:
        messages.error(request, f"Player stock lookup failed (HTTP {ex.response.status_code}).")
    except httpx.RequestError:
        messages.error(request, "Unable to reach backend player stock endpoint.")
    return render(request, "dashboard/player_stock.html", context)


@operator_login_required
@require_permission("store:write")
def store_player_stock_override(request, player_id: str):
    if request.method != "POST":
        return JsonResponse({"code": "METHOD_NOT_ALLOWED"}, status=405)
    access_token = request.session.get(SESSION_ACCESS_TOKEN_KEY)
    redirect_url = f"/store/player-stock?playerId={player_id}"
    if not _is_valid_uuid(player_id):
        messages.error(request, "Enter a valid player UUID.")
        return redirect("store-player-stock-view")

    sku = (request.POST.get("sku") or "").strip().lower()
    if not sku:
        messages.error(request, "SKU is required for a player stock override.")
        return redirect(redirect_url)

    raw_effective_max = (request.POST.get("effectiveMaxQuantity") or "").strip()
    effective_max_quantity = None
    if raw_effective_max:
        try:
            effective_max_quantity = int(raw_effective_max)
        except ValueError:
            messages.error(request, "Effective max quantity must be a non-negative whole number, or blank to clear.")
            return redirect(redirect_url)
        if effective_max_quantity < 0:
            messages.error(request, "Effective max quantity must be a non-negative whole number, or blank to clear.")
            return redirect(redirect_url)

    reason = (request.POST.get("reason") or "").strip()
    try:
        override_player_stock(access_token, player_id, sku, effective_max_quantity, reason)
        if effective_max_quantity is None:
            messages.success(request, f"Cleared stock override for {sku}.")
        else:
            messages.success(request, f"Set stock override for {sku} to {effective_max_quantity}.")
    except httpx.HTTPStatusError as ex:
        messages.error(request, f"Stock override failed (HTTP {ex.response.status_code}).")
    except httpx.RequestError:
        messages.error(request, "Unable to reach backend player stock endpoint.")
    return redirect(redirect_url)


# ---------------------------------------------------------------------------
# Store — Purchase Analytics
# ---------------------------------------------------------------------------

@operator_login_required
@require_permission("store:read")
def store_analytics_view(request):
    access_token = request.session.get(SESSION_ACCESS_TOKEN_KEY)
    params = {
        "from": request.GET.get("from"),
        "to": request.GET.get("to"),
        "sku": request.GET.get("sku"),
    }
    context = {
        "analytics": None,
        "top_skus_chart": "",
        "plotly_runtime": "",
        "query": {k: v for k, v in params.items() if v not in (None, "")},
        "admin_profile": request.session.get(SESSION_ADMIN_PROFILE_KEY),
    }
    try:
        context["analytics"] = get_purchase_analytics(access_token, params)
        context["top_skus_chart"] = top_skus_chart(context["analytics"].get("topSkus"))
        if context["top_skus_chart"]:
            context["plotly_runtime"] = plotly_runtime_script()
    except httpx.HTTPStatusError as ex:
        messages.error(request, f"Analytics lookup failed (HTTP {ex.response.status_code}).")
    except httpx.RequestError:
        messages.error(request, "Unable to reach backend analytics endpoint.")
    return render(request, "dashboard/store_analytics.html", context)


# ---------------------------------------------------------------------------
# Questions queue
# ---------------------------------------------------------------------------

@operator_login_required
@require_permission("questions:read")
def questions_queue_view(request):
    access_token = request.session.get(SESSION_ACCESS_TOKEN_KEY)
    params = {
        "q": request.GET.get("q"),
        "status": request.GET.get("status", "Pending"),
        "category": request.GET.get("category"),
        "page": request.GET.get("page", 1),
        "pageSize": request.GET.get("pageSize", 25),
    }
    context = {
        "items": [],
        "page": 1,
        "total": 0,
        "query": {k: v for k, v in params.items() if v not in (None, "")},
        "admin_profile": request.session.get(SESSION_ADMIN_PROFILE_KEY),
    }
    try:
        payload = list_questions(access_token, params)
        context["items"] = payload.get("items", [])
        context["page"] = payload.get("page", 1)
        context["total"] = payload.get("total", 0)
    except httpx.HTTPStatusError as ex:
        messages.error(request, f"Questions lookup failed (HTTP {ex.response.status_code}).")
    except httpx.RequestError:
        messages.error(request, "Unable to reach backend questions endpoint.")
    return render(request, "dashboard/questions_queue.html", context)


@operator_login_required
@require_permission("questions:read")
def question_detail_view(request, question_id: str):
    access_token = request.session.get(SESSION_ACCESS_TOKEN_KEY)

    if request.method == "POST":
        if not _has_permission(request, "questions:write"):
            return JsonResponse({"code": "FORBIDDEN", "message": "Missing required permission: questions:write"}, status=403)
        payload = {
            "text": (request.POST.get("text") or "").strip(),
            "category": (request.POST.get("category") or "").strip(),
            "difficulty": request.POST.get("difficulty") or "1",
            "options": _parse_question_options(request),
            "correctOptionId": (request.POST.get("correctOptionId") or "").strip(),
            "tags": _parse_tags(request.POST.get("tags") or ""),
            "mediaKey": (request.POST.get("mediaKey") or "").strip() or None,
            "status": (request.POST.get("status") or "").strip() or None,
        }
        missing = [k for k in ("text", "category", "correctOptionId") if not payload.get(k)]
        if missing:
            messages.error(request, f"Missing required fields: {', '.join(missing)}.")
        elif len(payload["options"]) < 2:
            messages.error(request, "At least two options are required.")
        else:
            try:
                update_question(access_token, question_id, payload)
                messages.success(request, "Question updated.")
            except httpx.HTTPStatusError as ex:
                messages.error(request, f"Question update failed (HTTP {ex.response.status_code}).")
            except httpx.RequestError:
                messages.error(request, "Unable to reach backend question endpoint.")
        return redirect("question-detail-view", question_id=question_id)

    context = {
        "question_id": question_id,
        "question": None,
        "question_json": "{}",
        "can_write_questions": _has_permission(request, "questions:write"),
        "admin_profile": request.session.get(SESSION_ADMIN_PROFILE_KEY),
    }
    try:
        question = get_question(access_token, question_id)
        context["question"] = question
        context["question_json"] = _to_pretty_json(question)
    except httpx.HTTPStatusError as ex:
        messages.error(request, f"Question lookup failed (HTTP {ex.response.status_code}).")
    except httpx.RequestError:
        messages.error(request, "Unable to reach backend question endpoint.")
    return render(request, "dashboard/question_detail.html", context)


@operator_login_required
@require_permission("questions:write")
def question_delete(request, question_id: str):
    if request.method != "POST":
        return JsonResponse({"code": "METHOD_NOT_ALLOWED"}, status=405)
    access_token = request.session.get(SESSION_ACCESS_TOKEN_KEY)
    try:
        delete_question(access_token, question_id)
        messages.success(request, f"Question {question_id} deleted.")
        return redirect("questions-queue-view")
    except httpx.HTTPStatusError as ex:
        messages.error(request, f"Delete failed (HTTP {ex.response.status_code}).")
    except httpx.RequestError:
        messages.error(request, "Unable to reach backend to delete question.")
    return redirect("question-detail-view", question_id=question_id)


@operator_login_required
@require_permission("questions:write")
def questions_approve(request, question_id: str):
    if request.method != "POST":
        return JsonResponse({"code": "METHOD_NOT_ALLOWED"}, status=405)
    access_token = request.session.get(SESSION_ACCESS_TOKEN_KEY)
    try:
        approve_question(access_token, question_id)
        messages.success(request, f"Question {question_id} approved.")
    except httpx.HTTPStatusError as ex:
        messages.error(request, f"Approve failed (HTTP {ex.response.status_code}).")
    except httpx.RequestError:
        messages.error(request, "Unable to reach backend to approve question.")
    return redirect(request.META.get("HTTP_REFERER", "questions-queue-view"))


@operator_login_required
@require_permission("questions:write")
def questions_reject(request, question_id: str):
    if request.method != "POST":
        return JsonResponse({"code": "METHOD_NOT_ALLOWED"}, status=405)
    access_token = request.session.get(SESSION_ACCESS_TOKEN_KEY)
    try:
        reject_question(access_token, question_id)
        messages.success(request, f"Question {question_id} rejected.")
    except httpx.HTTPStatusError as ex:
        messages.error(request, f"Reject failed (HTTP {ex.response.status_code}).")
    except httpx.RequestError:
        messages.error(request, "Unable to reach backend to reject question.")
    return redirect(request.META.get("HTTP_REFERER", "questions-queue-view"))


# ---------------------------------------------------------------------------
# Economy — player history + coin grant
# ---------------------------------------------------------------------------

@operator_login_required
@require_permission("economy:read")
def economy_player_view(request):
    access_token = request.session.get(SESSION_ACCESS_TOKEN_KEY)
    player_id = (request.GET.get("playerId") or "").strip()
    params = {
        "page": request.GET.get("page", 1),
        "pageSize": request.GET.get("pageSize", 50),
    }
    context = {
        "player_id": player_id,
        "history": None,
        "query": params,
        "admin_profile": request.session.get(SESSION_ADMIN_PROFILE_KEY),
    }
    if player_id:
        try:
            context["history"] = get_economy_history(access_token, player_id, params)
        except httpx.HTTPStatusError as ex:
            messages.error(request, f"History lookup failed (HTTP {ex.response.status_code}).")
        except httpx.RequestError:
            messages.error(request, "Unable to reach backend economy endpoint.")
    return render(request, "dashboard/economy_player.html", context)


@operator_login_required
@require_permission("economy:write")
def economy_grant(request):
    if request.method != "POST":
        return JsonResponse({"code": "METHOD_NOT_ALLOWED"}, status=405)
    access_token = request.session.get(SESSION_ACCESS_TOKEN_KEY)
    payload = {
        "playerId": request.POST.get("playerId"),
        "amount": request.POST.get("amount"),
        "type": request.POST.get("type", "grant"),
        "reason": request.POST.get("reason"),
    }
    missing = [k for k, v in payload.items() if not v]
    if missing:
        messages.error(request, f"Missing required fields: {', '.join(missing)}.")
        return redirect(f"economy-player-view?playerId={payload.get('playerId', '')}")
    try:
        create_economy_transaction(access_token, payload)
        messages.success(request, f"Transaction applied for player {payload['playerId']}.")
    except httpx.HTTPStatusError as ex:
        messages.error(request, f"Transaction failed (HTTP {ex.response.status_code}).")
    except httpx.RequestError:
        messages.error(request, "Unable to reach backend economy endpoint.")
    return redirect(f"/economy/player?playerId={payload.get('playerId', '')}")


# ---------------------------------------------------------------------------
# Game Events
# ---------------------------------------------------------------------------

@operator_login_required
@require_permission("events:read")
def game_events_view(request):
    access_token = request.session.get(SESSION_ACCESS_TOKEN_KEY)
    query = {
        "status": request.GET.get("status"),
        "page": request.GET.get("page", 1),
        "pageSize": request.GET.get("pageSize", 20),
    }
    query = {k: v for k, v in query.items() if v not in (None, "")}
    context = {
        "events": None,
        "query": query,
        "admin_profile": request.session.get(SESSION_ADMIN_PROFILE_KEY),
    }
    try:
        context["events"] = list_game_events(access_token, query)
    except httpx.HTTPStatusError as ex:
        messages.error(request, f"Failed to load game events (HTTP {ex.response.status_code}).")
    except httpx.RequestError:
        messages.error(request, "Unable to reach backend game events endpoint.")
    return render(request, "dashboard/game_events.html", context)


@operator_login_required
@require_permission("events:write")
def game_event_open(request, event_id: str):
    if request.method != "POST":
        return JsonResponse({"code": "METHOD_NOT_ALLOWED"}, status=405)
    access_token = request.session.get(SESSION_ACCESS_TOKEN_KEY)
    try:
        open_game_event(access_token, event_id)
        messages.success(request, f"Event {event_id} opened.")
    except httpx.HTTPStatusError as ex:
        messages.error(request, f"Open failed (HTTP {ex.response.status_code}).")
    except httpx.RequestError:
        messages.error(request, "Unable to reach backend.")
    return redirect("/events/game-events")


@operator_login_required
@require_permission("events:write")
def game_event_start(request, event_id: str):
    if request.method != "POST":
        return JsonResponse({"code": "METHOD_NOT_ALLOWED"}, status=405)
    access_token = request.session.get(SESSION_ACCESS_TOKEN_KEY)
    try:
        start_game_event(access_token, event_id)
        messages.success(request, f"Event {event_id} started.")
    except httpx.HTTPStatusError as ex:
        messages.error(request, f"Start failed (HTTP {ex.response.status_code}).")
    except httpx.RequestError:
        messages.error(request, "Unable to reach backend.")
    return redirect("/events/game-events")


@operator_login_required
@require_permission("events:write")
def game_event_close(request, event_id: str):
    if request.method != "POST":
        return JsonResponse({"code": "METHOD_NOT_ALLOWED"}, status=405)
    access_token = request.session.get(SESSION_ACCESS_TOKEN_KEY)
    try:
        close_game_event(access_token, event_id)
        messages.success(request, f"Event {event_id} closed and prizes distributed.")
    except httpx.HTTPStatusError as ex:
        messages.error(request, f"Close failed (HTTP {ex.response.status_code}).")
    except httpx.RequestError:
        messages.error(request, "Unable to reach backend.")
    return redirect("/events/game-events")


# ---------------------------------------------------------------------------
# Anti-Cheat
# ---------------------------------------------------------------------------

@operator_login_required
@require_permission("anticheat:read")
def anticheat_flags_view(request):
    access_token = request.session.get(SESSION_ACCESS_TOKEN_KEY)
    query = {
        "severity": request.GET.get("severity"),
        "playerId": request.GET.get("playerId"),
        "unreviewedOnly": request.GET.get("unreviewedOnly"),
        "page": request.GET.get("page", 1),
        "pageSize": request.GET.get("pageSize", 25),
    }
    query = {k: v for k, v in query.items() if v not in (None, "")}
    context = {
        "flags": None,
        "query": query,
        "admin_profile": request.session.get(SESSION_ADMIN_PROFILE_KEY),
    }
    try:
        context["flags"] = list_anticheat_flags(access_token, query)
    except httpx.HTTPStatusError as ex:
        messages.error(request, f"Failed to load flags (HTTP {ex.response.status_code}).")
    except httpx.RequestError:
        messages.error(request, "Unable to reach backend anti-cheat endpoint.")
    return render(request, "dashboard/anticheat_flags.html", context)


@operator_login_required
@require_permission("anticheat:write")
def anticheat_flag_review(request, flag_id: str):
    if request.method != "POST":
        return JsonResponse({"code": "METHOD_NOT_ALLOWED"}, status=405)
    access_token = request.session.get(SESSION_ACCESS_TOKEN_KEY)
    profile = request.session.get(SESSION_ADMIN_PROFILE_KEY) or {}
    reviewed_by = profile.get("email") or "operator"
    note = request.POST.get("note", "").strip()
    try:
        review_anticheat_flag(access_token, flag_id, reviewed_by, note)
        messages.success(request, f"Flag {flag_id} marked as reviewed.")
    except httpx.HTTPStatusError as ex:
        messages.error(request, f"Review failed (HTTP {ex.response.status_code}).")
    except httpx.RequestError:
        messages.error(request, "Unable to reach backend.")
    next_url = request.POST.get("next") or "/security/anticheat"
    return redirect(next_url)


# ---------------------------------------------------------------------------
# Seasons
# ---------------------------------------------------------------------------

@operator_login_required
@require_permission("seasons:read")
def seasons_view(request):
    access_token = request.session.get(SESSION_ACCESS_TOKEN_KEY)
    season_id = request.GET.get("seasonId")
    context = {
        "seasons": None,
        "leaderboard": None,
        "season_id": season_id,
        "admin_profile": request.session.get(SESSION_ADMIN_PROFILE_KEY),
    }
    try:
        context["seasons"] = list_seasons(access_token, {"page": request.GET.get("page", 1), "pageSize": 50})
    except httpx.HTTPStatusError as ex:
        messages.error(request, f"Failed to load seasons (HTTP {ex.response.status_code}).")
    except httpx.RequestError:
        messages.error(request, "Unable to reach backend seasons endpoint.")
    return render(request, "dashboard/seasons.html", context)


@operator_login_required
@require_permission("seasons:read")
def seasons_leaderboard(request, season_id: str):
    access_token = request.session.get(SESSION_ACCESS_TOKEN_KEY)
    context = {
        "seasons": None,
        "leaderboard": None,
        "season_id": season_id,
        "admin_profile": request.session.get(SESSION_ADMIN_PROFILE_KEY),
    }
    try:
        context["seasons"] = list_seasons(access_token, {"page": 1, "pageSize": 50})
        context["leaderboard"] = get_season_leaderboard(
            access_token, season_id,
            {"page": request.GET.get("page", 1), "pageSize": request.GET.get("pageSize", 50)},
        )
    except httpx.HTTPStatusError as ex:
        messages.error(request, f"Failed to load leaderboard (HTTP {ex.response.status_code}).")
    except httpx.RequestError:
        messages.error(request, "Unable to reach backend seasons endpoint.")
    return render(request, "dashboard/seasons.html", context)


@operator_login_required
@require_permission("seasons:write")
def seasons_activate(request):
    if request.method != "POST":
        return JsonResponse({"code": "METHOD_NOT_ALLOWED"}, status=405)
    access_token = request.session.get(SESSION_ACCESS_TOKEN_KEY)
    season_id = request.POST.get("seasonId", "").strip()
    try:
        activate_season(access_token, season_id)
        messages.success(request, f"Season {season_id} activated.")
    except httpx.HTTPStatusError as ex:
        messages.error(request, f"Activate failed (HTTP {ex.response.status_code}).")
    except httpx.RequestError:
        messages.error(request, "Unable to reach backend.")
    return redirect("/operations/seasons")


@operator_login_required
@require_permission("seasons:write")
def seasons_close(request):
    if request.method != "POST":
        return JsonResponse({"code": "METHOD_NOT_ALLOWED"}, status=405)
    access_token = request.session.get(SESSION_ACCESS_TOKEN_KEY)
    season_id = request.POST.get("seasonId", "").strip()
    try:
        close_season(access_token, season_id)
        messages.success(request, f"Season {season_id} closed.")
    except httpx.HTTPStatusError as ex:
        messages.error(request, f"Close failed (HTTP {ex.response.status_code}).")
    except httpx.RequestError:
        messages.error(request, "Unable to reach backend.")
    return redirect("/operations/seasons")


@operator_login_required
@require_permission("seasons:write")
def seasons_recompute(request, season_id: str):
    if request.method != "POST":
        return JsonResponse({"code": "METHOD_NOT_ALLOWED"}, status=405)
    access_token = request.session.get(SESSION_ACCESS_TOKEN_KEY)
    try:
        recompute_season_tiers(access_token, season_id)
        messages.success(request, f"Tier recompute triggered for season {season_id}.")
    except httpx.HTTPStatusError as ex:
        messages.error(request, f"Recompute failed (HTTP {ex.response.status_code}).")
    except httpx.RequestError:
        messages.error(request, "Unable to reach backend.")
    return redirect("/operations/seasons")


# ---------------------------------------------------------------------------
# Notifications
# ---------------------------------------------------------------------------

@operator_login_required
@require_permission("notifications:read")
def notifications_view(request):
    access_token = request.session.get(SESSION_ACCESS_TOKEN_KEY)
    history_query = {
        "from": request.GET.get("from"),
        "to": request.GET.get("to"),
        "channelKey": request.GET.get("channelKey"),
        "status": request.GET.get("status"),
        "page": request.GET.get("page", 1),
        "pageSize": request.GET.get("pageSize", 25),
    }
    context = {
        "channels": [],
        "scheduled": None,
        "templates": [],
        "history": None,
        "dead_letter": None,
        "history_query": history_query,
        "default_audience_json": '{\n  "type": "all"\n}',
        "default_payload_json": "{\n}",
        "default_repeat_json": "",
        "last_refreshed": timezone.now(),
        "admin_profile": request.session.get(SESSION_ADMIN_PROFILE_KEY),
    }
    try:
        context["channels"] = list_channels(access_token)
    except (httpx.HTTPStatusError, httpx.RequestError):
        messages.error(request, "Failed to load notification channels.")
    try:
        context["scheduled"] = list_scheduled(access_token, {"page": 1, "pageSize": 25})
    except (httpx.HTTPStatusError, httpx.RequestError):
        messages.error(request, "Failed to load scheduled notifications.")
    try:
        context["templates"] = list_templates(access_token)
    except (httpx.HTTPStatusError, httpx.RequestError):
        messages.error(request, "Failed to load notification templates.")
    try:
        context["history"] = get_notification_history(access_token, history_query)
    except (httpx.HTTPStatusError, httpx.RequestError):
        messages.error(request, "Failed to load notification history.")
    try:
        context["dead_letter"] = get_dead_letter(access_token, {"page": 1, "pageSize": 25})
    except (httpx.HTTPStatusError, httpx.RequestError):
        messages.error(request, "Failed to load notification dead-letter queue.")
    return render(request, "dashboard/notifications.html", context)


def _parse_json_object_field(request, field_name: str, label: str, *, required: bool) -> tuple[dict | None, bool]:
    raw = (request.POST.get(field_name) or "").strip()
    if not raw:
        if required:
            messages.error(request, f"{label} is required and must be a JSON object.")
            return None, False
        return None, True
    try:
        parsed = json.loads(raw)
    except json.JSONDecodeError:
        messages.error(request, f"{label} must be valid JSON. No backend call was sent.")
        return None, False
    if not isinstance(parsed, dict):
        messages.error(request, f"{label} must be a JSON object. No backend call was sent.")
        return None, False
    return parsed, True


def _parse_variables(raw: str) -> list[str]:
    return [part.strip() for part in re.split(r"[\n,]+", raw or "") if part.strip()]


def _parse_tags(raw: str) -> list[str]:
    return [part.strip() for part in re.split(r"[\n,]+", raw or "") if part.strip()]


def _parse_question_options(request) -> list[dict]:
    options = []
    for idx in range(1, 5):
        option_id = (request.POST.get(f"option{idx}Id") or chr(64 + idx)).strip()
        option_text = (request.POST.get(f"option{idx}Text") or "").strip()
        if option_id or option_text:
            options.append({"id": option_id, "text": option_text})
    return options


@operator_login_required
@require_permission("notifications:write")
def notifications_send(request):
    if request.method != "POST":
        return JsonResponse({"code": "METHOD_NOT_ALLOWED"}, status=405)
    access_token = request.session.get(SESSION_ACCESS_TOKEN_KEY)
    audience, audience_ok = _parse_json_object_field(request, "audience", "Audience JSON", required=True)
    payload_json, payload_ok = _parse_json_object_field(request, "payload", "Payload JSON", required=False)
    if not audience_ok or not payload_ok:
        return redirect("/operations/notifications")
    payload = {
        "channelKey": (request.POST.get("channelKey") or "").strip(),
        "title": (request.POST.get("title") or "").strip(),
        "body": (request.POST.get("body") or "").strip(),
        "audience": audience,
        "payload": payload_json,
    }
    missing = [k for k in ("channelKey", "title", "body") if not payload.get(k)]
    if missing:
        messages.error(request, f"Missing required fields: {', '.join(missing)}.")
        return redirect("/operations/notifications")
    try:
        send_notification(access_token, payload)
        messages.success(request, f"Notification queued on channel '{payload['channelKey']}'.")
    except httpx.HTTPStatusError as ex:
        messages.error(request, f"Send failed (HTTP {ex.response.status_code}).")
    except httpx.RequestError:
        messages.error(request, "Unable to reach backend notifications endpoint.")
    return redirect("/operations/notifications")


@operator_login_required
@require_permission("notifications:write")
def notifications_schedule(request):
    if request.method != "POST":
        return JsonResponse({"code": "METHOD_NOT_ALLOWED"}, status=405)
    access_token = request.session.get(SESSION_ACCESS_TOKEN_KEY)
    audience, audience_ok = _parse_json_object_field(request, "audience", "Audience JSON", required=True)
    repeat, repeat_ok = _parse_json_object_field(request, "repeat", "Repeat JSON", required=False)
    if not audience_ok or not repeat_ok:
        return redirect("/operations/notifications")
    payload = {
        "channelKey": (request.POST.get("channelKey") or "").strip(),
        "title": (request.POST.get("title") or "").strip(),
        "body": (request.POST.get("body") or "").strip(),
        "scheduledAt": (request.POST.get("scheduledAt") or "").strip(),
        "audience": audience,
        "repeat": repeat,
    }
    missing = [k for k in ("channelKey", "title", "body", "scheduledAt") if not payload.get(k)]
    if missing:
        messages.error(request, f"Missing required fields: {', '.join(missing)}.")
        return redirect("/operations/notifications")
    try:
        schedule_notification(access_token, payload)
        messages.success(request, f"Notification scheduled on channel '{payload['channelKey']}'.")
    except httpx.HTTPStatusError as ex:
        messages.error(request, f"Schedule failed (HTTP {ex.response.status_code}).")
    except httpx.RequestError:
        messages.error(request, "Unable to reach backend notifications endpoint.")
    return redirect("/operations/notifications")


@operator_login_required
@require_permission("notifications:write")
def notifications_scheduled_cancel(request, schedule_id: str):
    if request.method != "POST":
        return JsonResponse({"code": "METHOD_NOT_ALLOWED"}, status=405)
    access_token = request.session.get(SESSION_ACCESS_TOKEN_KEY)
    try:
        cancel_scheduled(access_token, schedule_id)
        messages.success(request, f"Schedule {schedule_id} cancelled.")
    except httpx.HTTPStatusError as ex:
        messages.error(request, f"Cancel failed (HTTP {ex.response.status_code}).")
    except httpx.RequestError:
        messages.error(request, "Unable to reach backend.")
    return redirect("/operations/notifications")


@operator_login_required
@require_permission("notifications:write")
def notifications_channel_upsert(request):
    if request.method != "POST":
        return JsonResponse({"code": "METHOD_NOT_ALLOWED"}, status=405)
    access_token = request.session.get(SESSION_ACCESS_TOKEN_KEY)
    key = (request.POST.get("key") or "").strip()
    payload = {
        "name": (request.POST.get("name") or "").strip(),
        "description": (request.POST.get("description") or "").strip(),
        "importance": (request.POST.get("importance") or "").strip() or "normal",
        "enabled": request.POST.get("enabled") == "on",
    }
    if not key or not payload["name"]:
        messages.error(request, "Channel key and name are required.")
        return redirect("/operations/notifications")
    try:
        upsert_channel(access_token, key, payload)
        messages.success(request, f"Channel '{key}' saved.")
    except httpx.HTTPStatusError as ex:
        messages.error(request, f"Channel save failed (HTTP {ex.response.status_code}).")
    except httpx.RequestError:
        messages.error(request, "Unable to reach backend.")
    return redirect("/operations/notifications")


@operator_login_required
@require_permission("notifications:write")
def notifications_template_create(request):
    if request.method != "POST":
        return JsonResponse({"code": "METHOD_NOT_ALLOWED"}, status=405)
    return _save_notification_template(request)


@operator_login_required
@require_permission("notifications:write")
def notifications_template_update(request, template_id: str):
    if request.method != "POST":
        return JsonResponse({"code": "METHOD_NOT_ALLOWED"}, status=405)
    return _save_notification_template(request, template_id)


def _save_notification_template(request, template_id: str | None = None):
    access_token = request.session.get(SESSION_ACCESS_TOKEN_KEY)
    payload = {
        "name": (request.POST.get("name") or "").strip(),
        "title": (request.POST.get("title") or "").strip(),
        "body": (request.POST.get("body") or "").strip(),
        "channelKey": (request.POST.get("channelKey") or "").strip(),
        "variables": _parse_variables(request.POST.get("variables") or ""),
    }
    missing = [k for k in ("name", "title", "body", "channelKey") if not payload.get(k)]
    if missing:
        messages.error(request, f"Missing required template fields: {', '.join(missing)}.")
        return redirect("/operations/notifications")
    try:
        if template_id:
            update_template(access_token, template_id, payload)
            messages.success(request, f"Template '{payload['name']}' updated.")
        else:
            create_template(access_token, payload)
            messages.success(request, f"Template '{payload['name']}' created.")
    except httpx.HTTPStatusError as ex:
        messages.error(request, f"Template save failed (HTTP {ex.response.status_code}).")
    except httpx.RequestError:
        messages.error(request, "Unable to reach backend.")
    return redirect("/operations/notifications")


@operator_login_required
@require_permission("notifications:write")
def notifications_template_delete(request, template_id: str):
    if request.method != "POST":
        return JsonResponse({"code": "METHOD_NOT_ALLOWED"}, status=405)
    access_token = request.session.get(SESSION_ACCESS_TOKEN_KEY)
    try:
        delete_template(access_token, template_id)
        messages.success(request, f"Template {template_id} deleted.")
    except httpx.HTTPStatusError as ex:
        messages.error(request, f"Template delete failed (HTTP {ex.response.status_code}).")
    except httpx.RequestError:
        messages.error(request, "Unable to reach backend.")
    return redirect("/operations/notifications")


@operator_login_required
@require_permission("notifications:write")
def notifications_dead_letter_replay(request, schedule_id: str):
    if request.method != "POST":
        return JsonResponse({"code": "METHOD_NOT_ALLOWED"}, status=405)
    access_token = request.session.get(SESSION_ACCESS_TOKEN_KEY)
    try:
        replay_dead_letter(access_token, schedule_id)
        messages.success(request, f"Schedule {schedule_id} re-queued.")
    except httpx.HTTPStatusError as ex:
        messages.error(request, f"Replay failed (HTTP {ex.response.status_code}).")
    except httpx.RequestError:
        messages.error(request, "Unable to reach backend.")
    return redirect("/operations/notifications")


# ---------------------------------------------------------------------------
# Event Queue
# ---------------------------------------------------------------------------

@operator_login_required
@require_permission("eventqueue:read")
def event_queue_view(request):
    return render(request, "dashboard/event_queue.html", {
        "job_result": None,
        "admin_profile": request.session.get(SESSION_ADMIN_PROFILE_KEY),
    })


@operator_login_required
@require_permission("eventqueue:write")
def event_queue_reprocess(request):
    if request.method != "POST":
        return JsonResponse({"code": "METHOD_NOT_ALLOWED"}, status=405)
    access_token = request.session.get(SESSION_ACCESS_TOKEN_KEY)
    scope = request.POST.get("scope", "").strip()
    limit_raw = request.POST.get("limit", "100")
    try:
        limit = int(limit_raw)
    except ValueError:
        limit = 100
    if not scope:
        messages.error(request, "Scope is required.")
        return redirect("/operations/event-queue")
    try:
        result = reprocess_event_queue(access_token, scope, limit)
        messages.success(request, f"Reprocess job queued: {result.get('jobId', '?')} ({result.get('status', '?')}).")
        return render(request, "dashboard/event_queue.html", {
            "job_result": result,
            "admin_profile": request.session.get(SESSION_ADMIN_PROFILE_KEY),
        })
    except httpx.HTTPStatusError as ex:
        messages.error(request, f"Reprocess failed (HTTP {ex.response.status_code}).")
    except httpx.RequestError:
        messages.error(request, "Unable to reach backend event queue endpoint.")
    return redirect("/operations/event-queue")


# ---------------------------------------------------------------------------
# Personalization
# ---------------------------------------------------------------------------

@operator_login_required
@require_permission("personalization:read")
def personalization_overview_view(request):
    access_token = request.session.get(SESSION_ACCESS_TOKEN_KEY)
    context = {
        "summary": None,
        "archetypes": [],
        "performance": [],
        "archetypes_chart": "",
        "performance_chart": "",
        "plotly_runtime": "",
        "admin_profile": request.session.get(SESSION_ADMIN_PROFILE_KEY),
    }
    try:
        context["summary"] = get_personalization_summary(access_token)
        context["archetypes"] = get_personalization_archetypes(access_token)
        context["performance"] = get_recommendation_performance(access_token)
        context["archetypes_chart"] = archetype_distribution_chart(context["archetypes"])
        context["performance_chart"] = recommendation_performance_chart(context["performance"])
        if context["archetypes_chart"] or context["performance_chart"]:
            context["plotly_runtime"] = plotly_runtime_script()
    except httpx.HTTPStatusError as ex:
        messages.error(request, f"Personalization overview failed (HTTP {ex.response.status_code}).")
    except httpx.RequestError:
        messages.error(request, "Unable to reach backend personalization endpoints.")
    return render(request, "dashboard/personalization_overview.html", context)


@operator_login_required
@require_permission("personalization:read")
def personalization_player_view(request):
    access_token = request.session.get(SESSION_ACCESS_TOKEN_KEY)
    player_id = (request.GET.get("playerId") or "").strip()
    context = {
        "player_id": player_id,
        "profile": None,
        "debug": None,
        "profile_json": "{}",
        "preferences_json": "{}",
        "guardrails_json": "{}",
        "admin_profile": request.session.get(SESSION_ADMIN_PROFILE_KEY),
    }
    if not player_id:
        messages.warning(request, "Enter a player ID to load personalization debug details.")
        return render(request, "dashboard/personalization_player.html", context)

    try:
        profile = get_player_profile(access_token, player_id)
        debug = get_player_debug(access_token, player_id)
        debug["recentAudit"] = _normalize_personalization_audit_rows(debug.get("recentAudit"))
        context["profile"] = profile
        context["debug"] = debug
        context["profile_json"] = _to_pretty_json(profile)
        context["preferences_json"] = _to_pretty_json(profile.get("preferences"))
        context["guardrails_json"] = _to_pretty_json(profile.get("guardrails"))
    except httpx.HTTPStatusError as ex:
        messages.error(request, f"Player personalization lookup failed (HTTP {ex.response.status_code}).")
    except httpx.RequestError:
        messages.error(request, "Unable to reach backend personalization endpoints.")
    return render(request, "dashboard/personalization_player.html", context)


@operator_login_required
@require_permission("personalization:write")
def personalization_player_recalculate(request, player_id: str):
    if request.method != "POST":
        return JsonResponse({"code": "METHOD_NOT_ALLOWED"}, status=405)
    access_token = request.session.get(SESSION_ACCESS_TOKEN_KEY)
    try:
        recalculate_player(access_token, player_id)
        messages.success(request, f"Recalculation triggered for player {player_id}.")
    except httpx.HTTPStatusError as ex:
        messages.error(request, f"Recalculate failed (HTTP {ex.response.status_code}).")
    except httpx.RequestError:
        messages.error(request, "Unable to reach backend personalization endpoint.")
    return redirect(f"/personalization/player?playerId={player_id}")


@operator_login_required
@require_permission("personalization:write")
def personalization_player_reset(request, player_id: str):
    if request.method != "POST":
        return JsonResponse({"code": "METHOD_NOT_ALLOWED"}, status=405)
    access_token = request.session.get(SESSION_ACCESS_TOKEN_KEY)
    try:
        reset_player(access_token, player_id)
        messages.success(request, f"Personalization reset to safe defaults for player {player_id}.")
    except httpx.HTTPStatusError as ex:
        messages.error(request, f"Reset failed (HTTP {ex.response.status_code}).")
    except httpx.RequestError:
        messages.error(request, "Unable to reach backend personalization endpoint.")
    return redirect(f"/personalization/player?playerId={player_id}")


@operator_login_required
@require_permission("personalization:read")
def personalization_rules_view(request):
    access_token = request.session.get(SESSION_ACCESS_TOKEN_KEY)
    context = {
        "rules": [],
        "admin_profile": request.session.get(SESSION_ADMIN_PROFILE_KEY),
    }
    try:
        rules = list_rules(access_token)
        context["rules"] = [
            {
                **rule,
                "ruleJson": _to_pretty_json(rule.get("rule")),
            }
            for rule in rules
        ]
    except httpx.HTTPStatusError as ex:
        messages.error(request, f"Guardrail rules lookup failed (HTTP {ex.response.status_code}).")
    except httpx.RequestError:
        messages.error(request, "Unable to reach backend personalization rules endpoint.")
    return render(request, "dashboard/personalization_rules.html", context)


@operator_login_required
@require_permission("personalization:write")
def personalization_rule_upsert(request, rule_key: str):
    if request.method != "POST":
        return JsonResponse({"code": "METHOD_NOT_ALLOWED"}, status=405)

    access_token = request.session.get(SESSION_ACCESS_TOKEN_KEY)
    raw_rule = (request.POST.get("ruleJson") or "{}").strip() or "{}"
    is_enabled = str(request.POST.get("isEnabled") or "").lower() in {"1", "true", "on", "yes"}

    try:
        parsed_rule = json.loads(raw_rule)
    except json.JSONDecodeError:
        messages.error(request, f"Rule JSON for '{rule_key}' is invalid. No update was sent.")
        return redirect("/personalization/rules")

    if not isinstance(parsed_rule, dict):
        messages.error(request, f"Rule JSON for '{rule_key}' must be a JSON object. No update was sent.")
        return redirect("/personalization/rules")

    try:
        upsert_rule(access_token, rule_key, {"isEnabled": is_enabled, "rule": parsed_rule})
        messages.success(request, f"Updated guardrail rule '{rule_key}'.")
    except httpx.HTTPStatusError as ex:
        messages.error(request, f"Rule update failed (HTTP {ex.response.status_code}).")
    except httpx.RequestError:
        messages.error(request, "Unable to reach backend personalization rules endpoint.")
    return redirect("/personalization/rules")
