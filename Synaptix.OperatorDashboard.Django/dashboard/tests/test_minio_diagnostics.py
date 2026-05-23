from unittest import mock

from django.test import SimpleTestCase, override_settings

from dashboard.services.minio_diagnostics import get_minio_diagnostics


class MinioDiagnosticsTests(SimpleTestCase):
    @override_settings(MINIO_BASE_URL="http://minio:9000")
    @mock.patch("dashboard.services.minio_diagnostics.httpx.get")
    def test_get_minio_diagnostics(self, mock_get):
        ok_response = mock.Mock()
        ok_response.is_success = True
        ok_response.status_code = 200
        ok_response.text = "OK"

        mock_get.side_effect = [ok_response, ok_response]

        payload = get_minio_diagnostics()

        self.assertEqual("healthy", payload["overallStatus"])
        self.assertIn("live", payload["checks"])
        self.assertIn("ready", payload["checks"])
