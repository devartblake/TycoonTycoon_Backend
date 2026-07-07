from django.apps import AppConfig


class DashboardConfig(AppConfig):
    default_auto_field = "django.db.models.BigAutoField"
    name = "dashboard"

    def ready(self):
        """Initialize app and register signal handlers."""
        import atexit
        from .services.http_client_pool import close_http_client

        # Close HTTP client on app shutdown
        atexit.register(close_http_client)
