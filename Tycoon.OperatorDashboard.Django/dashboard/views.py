from django.http import JsonResponse
from django.shortcuts import render

from .services.api_clients import get_dotnet_status, get_fastapi_status


STATUS_CLASSES = {
    "healthy": "status-ok",
    "degraded": "status-warn",
    "offline": "status-bad",
}


def dashboard_home(request):
    services = [get_dotnet_status(), get_fastapi_status()]

    for service in services:
        service.css_class = STATUS_CLASSES.get(service.status, "status-unknown")

    return render(request, "dashboard/home.html", {"services": services})


def healthz(request):
    return JsonResponse({"status": "ok"})
