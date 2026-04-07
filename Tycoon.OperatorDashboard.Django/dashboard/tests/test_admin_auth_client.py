from unittest import mock

from django.test import SimpleTestCase, override_settings

from dashboard.services.admin_auth_client import admin_login, admin_me, admin_refresh


class AdminAuthClientTests(SimpleTestCase):
    @override_settings(DOTNET_API_BASE_URL="http://backend-api:5000", ADMIN_OPS_KEY="abc123")
    @mock.patch("dashboard.services.admin_auth_client.httpx.post")
    def test_admin_login_uses_admin_ops_header(self, mock_post):
        response = mock.Mock()
        response.raise_for_status.return_value = None
        response.json.return_value = {
            "accessToken": "at",
            "refreshToken": "rt",
            "expiresIn": 3600,
            "admin": {"email": "ops@example.com"},
        }
        mock_post.return_value = response

        result = admin_login("ops@example.com", "secret")

        self.assertEqual("at", result.access_token)
        _, kwargs = mock_post.call_args
        self.assertEqual("abc123", kwargs["headers"]["X-Admin-Ops-Key"])

    @override_settings(DOTNET_API_BASE_URL="http://backend-api:5000", ADMIN_OPS_KEY="abc123")
    @mock.patch("dashboard.services.admin_auth_client.httpx.post")
    def test_admin_refresh_returns_access_token(self, mock_post):
        response = mock.Mock()
        response.raise_for_status.return_value = None
        response.json.return_value = {
            "accessToken": "new-token",
            "expiresIn": 1800,
            "tokenType": "Bearer",
        }
        mock_post.return_value = response

        result = admin_refresh("refresh-token")

        self.assertEqual("new-token", result.access_token)
        self.assertEqual(1800, result.expires_in)

    @override_settings(DOTNET_API_BASE_URL="http://backend-api:5000", ADMIN_OPS_KEY="abc123")
    @mock.patch("dashboard.services.admin_auth_client.httpx.get")
    def test_admin_me_uses_bearer_token(self, mock_get):
        response = mock.Mock()
        response.raise_for_status.return_value = None
        response.json.return_value = {"email": "ops@example.com"}
        mock_get.return_value = response

        payload = admin_me("at")

        self.assertEqual("ops@example.com", payload["email"])
        _, kwargs = mock_get.call_args
        self.assertEqual("Bearer at", kwargs["headers"]["Authorization"])
        self.assertEqual("abc123", kwargs["headers"]["X-Admin-Ops-Key"])

    @override_settings(
        DOTNET_API_BASE_URL="http://backend-api:5000",
        ADMIN_OPS_HEADER="X-Custom-Ops",
        ADMIN_OPS_KEY="custom-key",
    )
    @mock.patch("dashboard.services.admin_auth_client.httpx.post")
    def test_admin_login_uses_custom_admin_ops_header_name(self, mock_post):
        response = mock.Mock()
        response.raise_for_status.return_value = None
        response.json.return_value = {
            "accessToken": "at",
            "refreshToken": "rt",
            "expiresIn": 3600,
            "admin": {"email": "ops@example.com"},
        }
        mock_post.return_value = response

        admin_login("ops@example.com", "secret")

        _, kwargs = mock_post.call_args
        self.assertEqual("custom-key", kwargs["headers"]["X-Custom-Ops"])
