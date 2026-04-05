from django.urls import path

from .views import (
    dashboard_home,
    healthz,
    login_view,
    logout_view,
    operator_health,
    operator_user_activity,
    operator_user_ban,
    operator_user_detail,
    operator_user_unban,
    operator_user_update,
    operator_users,
)

urlpatterns = [
    path("", dashboard_home, name="dashboard-home"),
    path("login", login_view, name="operator-login"),
    path("logout", logout_view, name="operator-logout"),
    path("api/operator/health", operator_health, name="operator-health"),
    path("api/operator/users", operator_users, name="operator-users"),
    path("api/operator/users/<str:user_id>", operator_user_detail, name="operator-user-detail"),
    path("api/operator/users/<str:user_id>/activity", operator_user_activity, name="operator-user-activity"),
    path("api/operator/users/<str:user_id>/update", operator_user_update, name="operator-user-update"),
    path("api/operator/users/<str:user_id>/ban", operator_user_ban, name="operator-user-ban"),
    path("api/operator/users/<str:user_id>/unban", operator_user_unban, name="operator-user-unban"),
    path("healthz", healthz, name="dashboard-healthz"),
]
