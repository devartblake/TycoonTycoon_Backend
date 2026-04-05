from unittest import mock

from django.test import SimpleTestCase, override_settings

from dashboard.services.admin_users_client import (
    ban_admin_user,
    get_admin_user,
    get_admin_user_activity,
    list_admin_users,
    unban_admin_user,
    update_admin_user,
)


class AdminUsersClientTests(SimpleTestCase):
    @override_settings(DOTNET_API_BASE_URL="http://backend-api:5000", ADMIN_OPS_KEY="abc123")
    @mock.patch("dashboard.services.admin_users_client.httpx.get")
    def test_list_admin_users_passes_auth_and_query(self, mock_get):
        response = mock.Mock()
        response.raise_for_status.return_value = None
        response.json.return_value = {"items": [], "page": 1}
        mock_get.return_value = response

        payload = list_admin_users("access-token", {"page": 1, "pageSize": 25})

        self.assertEqual({"items": [], "page": 1}, payload)
        _, kwargs = mock_get.call_args
        self.assertEqual("Bearer access-token", kwargs["headers"]["Authorization"])
        self.assertEqual("abc123", kwargs["headers"]["X-Admin-Ops-Key"])
        self.assertEqual({"page": 1, "pageSize": 25}, kwargs["params"])

    @override_settings(DOTNET_API_BASE_URL="http://backend-api:5000", ADMIN_OPS_KEY="abc123")
    @mock.patch("dashboard.services.admin_users_client.httpx.get")
    def test_get_admin_user(self, mock_get):
        response = mock.Mock()
        response.raise_for_status.return_value = None
        response.json.return_value = {"id": "u1"}
        mock_get.return_value = response

        payload = get_admin_user("access-token", "u1")

        self.assertEqual({"id": "u1"}, payload)

    @override_settings(DOTNET_API_BASE_URL="http://backend-api:5000", ADMIN_OPS_KEY="abc123")
    @mock.patch("dashboard.services.admin_users_client.httpx.patch")
    def test_update_admin_user(self, mock_patch):
        response = mock.Mock()
        response.raise_for_status.return_value = None
        response.json.return_value = {"id": "u1", "updatedAt": "2026-04-05T00:00:00Z"}
        mock_patch.return_value = response

        payload = update_admin_user("access-token", "u1", {"role": "admin"})

        self.assertEqual("u1", payload["id"])

    @override_settings(DOTNET_API_BASE_URL="http://backend-api:5000", ADMIN_OPS_KEY="abc123")
    @mock.patch("dashboard.services.admin_users_client.httpx.get")
    def test_get_admin_user_activity(self, mock_get):
        response = mock.Mock()
        response.raise_for_status.return_value = None
        response.json.return_value = {"items": [], "page": 1}
        mock_get.return_value = response

        payload = get_admin_user_activity("access-token", "u1", {"page": 1})

        self.assertEqual({"items": [], "page": 1}, payload)

    @override_settings(DOTNET_API_BASE_URL="http://backend-api:5000", ADMIN_OPS_KEY="abc123")
    @mock.patch("dashboard.services.admin_users_client.httpx.post")
    def test_ban_admin_user(self, mock_post):
        response = mock.Mock()
        response.raise_for_status.return_value = None
        response.json.return_value = {"id": "u1", "isBanned": True}
        mock_post.return_value = response

        payload = ban_admin_user("access-token", "u1", "policy violation")

        self.assertTrue(payload["isBanned"])

    @override_settings(DOTNET_API_BASE_URL="http://backend-api:5000", ADMIN_OPS_KEY="abc123")
    @mock.patch("dashboard.services.admin_users_client.httpx.post")
    def test_unban_admin_user(self, mock_post):
        response = mock.Mock()
        response.raise_for_status.return_value = None
        response.json.return_value = {"id": "u1", "isBanned": False}
        mock_post.return_value = response

        payload = unban_admin_user("access-token", "u1")

        self.assertFalse(payload["isBanned"])
