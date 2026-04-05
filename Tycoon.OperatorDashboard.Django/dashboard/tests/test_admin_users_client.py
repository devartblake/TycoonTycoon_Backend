from unittest import mock

from django.test import SimpleTestCase, override_settings

from dashboard.services.admin_users_client import list_admin_users


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
