from unittest import mock

from django.test import SimpleTestCase, override_settings

from dashboard.services.admin_moderation_client import (
    get_moderation_logs,
    get_moderation_profile,
    set_moderation_status,
)


class AdminModerationClientTests(SimpleTestCase):
    @override_settings(DOTNET_API_BASE_URL="http://backend-api:5000", ADMIN_OPS_KEY="abc123")
    @mock.patch("dashboard.services.admin_moderation_client.httpx.get")
    def test_get_moderation_profile(self, mock_get):
        response = mock.Mock()
        response.raise_for_status.return_value = None
        response.json.return_value = {"playerId": "p1"}
        mock_get.return_value = response

        payload = get_moderation_profile("access-token", "p1")

        self.assertEqual("p1", payload["playerId"])

    @override_settings(DOTNET_API_BASE_URL="http://backend-api:5000", ADMIN_OPS_KEY="abc123")
    @mock.patch("dashboard.services.admin_moderation_client.httpx.get")
    def test_get_moderation_logs(self, mock_get):
        response = mock.Mock()
        response.raise_for_status.return_value = None
        response.json.return_value = {"items": [], "page": 1}
        mock_get.return_value = response

        payload = get_moderation_logs("access-token", {"page": 1})

        self.assertEqual(1, payload["page"])

    @override_settings(DOTNET_API_BASE_URL="http://backend-api:5000", ADMIN_OPS_KEY="abc123")
    @mock.patch("dashboard.services.admin_moderation_client.httpx.post")
    def test_set_moderation_status(self, mock_post):
        response = mock.Mock()
        response.raise_for_status.return_value = None
        response.json.return_value = {"playerId": "p1", "status": 2}
        mock_post.return_value = response

        payload = set_moderation_status(
            "access-token",
            "ops@example.com",
            {"playerId": "p1", "status": 2, "reason": "signal"},
        )

        self.assertEqual(2, payload["status"])
        _, kwargs = mock_post.call_args
        self.assertEqual("ops@example.com", kwargs["headers"]["X-Admin-User"])
