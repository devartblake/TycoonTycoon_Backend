from datetime import timedelta
from unittest import mock

import httpx
from django.test import TestCase
from django.urls import reverse
from django.utils import timezone

from dashboard.models import OperatorSavedViewAuditEvent, ProbeCheckRecord
from dashboard.services.admin_auth_client import AdminAuthConfigurationError, KmsUnavailableError
from dashboard.views import _build_probe_history_context, _format_ago, _normalize_audit_rows


class OperatorDetailDrilldownViewsTests(TestCase):
    def _login(self, permissions):
        session = self.client.session
        session["operator_access_token"] = "token"
        session["operator_access_expires_at"] = 32503680000
        session["operator_admin_profile"] = {"permissions": permissions, "email": "ops@example.com"}
        session.save()

    def test_user_detail_redirects_to_login_when_not_authenticated(self):
        response = self.client.get(reverse("operator-user-detail-view", kwargs={"user_id": "u1"}))
        self.assertEqual(302, response.status_code)
        self.assertEqual(reverse("operator-login"), response.url)

    def test_user_detail_requires_users_read(self):
        self._login([])
        response = self.client.get(reverse("operator-user-detail-view", kwargs={"user_id": "u1"}))
        self.assertEqual(403, response.status_code)

    @mock.patch("dashboard.views.get_admin_user_activity")
    @mock.patch("dashboard.views.get_admin_user")
    def test_user_detail_renders_profile_and_activity(self, mock_get_user, mock_activity):
        self._login(["users:read"])
        mock_get_user.return_value = {"id": "u1", "email": "user@example.com", "username": "User One", "isBanned": False}
        mock_activity.return_value = {"items": [{"type": "LOGIN", "description": "Signed in", "createdAt": "2026-05-18T00:00:00Z"}]}

        response = self.client.get(reverse("operator-user-detail-view", kwargs={"user_id": "u1"}))

        self.assertEqual(200, response.status_code)
        self.assertContains(response, "User detail")
        self.assertContains(response, "user@example.com")
        self.assertContains(response, "Signed in")
        self.assertContains(response, "ops@example.com")
        self.assertContains(response, "account-summary-grid")

    @mock.patch("dashboard.views.get_admin_user_activity")
    @mock.patch("dashboard.views.get_admin_user")
    def test_user_detail_investigation_link_carries_return_target(self, mock_get_user, mock_activity):
        self._login(["users:read"])
        long_id = "usr_03b0b24d124949d7b0ae42bd56825e26"
        mock_get_user.return_value = {
            "id": long_id,
            "email": "admin.with.a.long.name@example.tycoon.local",
            "username": "User One",
            "isBanned": False,
        }
        mock_activity.return_value = {"items": []}

        response = self.client.get(reverse("operator-user-detail-view", kwargs={"user_id": long_id}))

        self.assertEqual(200, response.status_code)
        self.assertContains(response, "breakable-token")
        self.assertContains(response, long_id)
        self.assertContains(response, "admin.with.a.long.name@example.tycoon.local")
        self.assertContains(response, "returnTo=/users/usr_03b0b24d124949d7b0ae42bd56825e26")

    @mock.patch("dashboard.views.update_admin_user")
    def test_user_detail_update_requires_write_permission(self, mock_update):
        self._login(["users:read"])
        response = self.client.post(reverse("operator-user-detail-view", kwargs={"user_id": "u1"}), data={"action": "update", "username": "New"})
        self.assertEqual(403, response.status_code)
        mock_update.assert_not_called()

    @mock.patch("dashboard.views.update_admin_user")
    def test_user_detail_update_calls_backend(self, mock_update):
        self._login(["users:read", "users:write"])
        response = self.client.post(reverse("operator-user-detail-view", kwargs={"user_id": "u1"}), data={"action": "update", "username": "New"})
        self.assertEqual(302, response.status_code)
        mock_update.assert_called_once_with("token", "u1", {"username": "New"})

    @mock.patch("dashboard.views.get_question")
    def test_question_detail_renders_full_payload(self, mock_get_question):
        self._login(["questions:read"])
        mock_get_question.return_value = {
            "id": "q1",
            "text": "What is Synaptix?",
            "category": "science",
            "difficulty": "Easy",
            "status": "Pending",
            "correctOptionId": "A",
            "options": [{"id": "A", "text": "A platform"}, {"id": "B", "text": "A comet"}],
            "tags": ["alpha"],
        }

        response = self.client.get(reverse("question-detail-view", kwargs={"question_id": "q1"}))

        self.assertEqual(200, response.status_code)
        self.assertContains(response, "Question detail")
        self.assertContains(response, "What is Synaptix?")
        self.assertContains(response, "A platform")

    @mock.patch("dashboard.views.update_question")
    def test_question_detail_update_calls_backend(self, mock_update):
        self._login(["questions:read", "questions:write"])
        response = self.client.post(
            reverse("question-detail-view", kwargs={"question_id": "q1"}),
            data={
                "text": "Question?",
                "category": "science",
                "difficulty": "2",
                "status": "Pending",
                "correctOptionId": "A",
                "option1Id": "A",
                "option1Text": "Alpha",
                "option2Id": "B",
                "option2Text": "Beta",
                "tags": "alpha,beta",
            },
        )

        self.assertEqual(302, response.status_code)
        payload = mock_update.call_args[0][2]
        self.assertEqual("Question?", payload["text"])
        self.assertEqual(["alpha", "beta"], payload["tags"])

    @mock.patch("dashboard.views.get_moderation_logs")
    @mock.patch("dashboard.views.get_moderation_profile")
    def test_moderation_player_renders_profile_and_logs(self, mock_profile, mock_logs):
        self._login(["events:read"])
        mock_profile.return_value = {"playerId": "p1", "status": 2, "reason": "policy"}
        mock_logs.return_value = {"items": [{"id": "l1", "playerId": "p1", "reason": "policy", "newStatus": 2}]}

        response = self.client.get(reverse("operator-moderation-player-view", kwargs={"player_id": "p1"}))

        self.assertEqual(200, response.status_code)
        self.assertContains(response, "Player moderation")
        self.assertContains(response, "policy")

    @mock.patch("dashboard.views.set_moderation_status")
    def test_moderation_player_update_requires_write_permission(self, mock_set_status):
        self._login(["events:read"])
        response = self.client.post(reverse("operator-moderation-player-view", kwargs={"player_id": "p1"}), data={"status": "2", "reason": "policy"})
        self.assertEqual(403, response.status_code)
        mock_set_status.assert_not_called()

    @mock.patch("dashboard.views.get_moderation_log")
    def test_moderation_log_detail_renders_payload(self, mock_log):
        self._login(["events:read"])
        mock_log.return_value = {"id": "l1", "playerId": "p1", "reason": "policy", "notes": "notes"}

        response = self.client.get(reverse("operator-moderation-log-detail-view", kwargs={"log_id": "l1"}))

        self.assertEqual(200, response.status_code)
        self.assertContains(response, "Moderation log detail")
        self.assertContains(response, "notes")

    @mock.patch("dashboard.views.get_security_audit_event")
    def test_security_audit_detail_renders_metadata(self, mock_event):
        self._login(["events:read"])
        mock_event.return_value = {"id": "e1", "title": "admin_auth_login", "status": "success", "metadata": {"email": "ops@example.com"}}

        response = self.client.get(reverse("operator-audit-security-detail-view", kwargs={"event_id": "e1"}))

        self.assertEqual(200, response.status_code)
        self.assertContains(response, "Event details")
        self.assertContains(response, "admin_auth_login")
        self.assertContains(response, "ops@example.com")

    @mock.patch("dashboard.views.unban_admin_user")
    @mock.patch("dashboard.views.ban_admin_user")
    @mock.patch("dashboard.views.update_admin_user")
    @mock.patch("dashboard.views.get_player_stock")
    @mock.patch("dashboard.views.get_player_debug")
    @mock.patch("dashboard.views.get_player_profile")
    @mock.patch("dashboard.views.get_economy_history")
    @mock.patch("dashboard.views.get_moderation_profile")
    @mock.patch("dashboard.views.get_admin_user_activity")
    @mock.patch("dashboard.views.get_admin_user")
    def test_investigation_workbench_navigation_and_readonly_policy(
        self,
        mock_get_admin_user,
        mock_get_admin_user_activity,
        mock_get_moderation_profile,
        mock_get_economy_history,
        mock_get_player_profile,
        mock_get_player_debug,
        mock_get_player_stock,
        mock_update_admin_user,
        mock_ban_admin_user,
        mock_unban_admin_user,
    ):
        self._login(["users:read", "moderation:read", "economy:read", "personalization:read", "store:read"])
        user_id = "usr_03b0b24d124949d7b0ae42bd56825e26"
        return_to = f"/users/{user_id}"
        mock_get_admin_user.return_value = {"id": user_id, "email": "user@example.com", "role": "user", "isVerified": True, "isBanned": False}
        mock_get_admin_user_activity.return_value = {"items": []}
        mock_get_moderation_profile.return_value = {"status": "clear"}
        mock_get_economy_history.return_value = {"items": []}
        mock_get_player_profile.return_value = {}
        mock_get_player_debug.return_value = {"recentEvents": [], "recentAudit": []}
        mock_get_player_stock.return_value = {"items": []}

        response = self.client.get(
            reverse("operator-user-investigation-view", kwargs={"user_id": user_id}),
            {"returnTo": return_to, "playerId": "00000000-0000-0000-0000-000000000001", "activityPageSize": "50"},
        )

        self.assertEqual(200, response.status_code)
        self.assertContains(response, "Back to user detail")
        self.assertContains(response, "Back to users")
        self.assertContains(response, "Cancel")
        self.assertContains(response, f'href="{return_to}"')
        self.assertContains(response, f'name="returnTo" value="{return_to}"')
        self.assertContains(response, "Editable actions")
        self.assertContains(response, "This workbench is read-only")
        self.assertContains(response, "readonly")
        mock_update_admin_user.assert_not_called()
        mock_ban_admin_user.assert_not_called()
        mock_unban_admin_user.assert_not_called()


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

    def test_operator_user_investigation_redirects_to_login_when_not_authenticated(self):
        response = self.client.get(reverse("operator-user-investigation-view", kwargs={"user_id": "u1"}))
        self.assertEqual(302, response.status_code)
        self.assertEqual(reverse("operator-login"), response.url)

    def test_operator_user_investigation_requires_users_read(self):
        session = self.client.session
        session["operator_access_token"] = "token"
        session["operator_access_expires_at"] = 32503680000
        session["operator_admin_profile"] = {"permissions": []}
        session.save()

        response = self.client.get(reverse("operator-user-investigation-view", kwargs={"user_id": "u1"}))
        self.assertEqual(403, response.status_code)

    @mock.patch("dashboard.views.get_player_stock")
    @mock.patch("dashboard.views.get_player_debug")
    @mock.patch("dashboard.views.get_player_profile")
    @mock.patch("dashboard.views.get_economy_history")
    @mock.patch("dashboard.views.get_moderation_profile")
    @mock.patch("dashboard.views.get_admin_user_activity")
    @mock.patch("dashboard.views.get_admin_user")
    def test_operator_user_investigation_renders_cross_surface_details(
        self,
        mock_get_admin_user,
        mock_get_admin_user_activity,
        mock_get_moderation_profile,
        mock_get_economy_history,
        mock_get_player_profile,
        mock_get_player_debug,
        mock_get_player_stock,
    ):
        player_id = "00000000-0000-0000-0000-000000000001"
        session = self.client.session
        session["operator_access_token"] = "token"
        session["operator_access_expires_at"] = 32503680000
        session["operator_admin_profile"] = {
            "permissions": [
                "users:read",
                "moderation:read",
                "economy:read",
                "personalization:read",
                "store:read",
            ],
            "email": "ops@example.com",
        }
        session.save()

        mock_get_admin_user.return_value = {
            "id": player_id,
            "email": "player@example.com",
            "role": "player",
            "isVerified": True,
            "isBanned": False,
        }
        mock_get_admin_user_activity.return_value = {
            "items": [{"type": "login", "status": "success", "ipAddress": "127.0.0.1", "createdAt": "2026-05-13T00:00:00Z"}]
        }
        mock_get_moderation_profile.return_value = {"status": "clear"}
        mock_get_economy_history.return_value = {"items": [{"type": "grant", "amount": 25, "reason": "support"}]}
        mock_get_player_profile.return_value = {"archetype": "Explorer", "churnRiskScore": 0.1, "frustrationRiskScore": 0.2}
        mock_get_player_debug.return_value = {
            "recentEvents": [{"eventType": "question_answered", "eventSource": "quiz", "category": "science"}],
            "recentAudit": [{"finalDecision": "allow", "candidateType": "store_offer", "allowed": True}],
        }
        mock_get_player_stock.return_value = {"items": [{"sku": "powerup:skip", "quantityUsed": 1, "remaining": 2}]}

        response = self.client.get(reverse("operator-user-investigation-view", kwargs={"user_id": player_id}))

        self.assertEqual(200, response.status_code)
        self.assertContains(response, "Investigation workbench")
        self.assertContains(response, "player@example.com")
        self.assertContains(response, "Explorer")
        self.assertContains(response, "powerup:skip")
        mock_get_moderation_profile.assert_called_once_with("token", player_id)
        mock_get_player_stock.assert_called_once_with("token", player_id)

    @mock.patch("dashboard.views.get_player_stock")
    @mock.patch("dashboard.views.get_player_debug")
    @mock.patch("dashboard.views.get_player_profile")
    @mock.patch("dashboard.views.get_economy_history")
    @mock.patch("dashboard.views.get_moderation_profile")
    @mock.patch("dashboard.views.get_admin_user_activity")
    @mock.patch("dashboard.views.get_admin_user")
    def test_operator_user_investigation_skips_optional_sections_without_permissions(
        self,
        mock_get_admin_user,
        mock_get_admin_user_activity,
        mock_get_moderation_profile,
        mock_get_economy_history,
        mock_get_player_profile,
        mock_get_player_debug,
        mock_get_player_stock,
    ):
        session = self.client.session
        session["operator_access_token"] = "token"
        session["operator_access_expires_at"] = 32503680000
        session["operator_admin_profile"] = {"permissions": ["users:read"], "email": "ops@example.com"}
        session.save()
        mock_get_admin_user.return_value = {"id": "u1", "email": "player@example.com"}
        mock_get_admin_user_activity.return_value = {"items": []}

        response = self.client.get(reverse("operator-user-investigation-view", kwargs={"user_id": "u1"}))

        self.assertEqual(200, response.status_code)
        self.assertContains(response, "Missing permission: moderation:read")
        self.assertContains(response, "Missing permission: economy:read")
        mock_get_moderation_profile.assert_not_called()
        mock_get_economy_history.assert_not_called()
        mock_get_player_profile.assert_not_called()
        mock_get_player_debug.assert_not_called()
        mock_get_player_stock.assert_not_called()

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


class NotificationsViewsTests(TestCase):
    def _login(self, permissions):
        session = self.client.session
        session["operator_access_token"] = "token"
        session["operator_access_expires_at"] = 32503680000
        session["operator_admin_profile"] = {"email": "ops@example.com", "permissions": permissions}
        session.save()

    def test_notifications_page_requires_login(self):
        response = self.client.get(reverse("notifications-view"))
        self.assertEqual(302, response.status_code)
        self.assertEqual(reverse("operator-login"), response.url)

    def test_notifications_page_requires_read_permission(self):
        self._login([])
        response = self.client.get(reverse("notifications-view"))
        self.assertEqual(403, response.status_code)

    def test_notification_mutation_requires_write_permission(self):
        self._login(["notifications:read"])
        response = self.client.post(reverse("notifications-send"), data={})
        self.assertEqual(403, response.status_code)

    @mock.patch("dashboard.views.get_dead_letter")
    @mock.patch("dashboard.views.get_notification_history")
    @mock.patch("dashboard.views.list_templates")
    @mock.patch("dashboard.views.list_scheduled")
    @mock.patch("dashboard.views.list_channels")
    def test_notifications_page_renders_admin_sections(
        self,
        mock_channels,
        mock_scheduled,
        mock_templates,
        mock_history,
        mock_dead_letter,
    ):
        self._login(["notifications:read"])
        mock_channels.return_value = [{"key": "admin_basic", "name": "Admin Basic", "importance": "high", "enabled": True}]
        mock_scheduled.return_value = {"items": [{"scheduleId": "sch_1", "channelKey": "admin_basic", "title": "Later", "scheduledAt": "2026-05-13T12:00:00Z", "status": "scheduled"}], "totalItems": 1}
        mock_templates.return_value = [{"templateId": "tpl_1", "name": "Winback", "title": "Hi", "body": "Body", "channelKey": "admin_basic", "variables": ["playerName"]}]
        mock_history.return_value = {"items": [{"id": "job_1", "channelKey": "admin_basic", "title": "Hello", "status": "queued", "createdAt": "2026-05-13T00:00:00Z"}], "totalItems": 1}
        mock_dead_letter.return_value = {"items": [{"scheduleId": "sch_dead", "channelKey": "admin_basic", "title": "Failed", "scheduledAt": "2026-05-13T00:00:00Z"}], "totalItems": 1}

        response = self.client.get(reverse("notifications-view"), data={"channelKey": "admin_basic", "status": "queued"})

        self.assertEqual(200, response.status_code)
        self.assertContains(response, "Schedule notification")
        self.assertContains(response, "Template library")
        self.assertContains(response, "Scheduled queue")
        self.assertContains(response, "sch_1")
        self.assertContains(response, "tpl_1")
        mock_history.assert_called_once_with(
            "token",
            {"from": None, "to": None, "channelKey": "admin_basic", "status": "queued", "page": 1, "pageSize": 25},
        )

    @mock.patch("dashboard.views.send_notification")
    def test_send_notification_valid_payload_calls_backend(self, mock_send):
        self._login(["notifications:write"])
        response = self.client.post(
            reverse("notifications-send"),
            data={
                "channelKey": "admin_basic",
                "title": "Hello",
                "body": "Body",
                "audience": '{"type":"all"}',
                "payload": '{"deepLink":"/home"}',
            },
        )

        self.assertEqual(302, response.status_code)
        self.assertEqual("/operations/notifications", response.url)
        mock_send.assert_called_once_with(
            "token",
            {
                "channelKey": "admin_basic",
                "title": "Hello",
                "body": "Body",
                "audience": {"type": "all"},
                "payload": {"deepLink": "/home"},
            },
        )

    @mock.patch("dashboard.views.send_notification")
    def test_send_notification_invalid_json_does_not_call_backend(self, mock_send):
        self._login(["notifications:write"])
        response = self.client.post(
            reverse("notifications-send"),
            data={"channelKey": "admin_basic", "title": "Hello", "body": "Body", "audience": "{bad-json}"},
        )

        self.assertEqual(302, response.status_code)
        mock_send.assert_not_called()

    @mock.patch("dashboard.views.schedule_notification")
    def test_schedule_notification_valid_payload_calls_backend(self, mock_schedule):
        self._login(["notifications:write"])
        response = self.client.post(
            reverse("notifications-schedule"),
            data={
                "channelKey": "admin_basic",
                "title": "Later",
                "body": "Body",
                "scheduledAt": "2026-05-13T12:00",
                "audience": '{"type":"all"}',
                "repeat": '{"interval":"daily"}',
            },
        )

        self.assertEqual(302, response.status_code)
        mock_schedule.assert_called_once_with(
            "token",
            {
                "channelKey": "admin_basic",
                "title": "Later",
                "body": "Body",
                "scheduledAt": "2026-05-13T12:00",
                "audience": {"type": "all"},
                "repeat": {"interval": "daily"},
            },
        )

    @mock.patch("dashboard.views.schedule_notification")
    def test_schedule_notification_invalid_repeat_does_not_call_backend(self, mock_schedule):
        self._login(["notifications:write"])
        response = self.client.post(
            reverse("notifications-schedule"),
            data={
                "channelKey": "admin_basic",
                "title": "Later",
                "body": "Body",
                "scheduledAt": "2026-05-13T12:00",
                "audience": '{"type":"all"}',
                "repeat": "[1,2]",
            },
        )

        self.assertEqual(302, response.status_code)
        mock_schedule.assert_not_called()

    @mock.patch("dashboard.views.cancel_scheduled")
    def test_cancel_scheduled_calls_backend(self, mock_cancel):
        self._login(["notifications:write"])
        response = self.client.post(reverse("notifications-scheduled-cancel", kwargs={"schedule_id": "sch_1"}))

        self.assertEqual(302, response.status_code)
        mock_cancel.assert_called_once_with("token", "sch_1")

    @mock.patch("dashboard.views.upsert_channel")
    def test_upsert_channel_calls_backend(self, mock_upsert):
        self._login(["notifications:write"])
        response = self.client.post(
            reverse("notifications-channel-upsert"),
            data={"key": "admin_security", "name": "Admin Security", "description": "Alerts", "importance": "high", "enabled": "on"},
        )

        self.assertEqual(302, response.status_code)
        mock_upsert.assert_called_once_with(
            "token",
            "admin_security",
            {"name": "Admin Security", "description": "Alerts", "importance": "high", "enabled": True},
        )

    @mock.patch("dashboard.views.create_template")
    def test_create_template_calls_backend(self, mock_create):
        self._login(["notifications:write"])
        response = self.client.post(
            reverse("notifications-template-create"),
            data={"name": "Winback", "title": "Hi", "body": "Body", "channelKey": "admin_basic", "variables": "playerName\nrewardName"},
        )

        self.assertEqual(302, response.status_code)
        mock_create.assert_called_once_with(
            "token",
            {"name": "Winback", "title": "Hi", "body": "Body", "channelKey": "admin_basic", "variables": ["playerName", "rewardName"]},
        )

    @mock.patch("dashboard.views.update_template")
    def test_update_template_calls_backend(self, mock_update):
        self._login(["notifications:write"])
        response = self.client.post(
            reverse("notifications-template-update", kwargs={"template_id": "tpl_1"}),
            data={"name": "Winback", "title": "Hi", "body": "Body", "channelKey": "admin_basic", "variables": "playerName, rewardName"},
        )

        self.assertEqual(302, response.status_code)
        mock_update.assert_called_once_with(
            "token",
            "tpl_1",
            {"name": "Winback", "title": "Hi", "body": "Body", "channelKey": "admin_basic", "variables": ["playerName", "rewardName"]},
        )

    @mock.patch("dashboard.views.delete_template")
    def test_delete_template_calls_backend(self, mock_delete):
        self._login(["notifications:write"])
        response = self.client.post(reverse("notifications-template-delete", kwargs={"template_id": "tpl_1"}))

        self.assertEqual(302, response.status_code)
        mock_delete.assert_called_once_with("token", "tpl_1")


class StorePlayerStockViewsTests(TestCase):
    player_id = "00000000-0000-0000-0000-000000000001"

    def _login(self, permissions):
        session = self.client.session
        session["operator_access_token"] = "token"
        session["operator_access_expires_at"] = 32503680000
        session["operator_admin_profile"] = {"permissions": permissions, "email": "ops@example.com"}
        session.save()

    def test_player_stock_requires_login(self):
        response = self.client.get(reverse("store-player-stock-view"))
        self.assertEqual(302, response.status_code)
        self.assertEqual(reverse("operator-login"), response.url)

    def test_player_stock_requires_read_permission(self):
        self._login([])
        response = self.client.get(reverse("store-player-stock-view"))
        self.assertEqual(403, response.status_code)

    def test_player_stock_without_player_id_renders_lookup(self):
        self._login(["store:read"])
        response = self.client.get(reverse("store-player-stock-view"))
        self.assertEqual(200, response.status_code)
        self.assertContains(response, "Load player stock")

    @mock.patch("dashboard.views.get_player_stock")
    def test_player_stock_invalid_player_id_does_not_call_backend(self, mock_get_player_stock):
        self._login(["store:read"])
        response = self.client.get(reverse("store-player-stock-view"), {"playerId": "not-a-uuid"})
        self.assertEqual(200, response.status_code)
        self.assertContains(response, "Enter a valid player UUID.")
        mock_get_player_stock.assert_not_called()

    @mock.patch("dashboard.views.get_player_stock")
    def test_player_stock_renders_backend_rows(self, mock_get_player_stock):
        self._login(["store:read"])
        mock_get_player_stock.return_value = {
            "playerId": self.player_id,
            "items": [
                {
                    "sku": "powerup:skip",
                    "quantityUsed": 2,
                    "maxQuantity": 5,
                    "remaining": 3,
                    "effectiveMaxQuantity": 5,
                    "lastResetAtUtc": "2026-05-12T00:00:00Z",
                    "nextResetAtUtc": "2026-05-13T00:00:00Z",
                    "updatedAtUtc": "2026-05-12T00:00:00Z",
                }
            ],
        }

        response = self.client.get(reverse("store-player-stock-view"), {"playerId": self.player_id})

        self.assertEqual(200, response.status_code)
        self.assertContains(response, "powerup:skip")
        self.assertContains(response, "2026-05-13T00:00:00Z")
        mock_get_player_stock.assert_called_once_with("token", self.player_id)

    def test_player_stock_override_requires_write_permission(self):
        self._login(["store:read"])
        response = self.client.post(
            reverse("store-player-stock-override", kwargs={"player_id": self.player_id}),
            data={"sku": "powerup:skip", "effectiveMaxQuantity": "4"},
        )
        self.assertEqual(403, response.status_code)

    @mock.patch("dashboard.views.override_player_stock")
    def test_player_stock_override_valid_integer_calls_backend(self, mock_override):
        self._login(["store:write"])
        response = self.client.post(
            reverse("store-player-stock-override", kwargs={"player_id": self.player_id}),
            data={"sku": "POWERUP:SKIP", "effectiveMaxQuantity": "4", "reason": "ticket-1"},
        )

        self.assertEqual(302, response.status_code)
        self.assertEqual(f"/store/player-stock?playerId={self.player_id}", response.url)
        mock_override.assert_called_once_with("token", self.player_id, "powerup:skip", 4, "ticket-1")

    @mock.patch("dashboard.views.override_player_stock")
    def test_player_stock_override_blank_quantity_clears_override(self, mock_override):
        self._login(["store:write"])
        response = self.client.post(
            reverse("store-player-stock-override", kwargs={"player_id": self.player_id}),
            data={"sku": "powerup:skip", "effectiveMaxQuantity": "", "reason": ""},
        )

        self.assertEqual(302, response.status_code)
        mock_override.assert_called_once_with("token", self.player_id, "powerup:skip", None, "")

    @mock.patch("dashboard.views.override_player_stock")
    def test_player_stock_override_empty_sku_does_not_call_backend(self, mock_override):
        self._login(["store:write"])
        response = self.client.post(
            reverse("store-player-stock-override", kwargs={"player_id": self.player_id}),
            data={"sku": "", "effectiveMaxQuantity": "4"},
        )

        self.assertEqual(302, response.status_code)
        mock_override.assert_not_called()

    @mock.patch("dashboard.views.override_player_stock")
    def test_player_stock_override_invalid_quantity_does_not_call_backend(self, mock_override):
        self._login(["store:write"])
        response = self.client.post(
            reverse("store-player-stock-override", kwargs={"player_id": self.player_id}),
            data={"sku": "powerup:skip", "effectiveMaxQuantity": "-1"},
        )

        self.assertEqual(302, response.status_code)
        mock_override.assert_not_called()

    def test_bulk_reset_requires_write_permission(self):
        self._login(["store:read"])
        response = self.client.post(
            reverse("store-stock-policies-bulk-reset"),
            data={"skus": "powerup:skip"},
        )
        self.assertEqual(403, response.status_code)

    @mock.patch("dashboard.views.bulk_reset_stock")
    def test_bulk_reset_valid_skus_calls_backend(self, mock_bulk_reset):
        self._login(["store:write"])
        mock_bulk_reset.return_value = {"playersAffected": 2}

        response = self.client.post(
            reverse("store-stock-policies-bulk-reset"),
            data={"skus": "POWERUP:SKIP, powerup:hint\npowerup:skip", "reason": "rollout"},
        )

        self.assertEqual(302, response.status_code)
        self.assertEqual(reverse("store-player-stock-view"), response.url)
        mock_bulk_reset.assert_called_once_with("token", ["powerup:skip", "powerup:hint"], "rollout")

    @mock.patch("dashboard.views.bulk_reset_stock")
    def test_bulk_reset_empty_skus_does_not_call_backend(self, mock_bulk_reset):
        self._login(["store:write"])
        response = self.client.post(reverse("store-stock-policies-bulk-reset"), data={"skus": "  \n "})

        self.assertEqual(302, response.status_code)
        mock_bulk_reset.assert_not_called()


class StoreAnalyticsViewsTests(TestCase):
    def _login(self, permissions):
        session = self.client.session
        session["operator_access_token"] = "token"
        session["operator_access_expires_at"] = 32503680000
        session["operator_admin_profile"] = {"permissions": permissions, "email": "ops@example.com"}
        session.save()

    def test_store_analytics_requires_login(self):
        response = self.client.get(reverse("store-analytics-view"))
        self.assertEqual(302, response.status_code)
        self.assertEqual(reverse("operator-login"), response.url)

    def test_store_analytics_requires_read_permission(self):
        self._login([])
        response = self.client.get(reverse("store-analytics-view"))
        self.assertEqual(403, response.status_code)

    @mock.patch("dashboard.views.get_purchase_analytics")
    def test_store_analytics_renders_plotly_chart_and_table(self, mock_analytics):
        self._login(["store:read"])
        mock_analytics.return_value = {
            "totalPurchases": 1842,
            "totalCoinsSpent": 92000,
            "topSkus": [{"sku": "powerup:skip", "purchaseCount": 731}],
        }

        response = self.client.get(reverse("store-analytics-view"))

        self.assertEqual(200, response.status_code)
        self.assertContains(response, "Purchase analytics")
        self.assertContains(response, "plotly-graph-div")
        self.assertContains(response, "powerup:skip")
        self.assertContains(response, "Top SKUs")

    @mock.patch("dashboard.views.get_purchase_analytics")
    def test_store_analytics_empty_data_still_renders(self, mock_analytics):
        self._login(["store:read"])
        mock_analytics.return_value = {"totalPurchases": 0, "totalCoinsSpent": 0, "topSkus": []}

        response = self.client.get(reverse("store-analytics-view"))

        self.assertEqual(200, response.status_code)
        self.assertContains(response, "Summary")
        self.assertNotContains(response, "plotly-graph-div")


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
        self.assertContains(response, "plotly-graph-div")
        self.assertContains(response, "Recommendation performance")

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


class ProbeHistoryTests(TestCase):
    def _make_record(self, service_name, status, latency_ms, offset_minutes=0):
        ProbeCheckRecord.objects.create(
            service_name=service_name,
            status=status,
            latency_ms=latency_ms,
            detail="",
            checked_at=timezone.now() - timedelta(minutes=offset_minutes),
        )

    def test_format_ago_seconds(self):
        self.assertEqual("5s ago", _format_ago(5))

    def test_format_ago_minutes(self):
        self.assertEqual("3m ago", _format_ago(180))

    def test_format_ago_hours(self):
        self.assertEqual("2h ago", _format_ago(7200))

    def test_build_history_returns_unknown_bars_when_no_records(self):
        now = timezone.now()
        result = _build_probe_history_context("NoSuchService", now)
        self.assertEqual(12, len(result["bars"]))
        self.assertTrue(all(b["status"] == "unknown" for b in result["bars"]))
        self.assertIsNone(result["uptime_pct"])
        self.assertIsNone(result["p95_ms"])

    def test_build_history_uptime_all_healthy(self):
        for i in range(6):
            self._make_record(".NET API", "healthy", 50, offset_minutes=i * 30)
        now = timezone.now()
        result = _build_probe_history_context(".NET API", now)
        self.assertEqual(100.0, result["uptime_pct"])
        self.assertIsNone(result["degraded_since"])

    def test_build_history_degraded_since_set_when_consecutive_failures(self):
        # All recent records are degraded — should set degraded_since
        for i in range(3):
            self._make_record(".NET API", "degraded", 400, offset_minutes=i * 10)
        now = timezone.now()
        result = _build_probe_history_context(".NET API", now)
        self.assertIsNotNone(result["degraded_since"])
        self.assertIn("UTC", result["degraded_since"])

    def test_build_history_degraded_since_none_when_healthy_records_exist(self):
        # Mix: most recent is healthy → degraded_since should be None
        self._make_record(".NET API", "healthy", 30, offset_minutes=0)
        self._make_record(".NET API", "degraded", 400, offset_minutes=5)
        now = timezone.now()
        result = _build_probe_history_context(".NET API", now)
        self.assertIsNone(result["degraded_since"])

    def test_build_history_p95_from_small_sample(self):
        latencies = [100, 200, 300, 400, 500]
        for i, lat in enumerate(latencies):
            self._make_record(".NET API", "healthy", lat, offset_minutes=i)
        now = timezone.now()
        result = _build_probe_history_context(".NET API", now)
        self.assertIsNotNone(result["p95_ms"])
        self.assertIsInstance(result["p95_ms"], int)

    def test_build_history_bars_count(self):
        for i in range(12):
            self._make_record(".NET API", "healthy", 50, offset_minutes=i * 28)
        now = timezone.now()
        result = _build_probe_history_context(".NET API", now)
        self.assertEqual(12, len(result["bars"]))

    def test_build_history_last_checked_ago_populated(self):
        self._make_record(".NET API", "healthy", 50, offset_minutes=1)
        now = timezone.now()
        result = _build_probe_history_context(".NET API", now)
        self.assertIn("ago", result["last_checked_ago"])


class ProbeLogViewTests(TestCase):
    def _login(self):
        session = self.client.session
        session["operator_access_token"] = "token"
        session["operator_access_expires_at"] = 32503680000
        session["operator_admin_profile"] = {"permissions": [], "email": "ops@example.com"}
        session.save()

    def test_probe_log_requires_login(self):
        response = self.client.get(reverse("probe-log", kwargs={"service_slug": "dotnet"}))
        self.assertEqual(302, response.status_code)

    def test_probe_log_returns_404_for_unknown_slug(self):
        self._login()
        response = self.client.get(reverse("probe-log", kwargs={"service_slug": "unknown-svc"}))
        self.assertEqual(404, response.status_code)

    def test_probe_log_renders_empty_state(self):
        self._login()
        response = self.client.get(reverse("probe-log", kwargs={"service_slug": "dotnet"}))
        self.assertEqual(200, response.status_code)
        self.assertContains(response, ".NET API")
        self.assertContains(response, "No probe records")

    def test_probe_log_renders_records(self):
        ProbeCheckRecord.objects.create(
            service_name=".NET API",
            status="healthy",
            latency_ms=42,
            detail="Request succeeded",
            checked_at=timezone.now(),
        )
        self._login()
        response = self.client.get(reverse("probe-log", kwargs={"service_slug": "dotnet"}))
        self.assertEqual(200, response.status_code)
        self.assertContains(response, "42ms")
        self.assertContains(response, "healthy")


class NormalizeAuditRowsTests(TestCase):
    def test_extracts_actor_and_ip_from_metadata(self):
        rows = [{
            "title": "admin_auth_login",
            "status": "success",
            "createdAt": "2026-05-19T00:00:00Z",
            "metadata": {"actor": "admin@example.com", "ip": "1.2.3.4", "userAgent": "Mozilla/5.0"},
        }]
        result = _normalize_audit_rows(rows)
        self.assertEqual("admin@example.com", result[0]["actor"])
        self.assertEqual("1.2.3.4", result[0]["ipAddress"])
        self.assertEqual("Mozilla/5.0", result[0]["userAgent"])

    def test_falls_back_to_dash_when_no_metadata(self):
        rows = [{"title": "admin_auth_login", "status": "success", "createdAt": "2026-05-19T00:00:00Z"}]
        result = _normalize_audit_rows(rows)
        self.assertEqual("-", result[0]["actor"])
        self.assertEqual("-", result[0]["ipAddress"])
        self.assertEqual("-", result[0]["userAgent"])

    def test_falls_back_to_email_key_when_no_actor(self):
        rows = [{"metadata": {"email": "fallback@example.com"}}]
        result = _normalize_audit_rows(rows)
        self.assertEqual("fallback@example.com", result[0]["actor"])

    def test_returns_empty_list_for_none_input(self):
        self.assertEqual([], _normalize_audit_rows(None))


class GeoLookupViewTests(TestCase):
    def _login(self, permissions=None):
        session = self.client.session
        session["operator_access_token"] = "token"
        session["operator_access_expires_at"] = 32503680000
        session["operator_admin_profile"] = {"permissions": permissions or ["events:read"]}
        session.save()

    def test_requires_login(self):
        response = self.client.get(reverse("audit-geo-lookup") + "?ip=1.2.3.4")
        self.assertEqual(302, response.status_code)

    def test_returns_400_for_missing_ip(self):
        self._login()
        response = self.client.get(reverse("audit-geo-lookup"))
        self.assertEqual(400, response.status_code)

    def test_returns_400_for_placeholder_ip(self):
        self._login()
        response = self.client.get(reverse("audit-geo-lookup") + "?ip=-")
        self.assertEqual(400, response.status_code)

    @mock.patch("dashboard.views.httpx.get")
    def test_returns_geo_data_on_success(self, mock_get):
        self._login()
        geo_response = mock.Mock()
        geo_response.json.return_value = {
            "status": "success",
            "country": "United States",
            "countryCode": "US",
            "city": "Mountain View",
            "lat": 37.4,
            "lon": -122.0,
            "isp": "Google LLC",
            "proxy": False,
            "query": "8.8.8.8",
        }
        geo_response.raise_for_status.return_value = None
        mock_get.return_value = geo_response

        import dashboard.views as v
        v._GEO_CACHE.clear()

        response = self.client.get(reverse("audit-geo-lookup") + "?ip=8.8.8.8")
        self.assertEqual(200, response.status_code)
        data = response.json()
        self.assertEqual("United States", data["country"])
        self.assertEqual("Mountain View", data["city"])
        self.assertFalse(data["proxy"])
