"""
Full user-flow tests for the Django operator dashboard.

Covers the complete lifecycle of user management actions an operator
performs once logged in:

  1. Login / logout
  2. Users list  (browse, filter, saved views, bulk actions)
  3. User detail  (read, update, ban, unban – via both the page and the JSON APIs)
  4. User investigation workbench  (cross-surface, permission gating, error paths)
  5. Supporting JSON endpoints  (activity, update)

Each class is self-contained: the ``_login`` helper sets up a valid
session so tests can focus on the behaviour under test.
"""

from unittest import mock

import httpx
from django.test import TestCase
from django.urls import reverse


# ---------------------------------------------------------------------------
# Helpers
# ---------------------------------------------------------------------------

class _BaseUserFlowTests(TestCase):
    """Shared session helpers used by every test class in this module."""

    def _login(self, permissions=None, email="ops@example.com"):
        if permissions is None:
            permissions = ["users:read", "users:write"]
        session = self.client.session
        session["operator_access_token"] = "tok"
        session["operator_access_expires_at"] = 32503680000
        session["operator_admin_profile"] = {
            "email": email,
            "permissions": permissions,
        }
        session.save()


# ---------------------------------------------------------------------------
# 1. Login / Logout flow
# ---------------------------------------------------------------------------

class LoginLogoutFlowTests(_BaseUserFlowTests):
    """Tests for the login and logout views."""

    # ── GET login ─────────────────────────────────────────────────────────

    def test_login_get_renders_form_when_not_authenticated(self):
        response = self.client.get(reverse("operator-login"))
        self.assertEqual(200, response.status_code)
        self.assertContains(response, "<form")

    def test_login_get_redirects_to_dashboard_when_already_logged_in(self):
        self._login()
        response = self.client.get(reverse("operator-login"))
        self.assertEqual(302, response.status_code)
        self.assertEqual(reverse("dashboard-home"), response.url)

    # ── POST login – validation ────────────────────────────────────────────

    def test_login_post_missing_email_returns_400(self):
        response = self.client.post(
            reverse("operator-login"),
            data={"email": "", "password": "secret"},
        )
        self.assertEqual(400, response.status_code)

    def test_login_post_missing_password_returns_400(self):
        response = self.client.post(
            reverse("operator-login"),
            data={"email": "ops@example.com", "password": ""},
        )
        self.assertEqual(400, response.status_code)

    # ── POST login – backend errors ───────────────────────────────────────

    @mock.patch("dashboard.views.admin_login")
    def test_login_post_request_error_returns_503(self, mock_login):
        req = httpx.Request("POST", "http://backend/auth/login")
        mock_login.side_effect = httpx.ConnectError("refused", request=req)

        response = self.client.post(
            reverse("operator-login"),
            data={"email": "ops@example.com", "password": "secret"},
        )
        self.assertEqual(503, response.status_code)
        self.assertContains(response, "Unable to reach backend", status_code=503)

    @mock.patch("dashboard.views.admin_login")
    def test_login_post_503_admin_ops_key_not_configured(self, mock_login):
        req = httpx.Request("POST", "http://backend/auth/login")
        resp = httpx.Response(
            status_code=503,
            request=req,
            content=b"AdminOps key not configured",
        )
        mock_login.side_effect = httpx.HTTPStatusError("503", request=req, response=resp)

        response = self.client.post(
            reverse("operator-login"),
            data={"email": "ops@example.com", "password": "secret"},
        )
        self.assertEqual(503, response.status_code)
        self.assertContains(response, "admin ops key", status_code=503)

    @mock.patch("dashboard.views.admin_login")
    def test_login_post_generic_http_500_returns_502(self, mock_login):
        req = httpx.Request("POST", "http://backend/auth/login")
        resp = httpx.Response(status_code=500, request=req)
        mock_login.side_effect = httpx.HTTPStatusError("500", request=req, response=resp)

        response = self.client.post(
            reverse("operator-login"),
            data={"email": "ops@example.com", "password": "secret"},
        )
        self.assertEqual(502, response.status_code)
        self.assertContains(response, "Login failed", status_code=502)

    # ── Logout ────────────────────────────────────────────────────────────

    def test_logout_clears_session_and_redirects_to_login(self):
        self._login()
        # Confirm we are logged in.
        self.assertIsNotNone(self.client.session.get("operator_access_token"))

        response = self.client.get(reverse("operator-logout"))

        self.assertEqual(302, response.status_code)
        self.assertEqual(reverse("operator-login"), response.url)
        # Session should be cleared.
        self.assertIsNone(self.client.session.get("operator_access_token"))
        self.assertIsNone(self.client.session.get("operator_refresh_token"))
        self.assertIsNone(self.client.session.get("operator_admin_profile"))

    def test_logout_when_not_logged_in_still_redirects(self):
        response = self.client.get(reverse("operator-logout"))
        self.assertEqual(302, response.status_code)
        self.assertEqual(reverse("operator-login"), response.url)


# ---------------------------------------------------------------------------
# 2. Users list flow
# ---------------------------------------------------------------------------

class UsersListFlowTests(_BaseUserFlowTests):
    """Tests for the users list page (operator_users_view)."""

    @mock.patch("dashboard.views.list_admin_users")
    def _get_users_view(self, mock_list, permissions=None, params=None):
        self._login(permissions=permissions or ["users:read"])
        mock_list.return_value = {"items": [], "total": 0}
        return self.client.get(reverse("operator-users-view"), params or {})

    # ── Presets ───────────────────────────────────────────────────────────

    @mock.patch("dashboard.views.list_admin_users")
    def test_new_unverified_preset_filters_backend_call(self, mock_list_users):
        self._login(["users:read"])
        mock_list_users.return_value = {"items": [], "total": 0}
        response = self.client.get(reverse("operator-users-view"), {"preset": "new_unverified"})

        self.assertEqual(200, response.status_code)
        _, call_query = mock_list_users.call_args[0]
        self.assertEqual("false", call_query["isVerified"])
        self.assertEqual("createdAt", call_query["sortBy"])
        self.assertEqual("desc", call_query["sortOrder"])

    @mock.patch("dashboard.views.list_admin_users")
    def test_admins_preset_filters_backend_call(self, mock_list_users):
        self._login(["users:read"])
        mock_list_users.return_value = {"items": [], "total": 0}
        response = self.client.get(reverse("operator-users-view"), {"preset": "admins"})

        self.assertEqual(200, response.status_code)
        _, call_query = mock_list_users.call_args[0]
        self.assertEqual("admin", call_query["role"])
        self.assertEqual("updatedAt", call_query["sortBy"])

    # ── Bulk actions ──────────────────────────────────────────────────────

    @mock.patch("dashboard.views.list_admin_users")
    @mock.patch("dashboard.views.unban_admin_user")
    def test_bulk_unban_calls_backend_for_each_user(self, mock_unban, mock_list_users):
        self._login(["users:read", "users:write"])
        mock_list_users.return_value = {"items": [], "total": 0}

        response = self.client.post(
            reverse("operator-users-view"),
            data={"action": "unban", "confirm": "YES", "userIds": ["u1", "u2"]},
        )

        self.assertEqual(200, response.status_code)
        self.assertEqual(2, mock_unban.call_count)

    @mock.patch("dashboard.views.list_admin_users")
    @mock.patch("dashboard.views.ban_admin_user")
    def test_bulk_action_without_user_ids_shows_error(self, mock_ban, mock_list_users):
        self._login(["users:read", "users:write"])
        mock_list_users.return_value = {"items": [], "total": 0}

        response = self.client.post(
            reverse("operator-users-view"),
            data={"action": "ban", "confirm": "YES", "userIds": []},
        )

        self.assertEqual(200, response.status_code)
        mock_ban.assert_not_called()
        self.assertContains(response, "Select at least one user")

    @mock.patch("dashboard.views.list_admin_users")
    @mock.patch("dashboard.views.ban_admin_user")
    def test_bulk_action_without_confirmation_shows_error(self, mock_ban, mock_list_users):
        self._login(["users:read", "users:write"])
        mock_list_users.return_value = {"items": [], "total": 0}

        response = self.client.post(
            reverse("operator-users-view"),
            data={"action": "ban", "confirm": "", "userIds": ["u1"]},
        )

        self.assertEqual(200, response.status_code)
        mock_ban.assert_not_called()
        self.assertContains(response, "YES")

    @mock.patch("dashboard.views.list_admin_users")
    @mock.patch("dashboard.views.ban_admin_user")
    def test_unknown_bulk_action_shows_error(self, mock_ban, mock_list_users):
        self._login(["users:read", "users:write"])
        mock_list_users.return_value = {"items": [], "total": 0}

        response = self.client.post(
            reverse("operator-users-view"),
            data={"action": "delete", "confirm": "YES", "userIds": ["u1"]},
        )

        self.assertEqual(200, response.status_code)
        mock_ban.assert_not_called()
        self.assertContains(response, "Bulk action must be")

    # ── Saved views ───────────────────────────────────────────────────────

    @mock.patch("dashboard.views.list_admin_users")
    def test_save_view_with_empty_name_shows_error(self, mock_list_users):
        self._login(["users:read"])
        mock_list_users.return_value = {"items": [], "total": 0}

        response = self.client.post(
            reverse("operator-users-view"),
            data={"action": "save_view", "viewName": ""},
        )

        self.assertEqual(200, response.status_code)
        self.assertContains(response, "View name is required")

    @mock.patch("dashboard.views._delete_named_view")
    @mock.patch("dashboard.views.list_admin_users")
    def test_delete_view_not_found_shows_error(self, mock_list_users, mock_delete):
        self._login(["users:read"])
        mock_list_users.return_value = {"items": [], "total": 0}
        mock_delete.return_value = False

        response = self.client.post(
            reverse("operator-users-view"),
            data={"action": "delete_view", "viewName": "Missing"},
        )

        self.assertEqual(200, response.status_code)
        self.assertContains(response, "Saved view not found")

    @mock.patch("dashboard.views.list_admin_users")
    def test_delete_view_non_owned_shared_shows_error(self, mock_list_users):
        """Attempting to delete another operator's saved view shows an error."""
        self._login(["users:read"], email="ops@example.com")
        mock_list_users.return_value = {"items": [], "total": 0}

        response = self.client.post(
            reverse("operator-users-view"),
            data={"action": "delete_view", "viewName": "other@example.com::BannedView"},
        )

        self.assertEqual(200, response.status_code)
        self.assertContains(response, "Only the owner can delete")

    @mock.patch("dashboard.views.list_admin_users")
    def test_archive_view_non_owned_shared_shows_error(self, mock_list_users):
        self._login(["users:read"], email="ops@example.com")
        mock_list_users.return_value = {"items": [], "total": 0}

        response = self.client.post(
            reverse("operator-users-view"),
            data={"action": "archive_view", "viewName": "other@example.com::BannedView"},
        )

        self.assertEqual(200, response.status_code)
        self.assertContains(response, "Only the owner can archive")

    @mock.patch("dashboard.views.list_admin_users")
    def test_transfer_view_without_new_owner_shows_error(self, mock_list_users):
        self._login(["users:read"])
        mock_list_users.return_value = {"items": [], "total": 0}

        response = self.client.post(
            reverse("operator-users-view"),
            data={"action": "transfer_view", "viewName": "ops@example.com::BannedView", "newOwnerEmail": ""},
        )

        self.assertEqual(200, response.status_code)
        self.assertContains(response, "New owner email is required")

    @mock.patch("dashboard.views.list_admin_users")
    def test_transfer_view_non_owned_shared_shows_error(self, mock_list_users):
        self._login(["users:read"], email="ops@example.com")
        mock_list_users.return_value = {"items": [], "total": 0}

        response = self.client.post(
            reverse("operator-users-view"),
            data={
                "action": "transfer_view",
                "viewName": "other@example.com::BannedView",
                "newOwnerEmail": "third@example.com",
            },
        )

        self.assertEqual(200, response.status_code)
        self.assertContains(response, "Only the owner can transfer")

    @mock.patch("dashboard.views._transfer_named_view")
    @mock.patch("dashboard.views.list_admin_users")
    def test_transfer_view_not_found_shows_error(self, mock_list_users, mock_transfer):
        self._login(["users:read"])
        mock_list_users.return_value = {"items": [], "total": 0}
        mock_transfer.return_value = False

        response = self.client.post(
            reverse("operator-users-view"),
            data={
                "action": "transfer_view",
                "viewName": "ops@example.com::BannedView",
                "newOwnerEmail": "next@example.com",
            },
        )

        self.assertEqual(200, response.status_code)
        self.assertContains(response, "Saved view not found")

    @mock.patch("dashboard.views._archive_named_view")
    @mock.patch("dashboard.views.list_admin_users")
    def test_archive_view_not_found_shows_error(self, mock_list_users, mock_archive):
        self._login(["users:read"])
        mock_list_users.return_value = {"items": [], "total": 0}
        mock_archive.return_value = False

        response = self.client.post(
            reverse("operator-users-view"),
            data={"action": "archive_view", "viewName": "ops@example.com::BannedView"},
        )

        self.assertEqual(200, response.status_code)
        self.assertContains(response, "Saved view not found")

    # ── Backend error handling ─────────────────────────────────────────────

    @mock.patch("dashboard.views.list_admin_users")
    def test_users_view_shows_error_on_http_backend_failure(self, mock_list_users):
        self._login(["users:read"])
        req = httpx.Request("GET", "http://backend/users")
        resp = httpx.Response(status_code=502, request=req)
        mock_list_users.side_effect = httpx.HTTPStatusError("502", request=req, response=resp)

        response = self.client.get(reverse("operator-users-view"))

        self.assertEqual(200, response.status_code)
        self.assertContains(response, "Users lookup failed")

    @mock.patch("dashboard.views.list_admin_users")
    def test_users_view_shows_error_on_request_error(self, mock_list_users):
        self._login(["users:read"])
        req = httpx.Request("GET", "http://backend/users")
        mock_list_users.side_effect = httpx.ConnectError("refused", request=req)

        response = self.client.get(reverse("operator-users-view"))

        self.assertEqual(200, response.status_code)
        self.assertContains(response, "Unable to reach backend admin users endpoint")


# ---------------------------------------------------------------------------
# 3. User detail page flow
# ---------------------------------------------------------------------------

class UserDetailPageFlowTests(_BaseUserFlowTests):
    """Tests for the user detail page (operator_user_detail_view)."""

    @mock.patch("dashboard.views.get_admin_user_activity")
    @mock.patch("dashboard.views.get_admin_user")
    def _render_detail(self, user_id, mock_get_user, mock_activity, user=None, activity=None):
        self._login(["users:read", "users:write"])
        mock_get_user.return_value = user or {"id": user_id, "email": "u@example.com", "isBanned": False}
        mock_activity.return_value = activity or {"items": []}
        return self.client.get(reverse("operator-user-detail-view", kwargs={"user_id": user_id}))

    # ── GET ───────────────────────────────────────────────────────────────

    @mock.patch("dashboard.views.get_admin_user_activity")
    @mock.patch("dashboard.views.get_admin_user")
    def test_user_detail_exposes_can_write_users_true(self, mock_user, mock_activity):
        self._login(["users:read", "users:write"])
        mock_user.return_value = {"id": "u1", "email": "u@ex.com", "isBanned": False}
        mock_activity.return_value = {"items": []}
        response = self.client.get(reverse("operator-user-detail-view", kwargs={"user_id": "u1"}))
        # The template renders an enabled ban form only when can_write_users is True.
        self.assertEqual(200, response.status_code)
        self.assertNotContains(response, "read-only-badge")  # no read-only badge for write users

    @mock.patch("dashboard.views.get_admin_user_activity")
    @mock.patch("dashboard.views.get_admin_user")
    def test_user_detail_shows_error_message_on_user_http_error(self, mock_user, mock_activity):
        self._login(["users:read"])
        req = httpx.Request("GET", "http://backend/users/u1")
        resp = httpx.Response(status_code=404, request=req)
        mock_user.side_effect = httpx.HTTPStatusError("404", request=req, response=resp)
        mock_activity.return_value = {"items": []}

        response = self.client.get(reverse("operator-user-detail-view", kwargs={"user_id": "u1"}))

        self.assertEqual(200, response.status_code)
        self.assertContains(response, "User lookup failed")

    @mock.patch("dashboard.views.get_admin_user_activity")
    @mock.patch("dashboard.views.get_admin_user")
    def test_user_detail_shows_error_message_on_user_request_error(self, mock_user, mock_activity):
        self._login(["users:read"])
        req = httpx.Request("GET", "http://backend/users/u1")
        mock_user.side_effect = httpx.ConnectError("refused", request=req)
        mock_activity.return_value = {"items": []}

        response = self.client.get(reverse("operator-user-detail-view", kwargs={"user_id": "u1"}))

        self.assertEqual(200, response.status_code)
        self.assertContains(response, "Unable to reach backend user detail endpoint")

    @mock.patch("dashboard.views.get_admin_user_activity")
    @mock.patch("dashboard.views.get_admin_user")
    def test_user_detail_shows_error_message_on_activity_http_error(self, mock_user, mock_activity):
        self._login(["users:read"])
        mock_user.return_value = {"id": "u1", "email": "u@ex.com"}
        req = httpx.Request("GET", "http://backend/users/u1/activity")
        resp = httpx.Response(status_code=500, request=req)
        mock_activity.side_effect = httpx.HTTPStatusError("500", request=req, response=resp)

        response = self.client.get(reverse("operator-user-detail-view", kwargs={"user_id": "u1"}))

        self.assertEqual(200, response.status_code)
        self.assertContains(response, "User activity lookup failed")

    @mock.patch("dashboard.views.get_admin_user_activity")
    @mock.patch("dashboard.views.get_admin_user")
    def test_user_detail_shows_error_message_on_activity_request_error(self, mock_user, mock_activity):
        self._login(["users:read"])
        mock_user.return_value = {"id": "u1", "email": "u@ex.com"}
        req = httpx.Request("GET", "http://backend/users/u1/activity")
        mock_activity.side_effect = httpx.ConnectError("refused", request=req)

        response = self.client.get(reverse("operator-user-detail-view", kwargs={"user_id": "u1"}))

        self.assertEqual(200, response.status_code)
        self.assertContains(response, "Unable to reach backend user activity endpoint")

    @mock.patch("dashboard.views.get_admin_user_activity")
    @mock.patch("dashboard.views.get_admin_user")
    def test_user_detail_passes_activity_pagination_params(self, mock_user, mock_activity):
        self._login(["users:read"])
        mock_user.return_value = {"id": "u1", "email": "u@ex.com"}
        mock_activity.return_value = {"items": []}

        self.client.get(
            reverse("operator-user-detail-view", kwargs={"user_id": "u1"}),
            {"activityPage": "3", "activityPageSize": "50", "activityType": "LOGIN"},
        )

        _, call_user_id, call_query = mock_activity.call_args[0]
        self.assertEqual("u1", call_user_id)
        self.assertEqual("3", call_query["page"])
        self.assertEqual("50", call_query["pageSize"])
        self.assertEqual("LOGIN", call_query["type"])

    # ── POST: ban ────────────────────────────────────────────────────────

    @mock.patch("dashboard.views.ban_admin_user")
    def test_user_detail_post_ban_action_redirects_back(self, mock_ban):
        self._login(["users:read", "users:write"])
        response = self.client.post(
            reverse("operator-user-detail-view", kwargs={"user_id": "u1"}),
            data={"action": "ban", "reason": "policy violation"},
        )

        self.assertEqual(302, response.status_code)
        self.assertIn("/users/u1", response.url)
        mock_ban.assert_called_once_with("tok", "u1", "policy violation", None)

    @mock.patch("dashboard.views.ban_admin_user")
    def test_user_detail_post_ban_with_until_passes_expiry(self, mock_ban):
        self._login(["users:read", "users:write"])
        self.client.post(
            reverse("operator-user-detail-view", kwargs={"user_id": "u1"}),
            data={"action": "ban", "reason": "policy", "until": "2026-12-31T00:00"},
        )

        mock_ban.assert_called_once_with("tok", "u1", "policy", "2026-12-31T00:00")

    # ── POST: unban ───────────────────────────────────────────────────────

    @mock.patch("dashboard.views.unban_admin_user")
    def test_user_detail_post_unban_action_redirects_back(self, mock_unban):
        self._login(["users:read", "users:write"])
        response = self.client.post(
            reverse("operator-user-detail-view", kwargs={"user_id": "u1"}),
            data={"action": "unban"},
        )

        self.assertEqual(302, response.status_code)
        self.assertIn("/users/u1", response.url)
        mock_unban.assert_called_once_with("tok", "u1")

    # ── POST: update – missing username ───────────────────────────────────

    @mock.patch("dashboard.views.update_admin_user")
    def test_user_detail_post_update_with_empty_username_does_not_call_backend(self, mock_update):
        self._login(["users:read", "users:write"])
        response = self.client.post(
            reverse("operator-user-detail-view", kwargs={"user_id": "u1"}),
            data={"action": "update", "username": ""},
        )

        self.assertEqual(302, response.status_code)
        mock_update.assert_not_called()

    # ── POST: unknown action ──────────────────────────────────────────────

    @mock.patch("dashboard.views.update_admin_user")
    @mock.patch("dashboard.views.ban_admin_user")
    @mock.patch("dashboard.views.unban_admin_user")
    def test_user_detail_post_unknown_action_redirects_without_calling_backend(
        self, mock_unban, mock_ban, mock_update
    ):
        self._login(["users:read", "users:write"])
        response = self.client.post(
            reverse("operator-user-detail-view", kwargs={"user_id": "u1"}),
            data={"action": "delete"},
        )

        self.assertEqual(302, response.status_code)
        mock_ban.assert_not_called()
        mock_unban.assert_not_called()
        mock_update.assert_not_called()

    # ── POST: backend HTTP error ──────────────────────────────────────────

    @mock.patch("dashboard.views.ban_admin_user")
    def test_user_detail_post_ban_http_error_redirects_with_error(self, mock_ban):
        self._login(["users:read", "users:write"])
        req = httpx.Request("POST", "http://backend/users/u1/ban")
        resp = httpx.Response(status_code=409, request=req)
        mock_ban.side_effect = httpx.HTTPStatusError("409", request=req, response=resp)

        response = self.client.post(
            reverse("operator-user-detail-view", kwargs={"user_id": "u1"}),
            data={"action": "ban", "reason": "policy"},
        )

        self.assertEqual(302, response.status_code)

    # ── POST: backend request error ───────────────────────────────────────

    @mock.patch("dashboard.views.ban_admin_user")
    def test_user_detail_post_ban_request_error_redirects(self, mock_ban):
        self._login(["users:read", "users:write"])
        req = httpx.Request("POST", "http://backend/users/u1/ban")
        mock_ban.side_effect = httpx.ConnectError("refused", request=req)

        response = self.client.post(
            reverse("operator-user-detail-view", kwargs={"user_id": "u1"}),
            data={"action": "ban", "reason": "policy"},
        )

        self.assertEqual(302, response.status_code)


# ---------------------------------------------------------------------------
# 4. User investigation workbench flow
# ---------------------------------------------------------------------------

class UserInvestigationFlowTests(_BaseUserFlowTests):
    """Tests for the investigation workbench (operator_user_investigation_view)."""

    _PLAYER_UUID = "00000000-0000-0000-0000-000000000002"

    def _setup_mocks(self, user=None, activity=None, moderation=None, economy=None, profile=None, debug=None, stock=None):
        if user is not None:
            user.return_value = {"id": self._PLAYER_UUID, "email": "player@ex.com", "isBanned": False}
        if activity is not None:
            activity.return_value = {"items": []}
        if moderation is not None:
            moderation.return_value = {"status": "clear"}
        if economy is not None:
            economy.return_value = {"items": []}
        if profile is not None:
            profile.return_value = {"archetype": "Explorer"}
        if debug is not None:
            debug.return_value = {"recentEvents": [], "recentAudit": []}
        if stock is not None:
            stock.return_value = {"items": []}

    # ── Non-UUID player_id ────────────────────────────────────────────────

    @mock.patch("dashboard.views.get_admin_user_activity")
    @mock.patch("dashboard.views.get_admin_user")
    def test_investigation_non_uuid_player_id_shows_stock_skip_message(
        self, mock_user, mock_activity
    ):
        """When the user_id is not a UUID, the stock section should be skipped."""
        self._login(["users:read", "store:read"])
        mock_user.return_value = {"id": "usr_abc123", "email": "p@ex.com"}
        mock_activity.return_value = {"items": []}

        response = self.client.get(
            reverse("operator-user-investigation-view", kwargs={"user_id": "usr_abc123"})
        )

        self.assertEqual(200, response.status_code)
        self.assertContains(response, "requires a player UUID")

    # ── UUID player_id with valid stock lookup ────────────────────────────

    @mock.patch("dashboard.views.get_player_stock")
    @mock.patch("dashboard.views.get_player_debug")
    @mock.patch("dashboard.views.get_player_profile")
    @mock.patch("dashboard.views.get_economy_history")
    @mock.patch("dashboard.views.get_moderation_profile")
    @mock.patch("dashboard.views.get_admin_user_activity")
    @mock.patch("dashboard.views.get_admin_user")
    def test_investigation_user_contract_id_normalizes_player_scoped_calls(
        self,
        mock_user,
        mock_activity,
        mock_moderation,
        mock_economy,
        mock_profile,
        mock_debug,
        mock_stock,
    ):
        self._login(["users:read", "store:read", "moderation:read", "economy:read", "personalization:read"])
        self._setup_mocks(
            user=mock_user,
            activity=mock_activity,
            moderation=mock_moderation,
            economy=mock_economy,
            profile=mock_profile,
            debug=mock_debug,
            stock=mock_stock,
        )
        user_id = f"usr_{self._PLAYER_UUID.replace('-', '')}"

        response = self.client.get(
            reverse("operator-user-investigation-view", kwargs={"user_id": user_id})
        )

        self.assertEqual(200, response.status_code)
        mock_moderation.assert_called_once_with("tok", self._PLAYER_UUID)
        mock_economy.assert_called_once_with("tok", self._PLAYER_UUID, {"page": 1, "pageSize": 10})
        mock_profile.assert_called_once_with("tok", self._PLAYER_UUID)
        mock_debug.assert_called_once_with("tok", self._PLAYER_UUID)
        mock_stock.assert_called_once_with("tok", self._PLAYER_UUID)

    @mock.patch("dashboard.views.get_player_stock")
    @mock.patch("dashboard.views.get_player_debug")
    @mock.patch("dashboard.views.get_player_profile")
    @mock.patch("dashboard.views.get_economy_history")
    @mock.patch("dashboard.views.get_moderation_profile")
    @mock.patch("dashboard.views.get_admin_user_activity")
    @mock.patch("dashboard.views.get_admin_user")
    def test_investigation_uuid_player_id_calls_stock_endpoint(
        self,
        mock_user,
        mock_activity,
        mock_moderation,
        mock_economy,
        mock_profile,
        mock_debug,
        mock_stock,
    ):
        self._login(["users:read", "store:read", "moderation:read", "economy:read", "personalization:read"])
        self._setup_mocks(
            user=mock_user, activity=mock_activity, moderation=mock_moderation,
            economy=mock_economy, profile=mock_profile, debug=mock_debug, stock=mock_stock,
        )

        response = self.client.get(
            reverse("operator-user-investigation-view", kwargs={"user_id": self._PLAYER_UUID}),
            {"playerId": self._PLAYER_UUID},
        )

        self.assertEqual(200, response.status_code)
        mock_stock.assert_called_once_with("tok", self._PLAYER_UUID)

    # ── Optional sections: HTTP errors ───────────────────────────────────

    @mock.patch("dashboard.views.get_player_stock")
    @mock.patch("dashboard.views.get_player_debug")
    @mock.patch("dashboard.views.get_player_profile")
    @mock.patch("dashboard.views.get_economy_history")
    @mock.patch("dashboard.views.get_moderation_profile")
    @mock.patch("dashboard.views.get_admin_user_activity")
    @mock.patch("dashboard.views.get_admin_user")
    def test_investigation_moderation_http_error_shows_section_error(
        self,
        mock_user,
        mock_activity,
        mock_moderation,
        mock_economy,
        mock_profile,
        mock_debug,
        mock_stock,
    ):
        self._login(["users:read", "moderation:read", "economy:read", "personalization:read", "store:read"])
        self._setup_mocks(mock_user, mock_activity, economy=mock_economy, profile=mock_profile, debug=mock_debug, stock=mock_stock)
        req = httpx.Request("GET", "http://backend/moderation")
        resp = httpx.Response(status_code=503, request=req)
        mock_moderation.side_effect = httpx.HTTPStatusError("503", request=req, response=resp)

        response = self.client.get(
            reverse("operator-user-investigation-view", kwargs={"user_id": self._PLAYER_UUID}),
            {"playerId": self._PLAYER_UUID},
        )

        self.assertEqual(200, response.status_code)
        self.assertContains(response, "Moderation profile lookup failed")

    @mock.patch("dashboard.views.get_player_stock")
    @mock.patch("dashboard.views.get_player_debug")
    @mock.patch("dashboard.views.get_player_profile")
    @mock.patch("dashboard.views.get_economy_history")
    @mock.patch("dashboard.views.get_moderation_profile")
    @mock.patch("dashboard.views.get_admin_user_activity")
    @mock.patch("dashboard.views.get_admin_user")
    def test_investigation_economy_http_error_shows_section_error(
        self,
        mock_user,
        mock_activity,
        mock_moderation,
        mock_economy,
        mock_profile,
        mock_debug,
        mock_stock,
    ):
        self._login(["users:read", "moderation:read", "economy:read", "personalization:read", "store:read"])
        self._setup_mocks(mock_user, mock_activity, moderation=mock_moderation, profile=mock_profile, debug=mock_debug, stock=mock_stock)
        req = httpx.Request("GET", "http://backend/economy")
        resp = httpx.Response(status_code=503, request=req)
        mock_economy.side_effect = httpx.HTTPStatusError("503", request=req, response=resp)

        response = self.client.get(
            reverse("operator-user-investigation-view", kwargs={"user_id": self._PLAYER_UUID}),
            {"playerId": self._PLAYER_UUID},
        )

        self.assertEqual(200, response.status_code)
        self.assertContains(response, "Economy history lookup failed")

    @mock.patch("dashboard.views.get_player_stock")
    @mock.patch("dashboard.views.get_player_debug")
    @mock.patch("dashboard.views.get_player_profile")
    @mock.patch("dashboard.views.get_economy_history")
    @mock.patch("dashboard.views.get_moderation_profile")
    @mock.patch("dashboard.views.get_admin_user_activity")
    @mock.patch("dashboard.views.get_admin_user")
    def test_investigation_personalization_http_error_shows_section_error(
        self,
        mock_user,
        mock_activity,
        mock_moderation,
        mock_economy,
        mock_profile,
        mock_debug,
        mock_stock,
    ):
        self._login(["users:read", "moderation:read", "economy:read", "personalization:read", "store:read"])
        self._setup_mocks(mock_user, mock_activity, moderation=mock_moderation, economy=mock_economy, debug=mock_debug, stock=mock_stock)
        req = httpx.Request("GET", "http://backend/personalization")
        resp = httpx.Response(status_code=502, request=req)
        mock_profile.side_effect = httpx.HTTPStatusError("502", request=req, response=resp)

        response = self.client.get(
            reverse("operator-user-investigation-view", kwargs={"user_id": self._PLAYER_UUID}),
            {"playerId": self._PLAYER_UUID},
        )

        self.assertEqual(200, response.status_code)
        self.assertContains(response, "Personalization profile lookup failed")

    # ── Optional sections: request errors ────────────────────────────────

    @mock.patch("dashboard.views.get_player_stock")
    @mock.patch("dashboard.views.get_player_debug")
    @mock.patch("dashboard.views.get_player_profile")
    @mock.patch("dashboard.views.get_economy_history")
    @mock.patch("dashboard.views.get_moderation_profile")
    @mock.patch("dashboard.views.get_admin_user_activity")
    @mock.patch("dashboard.views.get_admin_user")
    def test_investigation_moderation_request_error_shows_section_error(
        self,
        mock_user,
        mock_activity,
        mock_moderation,
        mock_economy,
        mock_profile,
        mock_debug,
        mock_stock,
    ):
        self._login(["users:read", "moderation:read", "economy:read", "personalization:read", "store:read"])
        self._setup_mocks(mock_user, mock_activity, economy=mock_economy, profile=mock_profile, debug=mock_debug, stock=mock_stock)
        req = httpx.Request("GET", "http://backend/moderation")
        mock_moderation.side_effect = httpx.ConnectError("refused", request=req)

        response = self.client.get(
            reverse("operator-user-investigation-view", kwargs={"user_id": self._PLAYER_UUID}),
            {"playerId": self._PLAYER_UUID},
        )

        self.assertEqual(200, response.status_code)
        self.assertContains(response, "Unable to reach backend")

    # ── User profile errors in investigation ─────────────────────────────

    @mock.patch("dashboard.views.get_player_stock")
    @mock.patch("dashboard.views.get_player_debug")
    @mock.patch("dashboard.views.get_player_profile")
    @mock.patch("dashboard.views.get_economy_history")
    @mock.patch("dashboard.views.get_moderation_profile")
    @mock.patch("dashboard.views.get_admin_user_activity")
    @mock.patch("dashboard.views.get_admin_user")
    def test_investigation_user_http_error_shows_error_in_sections(
        self,
        mock_user,
        mock_activity,
        mock_moderation,
        mock_economy,
        mock_profile,
        mock_debug,
        mock_stock,
    ):
        self._login(["users:read"])
        req = httpx.Request("GET", "http://backend/users/u1")
        resp = httpx.Response(status_code=404, request=req)
        mock_user.side_effect = httpx.HTTPStatusError("404", request=req, response=resp)
        mock_activity.return_value = {"items": []}

        response = self.client.get(
            reverse("operator-user-investigation-view", kwargs={"user_id": "usr_missing"})
        )

        self.assertEqual(200, response.status_code)
        self.assertContains(response, "User profile lookup failed")

    @mock.patch("dashboard.views.get_player_stock")
    @mock.patch("dashboard.views.get_player_debug")
    @mock.patch("dashboard.views.get_player_profile")
    @mock.patch("dashboard.views.get_economy_history")
    @mock.patch("dashboard.views.get_moderation_profile")
    @mock.patch("dashboard.views.get_admin_user_activity")
    @mock.patch("dashboard.views.get_admin_user")
    def test_investigation_user_request_error_shows_error_in_sections(
        self,
        mock_user,
        mock_activity,
        mock_moderation,
        mock_economy,
        mock_profile,
        mock_debug,
        mock_stock,
    ):
        self._login(["users:read"])
        req = httpx.Request("GET", "http://backend/users/u1")
        mock_user.side_effect = httpx.ConnectError("refused", request=req)
        mock_activity.return_value = {"items": []}

        response = self.client.get(
            reverse("operator-user-investigation-view", kwargs={"user_id": "usr_missing"})
        )

        self.assertEqual(200, response.status_code)
        self.assertContains(response, "Unable to reach backend user profile endpoint")

    # ── Write permission context ───────────────────────────────────────────

    @mock.patch("dashboard.views.get_player_stock")
    @mock.patch("dashboard.views.get_player_debug")
    @mock.patch("dashboard.views.get_player_profile")
    @mock.patch("dashboard.views.get_economy_history")
    @mock.patch("dashboard.views.get_moderation_profile")
    @mock.patch("dashboard.views.get_admin_user_activity")
    @mock.patch("dashboard.views.get_admin_user")
    def test_investigation_with_write_permission_shows_editable_actions(
        self,
        mock_user,
        mock_activity,
        mock_moderation,
        mock_economy,
        mock_profile,
        mock_debug,
        mock_stock,
    ):
        self._login(["users:read", "users:write", "moderation:read", "economy:read", "personalization:read", "store:read"])
        self._setup_mocks(
            user=mock_user, activity=mock_activity, moderation=mock_moderation,
            economy=mock_economy, profile=mock_profile, debug=mock_debug, stock=mock_stock,
        )

        response = self.client.get(
            reverse("operator-user-investigation-view", kwargs={"user_id": self._PLAYER_UUID}),
            {"playerId": self._PLAYER_UUID},
        )

        self.assertEqual(200, response.status_code)
        self.assertContains(response, "Editable actions")
        # The workbench is intentionally read-only by design; the "Editable actions"
        # section links to authoritative pages for operator mutations.
        self.assertContains(response, "This workbench is read-only")


# ---------------------------------------------------------------------------
# 5. User JSON API endpoints
# ---------------------------------------------------------------------------

class UserJsonApiFlowTests(_BaseUserFlowTests):
    """Tests for the thin JSON API endpoints that power AJAX updates."""

    # ── operator_user_update: isVerified normalisation ────────────────────

    @mock.patch("dashboard.views.update_admin_user")
    def test_user_update_is_verified_true_string_normalised_to_bool(self, mock_update):
        self._login(["users:write"])
        mock_update.return_value = {"id": "u1"}

        self.client.post(
            reverse("operator-user-update", kwargs={"user_id": "u1"}),
            data={"isVerified": "true"},
        )

        _, _, call_payload = mock_update.call_args[0]
        self.assertIs(True, call_payload["isVerified"])

    @mock.patch("dashboard.views.update_admin_user")
    def test_user_update_is_verified_false_string_normalised_to_bool(self, mock_update):
        self._login(["users:write"])
        mock_update.return_value = {"id": "u1"}

        self.client.post(
            reverse("operator-user-update", kwargs={"user_id": "u1"}),
            data={"isVerified": "false"},
        )

        _, _, call_payload = mock_update.call_args[0]
        self.assertIs(False, call_payload["isVerified"])

    @mock.patch("dashboard.views.update_admin_user")
    def test_user_update_role_field_passed_to_backend(self, mock_update):
        self._login(["users:write"])
        mock_update.return_value = {"id": "u1"}

        response = self.client.post(
            reverse("operator-user-update", kwargs={"user_id": "u1"}),
            data={"role": "moderator"},
        )

        self.assertEqual(200, response.status_code)
        _, _, call_payload = mock_update.call_args[0]
        self.assertEqual("moderator", call_payload["role"])

    # ── operator_user_activity: time filters ─────────────────────────────

    @mock.patch("dashboard.views.get_admin_user_activity")
    def test_user_activity_passes_from_and_to_filters(self, mock_activity):
        self._login(["users:read"])
        mock_activity.return_value = {"items": [], "page": 1}

        self.client.get(
            reverse("operator-user-activity", kwargs={"user_id": "u1"}),
            {"from": "2026-05-01", "to": "2026-05-20", "type": "BAN"},
        )

        _, _, call_query = mock_activity.call_args[0]
        self.assertEqual("2026-05-01", call_query["from"])
        self.assertEqual("2026-05-20", call_query["to"])
        self.assertEqual("BAN", call_query["type"])

    # ── operator_user_ban: JSON API error forwarding ─────────────────────

    @mock.patch("dashboard.views.ban_admin_user")
    def test_user_ban_api_forwards_http_error_from_backend(self, mock_ban):
        self._login(["users:write"])
        req = httpx.Request("POST", "http://backend/users/u1/ban")
        resp = httpx.Response(status_code=409, request=req)
        mock_ban.side_effect = httpx.HTTPStatusError("409", request=req, response=resp)

        response = self.client.post(
            reverse("operator-user-ban", kwargs={"user_id": "u1"}),
            data={"reason": "policy"},
        )

        self.assertEqual(409, response.status_code)

    @mock.patch("dashboard.views.unban_admin_user")
    def test_user_unban_api_forwards_http_error_from_backend(self, mock_unban):
        self._login(["users:write"])
        req = httpx.Request("POST", "http://backend/users/u1/unban")
        resp = httpx.Response(status_code=404, request=req)
        mock_unban.side_effect = httpx.HTTPStatusError("404", request=req, response=resp)

        response = self.client.post(reverse("operator-user-unban", kwargs={"user_id": "u1"}))

        self.assertEqual(404, response.status_code)

    @mock.patch("dashboard.views.ban_admin_user")
    def test_user_ban_api_returns_503_on_request_error(self, mock_ban):
        self._login(["users:write"])
        req = httpx.Request("POST", "http://backend/users/u1/ban")
        mock_ban.side_effect = httpx.ConnectError("refused", request=req)

        response = self.client.post(
            reverse("operator-user-ban", kwargs={"user_id": "u1"}),
            data={"reason": "policy"},
        )

        self.assertEqual(503, response.status_code)

    # ── operator_user_detail JSON API ─────────────────────────────────────

    @mock.patch("dashboard.views.get_admin_user")
    def test_user_detail_json_api_forwards_404_from_backend(self, mock_user):
        self._login(["users:read"])
        req = httpx.Request("GET", "http://backend/users/missing")
        resp = httpx.Response(status_code=404, request=req)
        mock_user.side_effect = httpx.HTTPStatusError("404", request=req, response=resp)

        response = self.client.get(reverse("operator-user-detail", kwargs={"user_id": "missing"}))

        self.assertEqual(404, response.status_code)

    @mock.patch("dashboard.views.get_admin_user")
    def test_user_detail_json_api_returns_503_on_request_error(self, mock_user):
        self._login(["users:read"])
        req = httpx.Request("GET", "http://backend/users/u1")
        mock_user.side_effect = httpx.ConnectError("refused", request=req)

        response = self.client.get(reverse("operator-user-detail", kwargs={"user_id": "u1"}))

        self.assertEqual(503, response.status_code)

    # ── operator_users JSON API ────────────────────────────────────────────

    @mock.patch("dashboard.views.list_admin_users")
    def test_users_json_api_forwards_http_error_from_backend(self, mock_list):
        self._login(["users:read"])
        req = httpx.Request("GET", "http://backend/users")
        resp = httpx.Response(status_code=502, request=req)
        mock_list.side_effect = httpx.HTTPStatusError("502", request=req, response=resp)

        response = self.client.get(reverse("operator-users"))

        self.assertEqual(502, response.status_code)

    @mock.patch("dashboard.views.list_admin_users")
    def test_users_json_api_returns_503_on_request_error(self, mock_list):
        self._login(["users:read"])
        req = httpx.Request("GET", "http://backend/users")
        mock_list.side_effect = httpx.ConnectError("refused", request=req)

        response = self.client.get(reverse("operator-users"))

        self.assertEqual(503, response.status_code)
