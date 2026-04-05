from unittest import mock

import httpx
from django.test import TestCase
from django.urls import reverse


class DashboardViewsTests(TestCase):
    @mock.patch("dashboard.views.list_service_statuses")
    @mock.patch("dashboard.views.get_overall_status")
    def test_operator_health_endpoint_requires_login(self, mock_overall_status, mock_list_service_statuses):
        response = self.client.get(reverse("operator-health"))
        self.assertEqual(302, response.status_code)
        self.assertEqual(reverse("operator-login"), response.url)

    @mock.patch("dashboard.views.list_service_statuses")
    @mock.patch("dashboard.views.get_overall_status")
    def test_operator_health_endpoint_returns_json_when_logged_in(self, mock_overall_status, mock_list_service_statuses):
        session = self.client.session
        session["operator_access_token"] = "token"
        session["operator_access_expires_at"] = 32503680000
        session["operator_admin_profile"] = {"email": "ops@example.com"}
        session.save()

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

    def test_dashboard_home_redirects_to_login_when_not_authenticated(self):
        response = self.client.get(reverse("dashboard-home"))
        self.assertEqual(302, response.status_code)
        self.assertEqual(reverse("operator-login"), response.url)

    @mock.patch("dashboard.views.admin_me")
    @mock.patch("dashboard.views.admin_login")
    def test_login_success_sets_session_and_redirects(self, mock_admin_login, mock_admin_me):
        mock_admin_login.return_value = mock.Mock(
            access_token="at",
            refresh_token="rt",
            expires_in=3600,
        )
        mock_admin_me.return_value = {"email": "ops@example.com", "roles": ["admin"]}

        response = self.client.post(
            reverse("operator-login"),
            data={"email": "ops@example.com", "password": "secret"},
        )

        self.assertEqual(302, response.status_code)
        self.assertEqual(reverse("dashboard-home"), response.url)

        session = self.client.session
        self.assertEqual("at", session.get("operator_access_token"))
        self.assertEqual("rt", session.get("operator_refresh_token"))
        self.assertEqual("ops@example.com", session.get("operator_admin_profile")["email"])

    @mock.patch("dashboard.views.admin_login")
    def test_login_invalid_credentials(self, mock_admin_login):
        req = httpx.Request("POST", "http://backend/admin/auth/login")
        resp = httpx.Response(status_code=401, request=req)
        mock_admin_login.side_effect = httpx.HTTPStatusError("unauthorized", request=req, response=resp)

        response = self.client.post(
            reverse("operator-login"),
            data={"email": "ops@example.com", "password": "bad"},
        )

        self.assertEqual(401, response.status_code)
        self.assertContains(response, "Invalid operator credentials", status_code=401)

    @mock.patch("dashboard.views.admin_me")
    @mock.patch("dashboard.views.admin_refresh")
    @mock.patch("dashboard.views.list_service_statuses")
    @mock.patch("dashboard.views.get_overall_status")
    def test_expired_session_refreshes_on_protected_route(
        self,
        mock_overall_status,
        mock_list_service_statuses,
        mock_admin_refresh,
        mock_admin_me,
    ):
        session = self.client.session
        session["operator_access_token"] = "old-token"
        session["operator_refresh_token"] = "refresh-token"
        session["operator_access_expires_at"] = 1
        session.save()

        mock_admin_refresh.return_value = mock.Mock(access_token="new-token", expires_in=1200)
        mock_admin_me.return_value = {"email": "ops@example.com", "roles": ["admin"]}
        mock_list_service_statuses.return_value = []
        mock_overall_status.return_value = "healthy"

        response = self.client.get(reverse("dashboard-home"))

        self.assertEqual(200, response.status_code)
        self.assertEqual("new-token", self.client.session.get("operator_access_token"))

    @mock.patch("dashboard.views.list_admin_users")
    def test_operator_users_endpoint_returns_payload(self, mock_list_admin_users):
        session = self.client.session
        session["operator_access_token"] = "token"
        session["operator_access_expires_at"] = 32503680000
        session.save()

        mock_list_admin_users.return_value = {"items": [{"id": "u1"}], "page": 1}

        response = self.client.get(reverse("operator-users"), {"page": 1, "pageSize": 10})

        self.assertEqual(200, response.status_code)
        self.assertEqual({"items": [{"id": "u1"}], "page": 1}, response.json())
