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
        session["operator_admin_profile"] = {"email": "ops@example.com", "permissions": ["users:read"]}
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
        mock_admin_me.return_value = {"email": "ops@example.com", "roles": ["admin"], "permissions": ["users:read"]}

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
        mock_admin_me.return_value = {"email": "ops@example.com", "roles": ["admin"], "permissions": ["users:read"]}
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
        session["operator_admin_profile"] = {"permissions": ["users:read"]}
        session.save()

        mock_list_admin_users.return_value = {"items": [{"id": "u1"}], "page": 1}

        response = self.client.get(reverse("operator-users"), {"page": 1, "pageSize": 10})

        self.assertEqual(200, response.status_code)
        self.assertEqual({"items": [{"id": "u1"}], "page": 1}, response.json())

    def test_operator_users_requires_permission(self):
        session = self.client.session
        session["operator_access_token"] = "token"
        session["operator_access_expires_at"] = 32503680000
        session["operator_admin_profile"] = {"permissions": []}
        session.save()

        response = self.client.get(reverse("operator-users"))
        self.assertEqual(403, response.status_code)

    @mock.patch("dashboard.views.get_admin_user")
    def test_operator_user_detail(self, mock_get_admin_user):
        session = self.client.session
        session["operator_access_token"] = "token"
        session["operator_access_expires_at"] = 32503680000
        session["operator_admin_profile"] = {"permissions": ["users:read"]}
        session.save()

        mock_get_admin_user.return_value = {"id": "u1"}
        response = self.client.get(reverse("operator-user-detail", kwargs={"user_id": "u1"}))

        self.assertEqual(200, response.status_code)
        self.assertEqual("u1", response.json()["id"])

    @mock.patch("dashboard.views.ban_admin_user")
    def test_operator_user_ban(self, mock_ban_admin_user):
        session = self.client.session
        session["operator_access_token"] = "token"
        session["operator_access_expires_at"] = 32503680000
        session["operator_admin_profile"] = {"permissions": ["users:write"]}
        session.save()

        mock_ban_admin_user.return_value = {"id": "u1", "isBanned": True}
        response = self.client.post(reverse("operator-user-ban", kwargs={"user_id": "u1"}), data={"reason": "policy"})

        self.assertEqual(200, response.status_code)
        self.assertTrue(response.json()["isBanned"])

    @mock.patch("dashboard.views.unban_admin_user")
    def test_operator_user_unban(self, mock_unban_admin_user):
        session = self.client.session
        session["operator_access_token"] = "token"
        session["operator_access_expires_at"] = 32503680000
        session["operator_admin_profile"] = {"permissions": ["users:write"]}
        session.save()

        mock_unban_admin_user.return_value = {"id": "u1", "isBanned": False}
        response = self.client.post(reverse("operator-user-unban", kwargs={"user_id": "u1"}))

        self.assertEqual(200, response.status_code)
        self.assertFalse(response.json()["isBanned"])


    @mock.patch("dashboard.views.get_admin_user_activity")
    def test_operator_user_activity(self, mock_get_admin_user_activity):
        session = self.client.session
        session["operator_access_token"] = "token"
        session["operator_access_expires_at"] = 32503680000
        session["operator_admin_profile"] = {"permissions": ["users:read"]}
        session.save()

        mock_get_admin_user_activity.return_value = {"items": [{"id": "a1"}], "page": 1}
        response = self.client.get(reverse("operator-user-activity", kwargs={"user_id": "u1"}), {"page": 1})

        self.assertEqual(200, response.status_code)
        self.assertEqual(1, len(response.json()["items"]))

    @mock.patch("dashboard.views.update_admin_user")
    def test_operator_user_update(self, mock_update_admin_user):
        session = self.client.session
        session["operator_access_token"] = "token"
        session["operator_access_expires_at"] = 32503680000
        session["operator_admin_profile"] = {"permissions": ["users:write"]}
        session.save()

        mock_update_admin_user.return_value = {"id": "u1"}
        response = self.client.post(reverse("operator-user-update", kwargs={"user_id": "u1"}), data={"role": "admin"})

        self.assertEqual(200, response.status_code)
        self.assertEqual("u1", response.json()["id"])

    def test_operator_user_update_requires_payload(self):
        session = self.client.session
        session["operator_access_token"] = "token"
        session["operator_access_expires_at"] = 32503680000
        session["operator_admin_profile"] = {"permissions": ["users:write"]}
        session.save()

        response = self.client.post(reverse("operator-user-update", kwargs={"user_id": "u1"}), data={})
        self.assertEqual(422, response.status_code)


    @mock.patch("dashboard.views.get_security_audit")
    def test_operator_audit_security(self, mock_get_security_audit):
        session = self.client.session
        session["operator_access_token"] = "token"
        session["operator_access_expires_at"] = 32503680000
        session["operator_admin_profile"] = {"permissions": ["events:read"]}
        session.save()

        mock_get_security_audit.return_value = {"items": [], "page": 1}
        response = self.client.get(reverse("operator-audit-security"), {"page": 1})

        self.assertEqual(200, response.status_code)
        self.assertEqual(1, response.json()["page"])

    def test_operator_audit_security_requires_permission(self):
        session = self.client.session
        session["operator_access_token"] = "token"
        session["operator_access_expires_at"] = 32503680000
        session["operator_admin_profile"] = {"permissions": []}
        session.save()

        response = self.client.get(reverse("operator-audit-security"))
        self.assertEqual(403, response.status_code)


    @mock.patch("dashboard.views.get_moderation_logs")
    def test_operator_moderation_logs(self, mock_get_moderation_logs):
        session = self.client.session
        session["operator_access_token"] = "token"
        session["operator_access_expires_at"] = 32503680000
        session["operator_admin_profile"] = {"permissions": ["events:read"]}
        session.save()

        mock_get_moderation_logs.return_value = {"items": [], "page": 1}
        response = self.client.get(reverse("operator-moderation-logs"), {"page": 1})

        self.assertEqual(200, response.status_code)
        self.assertEqual(1, response.json()["page"])

    @mock.patch("dashboard.views.get_moderation_profile")
    def test_operator_moderation_profile(self, mock_get_moderation_profile):
        session = self.client.session
        session["operator_access_token"] = "token"
        session["operator_access_expires_at"] = 32503680000
        session["operator_admin_profile"] = {"permissions": ["events:read"]}
        session.save()

        mock_get_moderation_profile.return_value = {"playerId": "p1"}
        response = self.client.get(reverse("operator-moderation-profile", kwargs={"player_id": "p1"}))

        self.assertEqual(200, response.status_code)
        self.assertEqual("p1", response.json()["playerId"])

    @mock.patch("dashboard.views.set_moderation_status")
    def test_operator_moderation_set_status(self, mock_set_moderation_status):
        session = self.client.session
        session["operator_access_token"] = "token"
        session["operator_access_expires_at"] = 32503680000
        session["operator_admin_profile"] = {"permissions": ["events:write"], "email": "ops@example.com"}
        session.save()

        mock_set_moderation_status.return_value = {"playerId": "p1", "status": 2}
        response = self.client.post(
            reverse("operator-moderation-set-status"),
            data={"playerId": "p1", "status": 2, "reason": "signal"},
        )

        self.assertEqual(200, response.status_code)
        self.assertEqual(2, response.json()["status"])

    def test_operator_moderation_set_status_requires_fields(self):
        session = self.client.session
        session["operator_access_token"] = "token"
        session["operator_access_expires_at"] = 32503680000
        session["operator_admin_profile"] = {"permissions": ["events:write"], "email": "ops@example.com"}
        session.save()

        response = self.client.post(reverse("operator-moderation-set-status"), data={"playerId": "p1"})
        self.assertEqual(422, response.status_code)
