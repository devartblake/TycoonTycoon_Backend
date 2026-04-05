from django.urls import path

from .views import dashboard_home, healthz, login_view, logout_view, operator_health, operator_users

urlpatterns = [
    path("", dashboard_home, name="dashboard-home"),
    path("login", login_view, name="operator-login"),
    path("logout", logout_view, name="operator-logout"),
    path("api/operator/health", operator_health, name="operator-health"),
    path("api/operator/users", operator_users, name="operator-users"),
    path("healthz", healthz, name="dashboard-healthz"),
]
