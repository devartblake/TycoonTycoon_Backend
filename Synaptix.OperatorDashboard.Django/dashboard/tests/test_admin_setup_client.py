from unittest import mock

from django.test import SimpleTestCase, override_settings

from dashboard.services.admin_setup_client import get_setup_diagnostics


class AdminSetupClientTests(SimpleTestCase):
    @override_settings(DOTNET_API_BASE_URL="http://backend-api:5000", ADMIN_OPS_KEY="abc123")
    @mock.patch("dashboard.services.admin_setup_client.httpx.get")
    def test_get_setup_diagnostics_passes_auth_and_section(self, mock_get):
        response = mock.Mock()
        response.raise_for_status.return_value = None
        response.json.return_value = {"status": "healthy", "readOnly": True}
        mock_get.return_value = response

        payload = get_setup_diagnostics("access-token", "status")

        self.assertTrue(payload["readOnly"])
        url, = mock_get.call_args.args
        self.assertEqual("http://backend-api:5000/admin/setup/status", url)
        self.assertEqual("Bearer access-token", mock_get.call_args.kwargs["headers"]["Authorization"])
        self.assertEqual("abc123", mock_get.call_args.kwargs["headers"]["X-Admin-Ops-Key"])

    def test_get_setup_diagnostics_rejects_unknown_section(self):
        with self.assertRaises(ValueError):
            get_setup_diagnostics("access-token", "destroy")

    @override_settings(DOTNET_API_BASE_URL="http://backend-api:5000", ADMIN_OPS_KEY="abc123")
    @mock.patch("dashboard.services.admin_setup_client.httpx.get")
    def test_get_setup_diagnostics_uses_history_limit(self, mock_get):
        response = mock.Mock()
        response.raise_for_status.return_value = None
        response.json.return_value = {"source": "durable-setup-report-store", "reports": []}
        mock_get.return_value = response

        payload = get_setup_diagnostics("access-token", "history")

        self.assertEqual("durable-setup-report-store", payload["source"])
        url, = mock_get.call_args.args
        self.assertEqual("http://backend-api:5000/admin/setup/history?limit=20", url)
