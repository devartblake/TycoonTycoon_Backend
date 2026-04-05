from django.urls import path

from .views import dashboard_home, healthz

urlpatterns = [
    path("", dashboard_home, name="dashboard-home"),
    path("healthz", healthz, name="dashboard-healthz"),
]
