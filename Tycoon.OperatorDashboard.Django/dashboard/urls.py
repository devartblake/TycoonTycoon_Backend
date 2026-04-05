from django.urls import path

from .views import dashboard_home, healthz, operator_health

urlpatterns = [
    path("", dashboard_home, name="dashboard-home"),
    path("api/operator/health", operator_health, name="operator-health"),
    path("healthz", healthz, name="dashboard-healthz"),
]
