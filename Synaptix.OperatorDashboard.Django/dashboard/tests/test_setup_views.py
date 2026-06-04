from unittest import mock

from django.test import TestCase
from django.urls import reverse


class SetupViewsTests(TestCase):
    def _login(self, permissions):
        session = self.client.session
        session["operator_access_token"] = "token"
        session["operator_access_expires_at"] = 32503680000
        session["operator_admin_profile"] = {"permissions": permissions, "email": "ops@example.com"}
        session.save()

    def test_setup_page_redirects_to_login_when_unauthenticated(self):
        response = self.client.get(reverse("setup-status-view"))

        self.assertEqual(302, response.status_code)
        self.assertEqual(reverse("operator-login"), response.url)

    def test_setup_page_requires_setup_read(self):
        self._login(["users:read"])

        response = self.client.get(reverse("setup-status-view"), HTTP_ACCEPT="application/json")

        self.assertEqual(403, response.status_code)

    def test_setup_bff_requires_setup_read(self):
        self._login(["users:read"])

        response = self.client.get(reverse("operator-setup-status"))

        self.assertEqual(403, response.status_code)

    @mock.patch("dashboard.views.get_setup_diagnostics")
    def test_setup_status_page_renders_read_only_boundary(self, mock_get):
        self._login(["setup:read"])
        mock_get.return_value = {
            "status": "healthy",
            "source": "live-backend-diagnostics",
            "generatedAt": "2026-06-04T00:00:00Z",
            "readOnly": True,
            "durableReportAvailable": False,
            "remediation": "Use Synaptix.Setup CLI.",
        }

        response = self.client.get(reverse("setup-status-view"))

        self.assertEqual(200, response.status_code)
        self.assertContains(response, "Sanitized live Backend diagnostics")
        self.assertContains(response, "This dashboard never executes Synaptix.Setup")

    @mock.patch("dashboard.views.get_setup_diagnostics")
    def test_setup_services_bff_proxies_backend_payload(self, mock_get):
        self._login(["setup:read"])
        mock_get.return_value = {
            "source": "live-backend-diagnostics",
            "services": [{"name": "postgresql", "status": "healthy"}],
        }

        response = self.client.get(reverse("operator-setup-services"))

        self.assertEqual(200, response.status_code)
        self.assertEqual("postgresql", response.json()["services"][0]["name"])
        mock_get.assert_called_once_with("token", "services")

    @mock.patch("dashboard.views.get_setup_diagnostics")
    def test_setup_history_page_renders_durable_reports(self, mock_get):
        self._login(["setup:read"])
        mock_get.return_value = {
            "source": "durable-setup-report-store",
            "reports": [
                {
                    "createdAt": "2026-06-04T22:30:00Z",
                    "status": "healthy",
                    "warningCount": 0,
                    "source": "live-backend-diagnostics",
                }
            ],
        }

        response = self.client.get(reverse("setup-history-view"))

        self.assertEqual(200, response.status_code)
        self.assertContains(response, "Durable setup reports")
        self.assertContains(response, "2026-06-04T22:30:00Z")
