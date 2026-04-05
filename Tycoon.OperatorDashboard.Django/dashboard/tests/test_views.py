from unittest import mock

from django.test import SimpleTestCase
from django.urls import reverse


class DashboardViewsTests(SimpleTestCase):
    @mock.patch("dashboard.views.list_service_statuses")
    @mock.patch("dashboard.views.get_overall_status")
    def test_operator_health_endpoint_returns_json(self, mock_overall_status, mock_list_service_statuses):
        service = mock.Mock()
        service.to_dict.return_value = {
            "service_name": ".NET API",
            "base_url": "http://backend-api:5000",
            "status": "healthy",
            "detail": "Request succeeded",
            "payload": {"status": "ok"},
            "css_class": "status-ok",
        }
        mock_list_service_statuses.return_value = [service]
        mock_overall_status.return_value = "healthy"

        response = self.client.get(reverse("operator-health"))

        self.assertEqual(200, response.status_code)
        body = response.json()
        self.assertEqual("healthy", body["status"])
        self.assertEqual(1, len(body["services"]))
        self.assertIn("generatedAt", body)

    @mock.patch("dashboard.views.list_service_statuses")
    @mock.patch("dashboard.views.get_overall_status")
    def test_dashboard_home_renders(self, mock_overall_status, mock_list_service_statuses):
        service = mock.Mock()
        service.status = "healthy"
        service.css_class = "status-ok"
        mock_list_service_statuses.return_value = [service]
        mock_overall_status.return_value = "healthy"

        response = self.client.get(reverse("dashboard-home"))

        self.assertEqual(200, response.status_code)
        self.assertContains(response, "Tycoon Operator Dashboard")
