from unittest import mock

from django.test import SimpleTestCase, override_settings

from dashboard.services.admin_audit_client import (
    get_security_audit,
    get_security_audit_event,
)


class AdminAuditClientTests(SimpleTestCase):
    @override_settings(DOTNET_API_BASE_URL="http://backend-api:5000", ADMIN_OPS_KEY="abc123")
    @mock.patch("dashboard.services.admin_audit_client.httpx.get")
    def test_get_security_audit(self, mock_get):
        response = mock.Mock()
        response.raise_for_status.return_value = None
        response.json.return_value = {"items": [], "page": 1}
        mock_get.return_value = response

        payload = get_security_audit("access-token", {"page": 1, "pageSize": 25})

        self.assertEqual({"items": [], "page": 1}, payload)
        _, kwargs = mock_get.call_args
        self.assertEqual("Bearer access-token", kwargs["headers"]["Authorization"])
        self.assertEqual({"page": 1, "pageSize": 25}, kwargs["params"])

    @override_settings(DOTNET_API_BASE_URL="http://backend-api:5000", ADMIN_OPS_KEY="abc123")
    @mock.patch("dashboard.services.admin_audit_client.httpx.get")
    def test_get_security_audit_event(self, mock_get):
        response = mock.Mock()
        response.raise_for_status.return_value = None
        response.json.return_value = {"id": "evt1", "title": "admin_auth_login"}
        mock_get.return_value = response

        payload = get_security_audit_event("access-token", "evt1")

        self.assertEqual("evt1", payload["id"])
        self.assertEqual("http://backend-api:5000/admin/audit/security/evt1", mock_get.call_args[0][0])
