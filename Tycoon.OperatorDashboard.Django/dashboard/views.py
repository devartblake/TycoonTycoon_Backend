from functools import wraps
from time import time

import httpx
from django.contrib import messages
from django.http import JsonResponse
from django.shortcuts import redirect, render
from django.utils import timezone

from .services.admin_auth_client import admin_login, admin_me, admin_refresh
from .services.admin_users_client import list_admin_users
from .services.api_clients import get_overall_status, list_service_statuses


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
        return JsonResponse(
            {
                "code": "UPSTREAM_ERROR",
                "message": f"Admin users endpoint failed with HTTP {ex.response.status_code}.",
            },
            status=502,
        )
    except httpx.RequestError:
        return JsonResponse(
            {
                "code": "UPSTREAM_UNAVAILABLE",
                "message": "Unable to reach backend admin users endpoint.",
            },
            status=503,
        )


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
