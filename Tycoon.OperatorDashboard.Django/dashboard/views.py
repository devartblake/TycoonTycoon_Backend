from django.http import JsonResponse
from django.shortcuts import render
from django.utils import timezone

from .services.api_clients import get_overall_status, list_service_statuses


STATUS_CLASSES = {
    "healthy": "status-ok",
    "degraded": "status-warn",
    "offline": "status-bad",
}


def dashboard_home(request):
    services = list_service_statuses()

    for service in services:
        service.css_class = STATUS_CLASSES.get(service.status, "status-unknown")

    context = {
        "services": services,
        "overall_status": get_overall_status(services),
        "generated_at": timezone.now(),
    }
    return render(request, "dashboard/home.html", context)


def operator_health(request):
    services = list_service_statuses()
    return JsonResponse(
        {
            "status": get_overall_status(services),
            "services": [service.to_dict() for service in services],
            "generatedAt": timezone.now().isoformat(),
        }
    )


def healthz(request):
    return JsonResponse({"status": "ok"})
