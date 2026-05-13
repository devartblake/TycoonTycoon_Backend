from unittest import mock

import httpx
from django.test import TestCase
from django.urls import reverse

from dashboard.models import OperatorSavedViewAuditEvent
from dashboard.services.admin_auth_client import AdminAuthConfigurationError, KmsUnavailableError


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

    @mock.patch("dashboard.views.admin_login")
    def test_login_secure_channel_required_message(self, mock_admin_login):
        req = httpx.Request("POST", "http://backend/admin/auth/login")
        resp = httpx.Response(status_code=400, request=req, content=b'{"error":{"code":"secure_session_required"}}')
        mock_admin_login.side_effect = httpx.HTTPStatusError("secure channel", request=req, response=resp)

        response = self.client.post(
            reverse("operator-login"),
            data={"email": "ops@example.com", "password": "secret"},
        )

        self.assertEqual(502, response.status_code)
        self.assertContains(response, "requires secure-channel", status_code=502)

    @mock.patch("dashboard.views.admin_login")
    def test_login_kms_unavailable_message(self, mock_admin_login):
        mock_admin_login.side_effect = KmsUnavailableError("kms down")

        response = self.client.post(
            reverse("operator-login"),
            data={"email": "ops@example.com", "password": "secret"},
        )

        self.assertEqual(503, response.status_code)
        self.assertContains(response, "secure-channel login through KMS", status_code=503)

    @mock.patch("dashboard.views.admin_login")
    def test_login_auth_configuration_error_message(self, mock_admin_login):
        mock_admin_login.side_effect = AdminAuthConfigurationError("KMS_SERVICE_TOKEN is required")

        response = self.client.post(
            reverse("operator-login"),
            data={"email": "ops@example.com", "password": "secret"},
        )

        self.assertEqual(500, response.status_code)
        self.assertContains(response, "KMS_SERVICE_TOKEN is required", status_code=500)

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

    @mock.patch("dashboard.views.list_admin_users")
    def test_operator_users_view_renders_table(self, mock_list_admin_users):
        session = self.client.session
        session["operator_access_token"] = "token"
        session["operator_access_expires_at"] = 32503680000
        session["operator_admin_profile"] = {"permissions": ["users:read"], "email": "ops@example.com"}
        session.save()

        mock_list_admin_users.return_value = {
            "items": [{"id": "u1", "email": "user@example.com", "username": "user1", "role": "player", "isVerified": True, "isBanned": False}],
            "total": 51,
            "page": 1,
            "pageSize": 25,
        }
        response = self.client.get(reverse("operator-users-view"), {"q": "user"})

        self.assertEqual(200, response.status_code)
        self.assertContains(response, "Operator Users")
        self.assertContains(response, "user@example.com")
        self.assertContains(response, "Next →")
        _, call_query = mock_list_admin_users.call_args[0]
        self.assertEqual("createdAt", call_query["sortBy"])
        self.assertEqual("desc", call_query["sortOrder"])

    @mock.patch("dashboard.views.list_admin_users")
    def test_operator_users_view_preset_banned_recent(self, mock_list_admin_users):
        session = self.client.session
        session["operator_access_token"] = "token"
        session["operator_access_expires_at"] = 32503680000
        session["operator_admin_profile"] = {"permissions": ["users:read"], "email": "ops@example.com"}
        session.save()

        mock_list_admin_users.return_value = {"items": [], "total": 0}
        response = self.client.get(reverse("operator-users-view"), {"preset": "banned_recent"})

        self.assertEqual(200, response.status_code)
        _, call_query = mock_list_admin_users.call_args[0]
        self.assertEqual("true", call_query["isBanned"])
        self.assertEqual("updatedAt", call_query["sortBy"])

    @mock.patch("dashboard.views.list_admin_users")
    def test_operator_users_view_invalid_page_defaults_to_one(self, mock_list_admin_users):
        session = self.client.session
        session["operator_access_token"] = "token"
        session["operator_access_expires_at"] = 32503680000
        session["operator_admin_profile"] = {"permissions": ["users:read"], "email": "ops@example.com"}
        session.save()

        mock_list_admin_users.return_value = {"items": [], "total": 0}
        response = self.client.get(reverse("operator-users-view"), {"page": "bad", "pageSize": "nope"})

        self.assertEqual(200, response.status_code)
        self.assertContains(response, "page 1")

    def test_operator_users_view_redirects_to_login_when_not_authenticated(self):
        response = self.client.get(reverse("operator-users-view"))
        self.assertEqual(302, response.status_code)
        self.assertEqual(reverse("operator-login"), response.url)

    def test_operator_users_view_requires_permission(self):
        session = self.client.session
        session["operator_access_token"] = "token"
        session["operator_access_expires_at"] = 32503680000
        session["operator_admin_profile"] = {"permissions": []}
        session.save()

        response = self.client.get(reverse("operator-users-view"))
        self.assertEqual(403, response.status_code)

    @mock.patch("dashboard.views.list_admin_users")
    @mock.patch("dashboard.views.ban_admin_user")
    def test_operator_users_view_bulk_ban(self, mock_ban_admin_user, mock_list_admin_users):
        session = self.client.session
        session["operator_access_token"] = "token"
        session["operator_access_expires_at"] = 32503680000
        session["operator_admin_profile"] = {"permissions": ["users:read", "users:write"], "email": "ops@example.com"}
        session.save()

        mock_list_admin_users.return_value = {"items": [{"id": "u1"}, {"id": "u2"}], "total": 2}
        response = self.client.post(
            reverse("operator-users-view"),
            data={"action": "ban", "reason": "policy", "confirm": "YES", "userIds": ["u1", "u2"]},
        )

        self.assertEqual(200, response.status_code)
        self.assertEqual(2, mock_ban_admin_user.call_count)
        self.assertContains(response, "Bulk ban")

    @mock.patch("dashboard.views.list_admin_users")
    @mock.patch("dashboard.views.ban_admin_user")
    def test_operator_users_view_bulk_ban_dry_run(self, mock_ban_admin_user, mock_list_admin_users):
        session = self.client.session
        session["operator_access_token"] = "token"
        session["operator_access_expires_at"] = 32503680000
        session["operator_admin_profile"] = {"permissions": ["users:read", "users:write"], "email": "ops@example.com"}
        session.save()

        mock_list_admin_users.return_value = {"items": [{"id": "u1"}], "total": 1}
        response = self.client.post(
            reverse("operator-users-view"),
            data={"action": "ban", "dryRun": "true", "userIds": ["u1"]},
        )

        self.assertEqual(200, response.status_code)
        self.assertEqual(0, mock_ban_admin_user.call_count)
        self.assertContains(response, "Dry-run")

    def test_operator_users_view_bulk_action_requires_write_permission(self):
        session = self.client.session
        session["operator_access_token"] = "token"
        session["operator_access_expires_at"] = 32503680000
        session["operator_admin_profile"] = {"permissions": ["users:read"]}
        session.save()

        response = self.client.post(reverse("operator-users-view"), data={"action": "ban", "userIds": ["u1"]})
        self.assertEqual(403, response.status_code)

    @mock.patch("dashboard.views._save_named_view")
    @mock.patch("dashboard.views.list_admin_users")
    def test_operator_users_view_save_view(self, mock_list_admin_users, mock_save_named_view):
        session = self.client.session
        session["operator_access_token"] = "token"
        session["operator_access_expires_at"] = 32503680000
        session["operator_admin_profile"] = {"permissions": ["users:read"], "email": "ops@example.com"}
        session.save()

        mock_list_admin_users.return_value = {"items": [], "total": 0}
        response = self.client.post(
            reverse("operator-users-view"),
            data={"action": "save_view", "viewName": "Banned", "isBanned": "true", "sortBy": "updatedAt", "sortOrder": "desc", "pageSize": "25"},
        )

        self.assertEqual(200, response.status_code)
        mock_save_named_view.assert_called_once()

    @mock.patch("dashboard.views._load_saved_views")
    @mock.patch("dashboard.views.list_admin_users")
    def test_operator_users_view_applies_saved_view(self, mock_list_admin_users, mock_load_saved_views):
        session = self.client.session
        session["operator_access_token"] = "token"
        session["operator_access_expires_at"] = 32503680000
        session["operator_admin_profile"] = {"permissions": ["users:read"], "email": "ops@example.com"}
        session.save()
        mock_load_saved_views.return_value = {
            "ops@example.com::Banned": {
                "name": "Banned",
                "owner_email": "ops@example.com",
                "is_shared": False,
                "query": {"isBanned": "true", "sortBy": "updatedAt", "sortOrder": "desc"},
            }
        }

        mock_list_admin_users.return_value = {"items": [], "total": 0}
        response = self.client.get(reverse("operator-users-view"), {"savedView": "ops@example.com::Banned"})

        self.assertEqual(200, response.status_code)
        _, call_query = mock_list_admin_users.call_args[0]
        self.assertEqual("true", call_query["isBanned"])
        self.assertEqual("updatedAt", call_query["sortBy"])

    @mock.patch("dashboard.views._archive_named_view")
    @mock.patch("dashboard.views._load_saved_views")
    @mock.patch("dashboard.views.list_admin_users")
    def test_operator_users_view_archive_saved_view(self, mock_list_admin_users, mock_load_saved_views, mock_archive_named_view):
        session = self.client.session
        session["operator_access_token"] = "token"
        session["operator_access_expires_at"] = 32503680000
        session["operator_admin_profile"] = {"permissions": ["users:read"], "email": "ops@example.com"}
        session.save()

        mock_list_admin_users.return_value = {"items": [], "total": 0}
        mock_load_saved_views.return_value = {}
        mock_archive_named_view.return_value = True
        response = self.client.post(reverse("operator-users-view"), data={"action": "archive_view", "viewName": "ops@example.com::Banned"})

        self.assertEqual(200, response.status_code)
        mock_archive_named_view.assert_called_once()

    @mock.patch("dashboard.views._transfer_named_view")
    @mock.patch("dashboard.views._load_saved_views")
    @mock.patch("dashboard.views.list_admin_users")
    def test_operator_users_view_transfer_saved_view(self, mock_list_admin_users, mock_load_saved_views, mock_transfer_named_view):
        session = self.client.session
        session["operator_access_token"] = "token"
        session["operator_access_expires_at"] = 32503680000
        session["operator_admin_profile"] = {"permissions": ["users:read"], "email": "ops@example.com"}
        session.save()

        mock_list_admin_users.return_value = {"items": [], "total": 0}
        mock_load_saved_views.return_value = {}
        mock_transfer_named_view.return_value = True
        response = self.client.post(
            reverse("operator-users-view"),
            data={"action": "transfer_view", "viewName": "ops@example.com::Banned", "newOwnerEmail": "next@example.com"},
        )

        self.assertEqual(200, response.status_code)
        mock_transfer_named_view.assert_called_once()

    @mock.patch("dashboard.views.list_admin_users")
    def test_operator_users_view_saved_view_governance_audit_filters(self, mock_list_admin_users):
        session = self.client.session
        session["operator_access_token"] = "token"
        session["operator_access_expires_at"] = 32503680000
        session["operator_admin_profile"] = {"permissions": ["users:read"], "email": "ops@example.com"}
        session.save()

        OperatorSavedViewAuditEvent.objects.create(
            actor_email="ops@example.com",
            owner_email="ops@example.com",
            view_name="Banned",
            action="archive",
            metadata={"source": "users"},
        )
        OperatorSavedViewAuditEvent.objects.create(
            actor_email="ops@example.com",
            owner_email="ops@example.com",
            view_name="Banned",
            action="transfer",
            metadata={"from_owner": "ops@example.com"},
        )

        mock_list_admin_users.return_value = {"items": [], "total": 0}
        response = self.client.get(reverse("operator-users-view"), {"savedViewAuditAction": "archive"})

        self.assertEqual(200, response.status_code)
        self.assertContains(response, "Saved-view governance audit timeline")
        self.assertContains(response, "<td>archive</td>", html=True)
        self.assertNotContains(response, "<td>transfer</td>", html=True)

    @mock.patch("dashboard.views.list_admin_users")
    def test_operator_users_view_saved_view_governance_audit_exports_csv(self, mock_list_admin_users):
        session = self.client.session
        session["operator_access_token"] = "token"
        session["operator_access_expires_at"] = 32503680000
        session["operator_admin_profile"] = {"permissions": ["users:read"], "email": "ops@example.com"}
        session.save()

        OperatorSavedViewAuditEvent.objects.create(
            actor_email="ops@example.com",
            owner_email="next@example.com",
            view_name="Risk",
            action="transfer",
            metadata={"from_owner": "ops@example.com"},
        )

        mock_list_admin_users.return_value = {"items": [], "total": 0}
        response = self.client.get(reverse("operator-users-view"), {"savedViewAuditFormat": "csv"})

        self.assertEqual(200, response.status_code)
        self.assertEqual("text/csv; charset=utf-8", response["Content-Type"])
        self.assertIn("transfer", response.content.decode())
        self.assertIn("from_owner", response.content.decode())

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

    @mock.patch("dashboard.views.get_security_audit")
    def test_operator_audit_security_view_renders(self, mock_get_security_audit):
        session = self.client.session
        session["operator_access_token"] = "token"
        session["operator_access_expires_at"] = 32503680000
        session["operator_admin_profile"] = {"permissions": ["events:read"], "email": "ops@example.com"}
        session.save()

        mock_get_security_audit.return_value = {
            "items": [{"event": "admin_login", "status": "success", "actor": "ops@example.com"}],
            "page": 1,
            "total": 1,
        }
        response = self.client.get(reverse("operator-audit-security-view"), {"status": "success"})

        self.assertEqual(200, response.status_code)
        self.assertContains(response, "Security Audit")
        self.assertContains(response, "admin_login")

    def test_operator_audit_security_view_requires_login(self):
        response = self.client.get(reverse("operator-audit-security-view"))
        self.assertEqual(302, response.status_code)
        self.assertEqual(reverse("operator-login"), response.url)

    def test_operator_audit_security_view_requires_permission(self):
        session = self.client.session
        session["operator_access_token"] = "token"
        session["operator_access_expires_at"] = 32503680000
        session["operator_admin_profile"] = {"permissions": []}
        session.save()

        response = self.client.get(reverse("operator-audit-security-view"))
        self.assertEqual(403, response.status_code)

    @mock.patch("dashboard.views.get_security_audit")
    def test_operator_audit_security_view_preset(self, mock_get_security_audit):
        session = self.client.session
        session["operator_access_token"] = "token"
        session["operator_access_expires_at"] = 32503680000
        session["operator_admin_profile"] = {"permissions": ["events:read"]}
        session.save()

        mock_get_security_audit.return_value = {"items": [], "page": 1, "total": 0}
        response = self.client.get(reverse("operator-audit-security-view"), {"preset": "login_failures_today"})

        self.assertEqual(200, response.status_code)
        _, call_query = mock_get_security_audit.call_args[0]
        self.assertEqual("failure", call_query["status"])

    @mock.patch("dashboard.views.get_security_audit")
    def test_operator_audit_security_view_exports_csv(self, mock_get_security_audit):
        session = self.client.session
        session["operator_access_token"] = "token"
        session["operator_access_expires_at"] = 32503680000
        session["operator_admin_profile"] = {"permissions": ["events:read"]}
        session.save()

        mock_get_security_audit.return_value = {
            "items": [{"event": "admin_login", "status": "success", "actor": "ops@example.com", "ipAddress": "127.0.0.1", "createdAtUtc": "2026-04-06T00:00:00Z"}],
            "page": 1,
            "total": 1,
        }
        response = self.client.get(reverse("operator-audit-security-view"), {"format": "csv"})

        self.assertEqual(200, response.status_code)
        self.assertEqual("text/csv; charset=utf-8", response["Content-Type"])
        self.assertIn("admin_login", response.content.decode())

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

    @mock.patch("dashboard.views.get_moderation_logs")
    def test_operator_moderation_logs_view_renders(self, mock_get_moderation_logs):
        session = self.client.session
        session["operator_access_token"] = "token"
        session["operator_access_expires_at"] = 32503680000
        session["operator_admin_profile"] = {"permissions": ["events:read"], "email": "ops@example.com"}
        session.save()

        mock_get_moderation_logs.return_value = {
            "items": [{"playerId": "p1", "status": 2, "reason": "abuse", "appliedBy": "ops@example.com"}],
            "page": 1,
            "total": 1,
        }
        response = self.client.get(reverse("operator-moderation-logs-view"), {"playerId": "p1"})

        self.assertEqual(200, response.status_code)
        self.assertContains(response, "Moderation Logs")
        self.assertContains(response, "abuse")

    def test_operator_moderation_logs_view_requires_login(self):
        response = self.client.get(reverse("operator-moderation-logs-view"))
        self.assertEqual(302, response.status_code)
        self.assertEqual(reverse("operator-login"), response.url)

    def test_operator_moderation_logs_view_requires_permission(self):
        session = self.client.session
        session["operator_access_token"] = "token"
        session["operator_access_expires_at"] = 32503680000
        session["operator_admin_profile"] = {"permissions": []}
        session.save()

        response = self.client.get(reverse("operator-moderation-logs-view"))
        self.assertEqual(403, response.status_code)

    @mock.patch("dashboard.views.get_moderation_logs")
    def test_operator_moderation_logs_view_preset(self, mock_get_moderation_logs):
        session = self.client.session
        session["operator_access_token"] = "token"
        session["operator_access_expires_at"] = 32503680000
        session["operator_admin_profile"] = {"permissions": ["events:read"]}
        session.save()

        mock_get_moderation_logs.return_value = {"items": [], "page": 1, "total": 0}
        response = self.client.get(reverse("operator-moderation-logs-view"), {"preset": "suspended"})

        self.assertEqual(200, response.status_code)
        _, call_query = mock_get_moderation_logs.call_args[0]
        self.assertEqual("2", call_query["status"])

    @mock.patch("dashboard.views.get_moderation_logs")
    def test_operator_moderation_logs_view_exports_csv(self, mock_get_moderation_logs):
        session = self.client.session
        session["operator_access_token"] = "token"
        session["operator_access_expires_at"] = 32503680000
        session["operator_admin_profile"] = {"permissions": ["events:read"]}
        session.save()

        mock_get_moderation_logs.return_value = {
            "items": [{"playerId": "p1", "status": 2, "reason": "abuse", "appliedBy": "ops@example.com", "createdAtUtc": "2026-04-06T00:00:00Z"}],
            "page": 1,
            "total": 1,
        }
        response = self.client.get(reverse("operator-moderation-logs-view"), {"format": "csv"})

        self.assertEqual(200, response.status_code)
        self.assertEqual("text/csv; charset=utf-8", response["Content-Type"])
        self.assertIn("p1", response.content.decode())

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


    @mock.patch("dashboard.views.create_upload_intent")
    def test_operator_media_intent(self, mock_create_upload_intent):
        session = self.client.session
        session["operator_access_token"] = "token"
        session["operator_access_expires_at"] = 32503680000
        session["operator_admin_profile"] = {"permissions": ["questions:write"]}
        session.save()

        mock_create_upload_intent.return_value = {"assetKey": "media/key"}
        response = self.client.post(
            reverse("operator-media-intent"),
            data={"fileName": "avatar.png", "contentType": "image/png", "sizeBytes": 1234},
        )

        self.assertEqual(200, response.status_code)
        self.assertEqual("media/key", response.json()["assetKey"])

    def test_operator_media_intent_requires_fields(self):
        session = self.client.session
        session["operator_access_token"] = "token"
        session["operator_access_expires_at"] = 32503680000
        session["operator_admin_profile"] = {"permissions": ["questions:write"]}
        session.save()

        response = self.client.post(reverse("operator-media-intent"), data={"fileName": "avatar.png"})
        self.assertEqual(422, response.status_code)

    @mock.patch("dashboard.views.create_upload_intent")
    def test_operator_media_intent_view_renders_intent(self, mock_create_upload_intent):
        session = self.client.session
        session["operator_access_token"] = "token"
        session["operator_access_expires_at"] = 32503680000
        session["operator_admin_profile"] = {"permissions": ["questions:write"], "email": "ops@example.com"}
        session.save()

        mock_create_upload_intent.return_value = {"assetKey": "media/k", "uploadUrl": "https://example/upload"}
        response = self.client.post(
            reverse("operator-media-intent-view"),
            data={"fileName": "avatar.png", "contentType": "image/png", "sizeBytes": 321},
        )

        self.assertEqual(200, response.status_code)
        self.assertContains(response, "Intent response")
        self.assertContains(response, "media/k")

    def test_operator_media_intent_view_requires_login(self):
        response = self.client.get(reverse("operator-media-intent-view"))
        self.assertEqual(302, response.status_code)
        self.assertEqual(reverse("operator-login"), response.url)

    def test_operator_media_intent_view_requires_permission(self):
        session = self.client.session
        session["operator_access_token"] = "token"
        session["operator_access_expires_at"] = 32503680000
        session["operator_admin_profile"] = {"permissions": []}
        session.save()

        response = self.client.get(reverse("operator-media-intent-view"))
        self.assertEqual(403, response.status_code)

    @mock.patch("dashboard.views.get_minio_diagnostics")
    def test_operator_minio_diagnostics(self, mock_get_minio_diagnostics):
        session = self.client.session
        session["operator_access_token"] = "token"
        session["operator_access_expires_at"] = 32503680000
        session["operator_admin_profile"] = {"permissions": ["users:read"]}
        session.save()

        mock_get_minio_diagnostics.return_value = {"overallStatus": "healthy", "checks": {}}
        response = self.client.get(reverse("operator-minio-diagnostics"))

        self.assertEqual(200, response.status_code)
        self.assertEqual("healthy", response.json()["overallStatus"])

    @mock.patch("dashboard.views.get_minio_diagnostics")
    def test_operator_minio_diagnostics_view_renders(self, mock_get_minio_diagnostics):
        session = self.client.session
        session["operator_access_token"] = "token"
        session["operator_access_expires_at"] = 32503680000
        session["operator_admin_profile"] = {"permissions": ["users:read"], "email": "ops@example.com"}
        session.save()

        mock_get_minio_diagnostics.return_value = {
            "baseUrl": "http://minio:9000",
            "overallStatus": "degraded",
            "checks": {
                "live": {"status": "healthy", "httpStatus": 200, "latencyMs": 12.3},
                "ready": {"status": "degraded", "httpStatus": 503, "latencyMs": 40.1},
            },
        }

        response = self.client.get(reverse("operator-minio-diagnostics-view"))

        self.assertEqual(200, response.status_code)
        self.assertContains(response, "MinIO Diagnostics")
        self.assertContains(response, "Recommended actions")
        self.assertContains(response, "degraded")

    def test_operator_minio_diagnostics_view_requires_permission(self):
        session = self.client.session
        session["operator_access_token"] = "token"
        session["operator_access_expires_at"] = 32503680000
        session["operator_admin_profile"] = {"permissions": []}
        session.save()

        response = self.client.get(reverse("operator-minio-diagnostics-view"))
        self.assertEqual(403, response.status_code)

    def test_operator_minio_diagnostics_view_redirects_to_login_when_not_authenticated(self):
        response = self.client.get(reverse("operator-minio-diagnostics-view"))
        self.assertEqual(302, response.status_code)
        self.assertEqual(reverse("operator-login"), response.url)

    # ── UX hardening: table density toggle ────────────────────────────────────

    @mock.patch("dashboard.views.list_admin_users")
    def test_users_view_renders_density_toggle(self, mock_list_admin_users):
        """Users table should include the density-toggle widget."""
        session = self.client.session
        session["operator_access_token"] = "token"
        session["operator_access_expires_at"] = 32503680000
        session["operator_admin_profile"] = {"permissions": ["users:read"], "email": "ops@example.com"}
        session.save()

        mock_list_admin_users.return_value = {"items": [], "total": 0}
        response = self.client.get(reverse("operator-users-view"))

        self.assertEqual(200, response.status_code)
        self.assertContains(response, 'data-density-target="users-table"')
        self.assertContains(response, 'data-density="compact"')
        self.assertContains(response, 'data-density="comfortable"')

    @mock.patch("dashboard.views.get_security_audit")
    def test_audit_security_view_renders_density_toggle(self, mock_get_security_audit):
        """Audit security table should include the density-toggle widget."""
        session = self.client.session
        session["operator_access_token"] = "token"
        session["operator_access_expires_at"] = 32503680000
        session["operator_admin_profile"] = {"permissions": ["events:read"], "email": "ops@example.com"}
        session.save()

        mock_get_security_audit.return_value = {"items": [], "total": 0}
        response = self.client.get(reverse("operator-audit-security-view"))

        self.assertEqual(200, response.status_code)
        self.assertContains(response, 'data-density-target="audit-table"')

    @mock.patch("dashboard.views.get_moderation_logs")
    def test_moderation_logs_view_renders_density_toggle(self, mock_get_moderation_logs):
        """Moderation logs table should include the density-toggle widget."""
        session = self.client.session
        session["operator_access_token"] = "token"
        session["operator_access_expires_at"] = 32503680000
        session["operator_admin_profile"] = {"permissions": ["events:read"], "email": "ops@example.com"}
        session.save()

        mock_get_moderation_logs.return_value = {"items": [], "total": 0}
        response = self.client.get(reverse("operator-moderation-logs-view"))

        self.assertEqual(200, response.status_code)
        self.assertContains(response, 'data-density-target="moderation-table"')

    # ── UX hardening: inline form validation markup ────────────────────────────

    @mock.patch("dashboard.views.list_admin_users")
    def test_users_view_bulk_form_has_validation_attributes(self, mock_list_admin_users):
        """Bulk-action form should carry data-validate and field-error spans."""
        session = self.client.session
        session["operator_access_token"] = "token"
        session["operator_access_expires_at"] = 32503680000
        session["operator_admin_profile"] = {"permissions": ["users:read", "users:write"], "email": "ops@example.com"}
        session.save()

        mock_list_admin_users.return_value = {"items": [], "total": 0}
        response = self.client.get(reverse("operator-users-view"))

        self.assertEqual(200, response.status_code)
        self.assertContains(response, 'data-validate="1"')
        self.assertContains(response, 'class="field-error"')
        self.assertContains(response, 'class="field-hint"')

    @mock.patch("dashboard.views.get_economy_history")
    def test_economy_player_grant_form_has_validation_attributes(self, mock_list_history):
        """Economy grant form should carry data-validate and field hints."""
        session = self.client.session
        session["operator_access_token"] = "token"
        session["operator_access_expires_at"] = 32503680000
        session["operator_admin_profile"] = {"permissions": ["economy:read"], "email": "ops@example.com"}
        session.save()

        mock_list_history.return_value = {"items": [], "total": 0, "page": 1}
        response = self.client.get(reverse("economy-player-view"), {"playerId": "test-player-id"})

        self.assertEqual(200, response.status_code)
        self.assertContains(response, 'data-validate="1"')
        self.assertContains(response, "support ticket")

    # ── UX hardening: operator.js is loaded by base template ──────────────────

    @mock.patch("dashboard.views.list_service_statuses")
    @mock.patch("dashboard.views.get_overall_status")
    def test_base_template_loads_operator_js(self, mock_overall_status, mock_list_service_statuses):
        """Every authenticated page should load operator.js for progressive enhancement."""
        session = self.client.session
        session["operator_access_token"] = "token"
        session["operator_access_expires_at"] = 32503680000
        session["operator_admin_profile"] = {"email": "ops@example.com", "permissions": ["users:read"]}
        session.save()

        mock_list_service_statuses.return_value = []
        mock_overall_status.return_value = "healthy"

        response = self.client.get(reverse("dashboard-home"))

        self.assertEqual(200, response.status_code)
        self.assertContains(response, "operator.js")


class PersonalizationDashboardViewsTests(TestCase):
    def _login(self, permissions):
        session = self.client.session
        session["operator_access_token"] = "token"
        session["operator_access_expires_at"] = 32503680000
        session["operator_admin_profile"] = {"permissions": permissions, "email": "ops@example.com"}
        session.save()

    def test_personalization_overview_requires_login(self):
        response = self.client.get(reverse("personalization-overview-view"))
        self.assertEqual(302, response.status_code)
        self.assertEqual(reverse("operator-login"), response.url)

    def test_personalization_overview_requires_read_permission(self):
        self._login([])
        response = self.client.get(reverse("personalization-overview-view"))
        self.assertEqual(403, response.status_code)

    @mock.patch("dashboard.views.get_recommendation_performance")
    @mock.patch("dashboard.views.get_personalization_archetypes")
    @mock.patch("dashboard.views.get_personalization_summary")
    def test_personalization_overview_renders_data(self, mock_summary, mock_archetypes, mock_performance):
        self._login(["personalization:read"])
        mock_summary.return_value = {
            "totalProfiles": 7,
            "highChurnRiskCount": 2,
            "highFrustrationRiskCount": 1,
            "generatedAt": "2026-05-12T00:00:00Z",
        }
        mock_archetypes.return_value = [{"archetype": "new_player", "count": 4}]
        mock_performance.return_value = [{"type": "mission", "total": 5, "accepted": 3, "dismissed": 1, "pending": 1}]

        response = self.client.get(reverse("personalization-overview-view"))

        self.assertEqual(200, response.status_code)
        self.assertContains(response, "Personalization overview")
        self.assertContains(response, "new_player")
        self.assertContains(response, "mission")

    def test_personalization_player_requires_read_permission(self):
        self._login([])
        response = self.client.get(reverse("personalization-player-view"))
        self.assertEqual(403, response.status_code)

    def test_personalization_player_without_player_id_renders_lookup(self):
        self._login(["personalization:read"])
        response = self.client.get(reverse("personalization-player-view"))
        self.assertEqual(200, response.status_code)
        self.assertContains(response, "Enter a player ID")

    @mock.patch("dashboard.views.get_player_debug")
    @mock.patch("dashboard.views.get_player_profile")
    def test_personalization_player_renders_profile_and_debug(self, mock_profile, mock_debug):
        self._login(["personalization:read"])
        player_id = "00000000-0000-0000-0000-000000000001"
        mock_profile.return_value = {
            "playerId": player_id,
            "archetype": "at_risk",
            "churnRiskScore": 0.7,
            "frustrationRiskScore": 0.3,
            "confidenceLevel": 0.8,
            "preferences": {"pace": "fast"},
            "guardrails": {"pressure": "low"},
        }
        mock_debug.return_value = {
            "recentEvents": [{"eventType": "question_answered", "eventSource": "gameplay", "category": "Science"}],
            "recentAudit": [{"candidateType": "mission", "allowed": True, "createdAt": "2026-05-12T00:00:00Z"}],
        }

        response = self.client.get(reverse("personalization-player-view"), {"playerId": player_id})

        self.assertEqual(200, response.status_code)
        self.assertContains(response, "at_risk")
        self.assertContains(response, "question_answered")
        self.assertContains(response, "mission")

    def test_personalization_recalculate_requires_write_permission(self):
        self._login(["personalization:read"])
        response = self.client.post(
            reverse("personalization-player-recalculate", kwargs={"player_id": "p1"})
        )
        self.assertEqual(403, response.status_code)

    @mock.patch("dashboard.views.recalculate_player")
    def test_personalization_recalculate_posts_and_redirects(self, mock_recalculate):
        self._login(["personalization:write"])
        response = self.client.post(
            reverse("personalization-player-recalculate", kwargs={"player_id": "p1"})
        )
        self.assertEqual(302, response.status_code)
        self.assertEqual("/personalization/player?playerId=p1", response.url)
        mock_recalculate.assert_called_once_with("token", "p1")

    @mock.patch("dashboard.views.reset_player")
    def test_personalization_reset_posts_and_redirects(self, mock_reset):
        self._login(["personalization:write"])
        response = self.client.post(
            reverse("personalization-player-reset", kwargs={"player_id": "p1"})
        )
        self.assertEqual(302, response.status_code)
        self.assertEqual("/personalization/player?playerId=p1", response.url)
        mock_reset.assert_called_once_with("token", "p1")

    @mock.patch("dashboard.views.list_rules")
    def test_personalization_rules_renders_json_editor(self, mock_list_rules):
        self._login(["personalization:read"])
        mock_list_rules.return_value = [
            {
                "ruleKey": "fatigue",
                "description": "Cap notification pressure",
                "isEnabled": True,
                "rule": {"maxScore": 0.65},
                "updatedAt": "2026-05-12T00:00:00Z",
            }
        ]

        response = self.client.get(reverse("personalization-rules-view"))

        self.assertEqual(200, response.status_code)
        self.assertContains(response, "Guardrail rules")
        self.assertContains(response, "fatigue")
        self.assertContains(response, "maxScore")

    def test_personalization_rule_upsert_requires_write_permission(self):
        self._login(["personalization:read"])
        response = self.client.post(
            reverse("personalization-rule-upsert", kwargs={"rule_key": "fatigue"}),
            data={"ruleJson": "{}", "isEnabled": "on"},
        )
        self.assertEqual(403, response.status_code)

    @mock.patch("dashboard.views.upsert_rule")
    def test_personalization_rule_upsert_valid_json_calls_backend(self, mock_upsert):
        self._login(["personalization:write"])
        response = self.client.post(
            reverse("personalization-rule-upsert", kwargs={"rule_key": "fatigue"}),
            data={"ruleJson": '{"maxScore": 0.65}', "isEnabled": "on"},
        )

        self.assertEqual(302, response.status_code)
        self.assertEqual("/personalization/rules", response.url)
        mock_upsert.assert_called_once_with("token", "fatigue", {"isEnabled": True, "rule": {"maxScore": 0.65}})

    @mock.patch("dashboard.views.upsert_rule")
    def test_personalization_rule_upsert_invalid_json_does_not_call_backend(self, mock_upsert):
        self._login(["personalization:write"])
        response = self.client.post(
            reverse("personalization-rule-upsert", kwargs={"rule_key": "fatigue"}),
            data={"ruleJson": "{not-json}", "isEnabled": "on"},
        )

        self.assertEqual(302, response.status_code)
        self.assertEqual("/personalization/rules", response.url)
        mock_upsert.assert_not_called()
