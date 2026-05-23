from unittest.mock import MagicMock, patch

import httpx
from django.test import TestCase, override_settings

from dashboard.services.admin_notifications_client import (
    cancel_scheduled,
    create_template,
    delete_template,
    list_scheduled,
    list_templates,
    schedule_notification,
    send_notification,
    update_template,
    upsert_channel,
)

DOTNET_BASE = "http://backend-api:5000"
SETTINGS = {
    "DOTNET_API_BASE_URL": DOTNET_BASE,
    "ADMIN_OPS_KEY": "test-ops-key",
    "ADMIN_OPS_HEADER": "X-Admin-Ops-Key",
    "API_REQUEST_TIMEOUT_SECONDS": 5,
}
TOKEN = "test-access-token"


def _mock_response(status_code: int, body: dict | list | None = None):
    response = MagicMock(spec=httpx.Response)
    response.status_code = status_code
    response.json.return_value = body if body is not None else {}
    if status_code >= 400:
        response.raise_for_status.side_effect = httpx.HTTPStatusError(
            "error", request=MagicMock(), response=response
        )
    else:
        response.raise_for_status.return_value = None
    return response


@override_settings(**SETTINGS)
class AdminNotificationsClientTests(TestCase):
    @patch("dashboard.services.admin_notifications_client.httpx.post")
    def test_send_notification_posts_full_payload(self, mock_post):
        payload = {
            "channelKey": "admin_basic",
            "title": "Hello",
            "body": "Body",
            "audience": {"type": "all"},
            "payload": {"deepLink": "/home"},
        }
        mock_post.return_value = _mock_response(202, {"jobId": "job_1"})

        result = send_notification(TOKEN, payload)

        call_url = mock_post.call_args[0][0]
        _, kwargs = mock_post.call_args
        assert call_url == f"{DOTNET_BASE}/admin/notifications/send"
        assert kwargs["json"] == payload
        assert kwargs["headers"]["Authorization"] == f"Bearer {TOKEN}"
        assert kwargs["headers"]["X-Admin-Ops-Key"] == "test-ops-key"
        assert result["jobId"] == "job_1"

    @patch("dashboard.services.admin_notifications_client.httpx.put")
    def test_upsert_channel_puts_key_and_body(self, mock_put):
        payload = {"name": "Security", "description": "Alerts", "importance": "high", "enabled": True}
        mock_put.return_value = _mock_response(200, {"key": "admin_security"})

        upsert_channel(TOKEN, "admin_security", payload)

        call_url = mock_put.call_args[0][0]
        _, kwargs = mock_put.call_args
        assert call_url == f"{DOTNET_BASE}/admin/notifications/channels/admin_security"
        assert kwargs["json"] == payload

    @patch("dashboard.services.admin_notifications_client.httpx.post")
    def test_schedule_notification_posts_payload(self, mock_post):
        payload = {
            "channelKey": "admin_basic",
            "title": "Later",
            "body": "Body",
            "scheduledAt": "2026-05-13T12:00:00Z",
            "audience": {"type": "all"},
            "repeat": None,
        }
        mock_post.return_value = _mock_response(201, {"scheduleId": "sch_1"})

        result = schedule_notification(TOKEN, payload)

        call_url = mock_post.call_args[0][0]
        _, kwargs = mock_post.call_args
        assert call_url == f"{DOTNET_BASE}/admin/notifications/schedule"
        assert kwargs["json"] == payload
        assert result["scheduleId"] == "sch_1"

    @patch("dashboard.services.admin_notifications_client.httpx.get")
    def test_list_scheduled_passes_params(self, mock_get):
        mock_get.return_value = _mock_response(200, {"items": []})

        list_scheduled(TOKEN, {"page": 2, "pageSize": 50, "empty": ""})

        call_url = mock_get.call_args[0][0]
        _, kwargs = mock_get.call_args
        assert call_url == f"{DOTNET_BASE}/admin/notifications/scheduled"
        assert kwargs["params"] == {"page": 2, "pageSize": 50}

    @patch("dashboard.services.admin_notifications_client.httpx.delete")
    def test_cancel_scheduled_deletes_schedule(self, mock_delete):
        mock_delete.return_value = _mock_response(204)

        cancel_scheduled(TOKEN, "sch_1")

        assert mock_delete.call_args[0][0] == f"{DOTNET_BASE}/admin/notifications/scheduled/sch_1"

    @patch("dashboard.services.admin_notifications_client.httpx.get")
    def test_list_templates_returns_templates(self, mock_get):
        mock_get.return_value = _mock_response(200, [{"templateId": "tpl_1"}])

        result = list_templates(TOKEN)

        assert mock_get.call_args[0][0] == f"{DOTNET_BASE}/admin/notifications/templates"
        assert result[0]["templateId"] == "tpl_1"

    @patch("dashboard.services.admin_notifications_client.httpx.post")
    def test_create_template_posts_body(self, mock_post):
        payload = {"name": "Winback", "title": "Hi", "body": "Body", "channelKey": "admin_basic", "variables": []}
        mock_post.return_value = _mock_response(201, {"templateId": "tpl_1"})

        create_template(TOKEN, payload)

        assert mock_post.call_args[0][0] == f"{DOTNET_BASE}/admin/notifications/templates"
        assert mock_post.call_args.kwargs["json"] == payload

    @patch("dashboard.services.admin_notifications_client.httpx.patch")
    def test_update_template_patches_body(self, mock_patch):
        payload = {"name": "Winback", "title": "Hi", "body": "Body", "channelKey": "admin_basic", "variables": ["name"]}
        mock_patch.return_value = _mock_response(200, {"templateId": "tpl_1"})

        update_template(TOKEN, "tpl_1", payload)

        assert mock_patch.call_args[0][0] == f"{DOTNET_BASE}/admin/notifications/templates/tpl_1"
        assert mock_patch.call_args.kwargs["json"] == payload

    @patch("dashboard.services.admin_notifications_client.httpx.delete")
    def test_delete_template_deletes_template(self, mock_delete):
        mock_delete.return_value = _mock_response(204)

        delete_template(TOKEN, "tpl_1")

        assert mock_delete.call_args[0][0] == f"{DOTNET_BASE}/admin/notifications/templates/tpl_1"

    @patch("dashboard.services.admin_notifications_client.httpx.post")
    def test_http_error_propagates(self, mock_post):
        mock_post.return_value = _mock_response(403)
        with self.assertRaises(httpx.HTTPStatusError):
            schedule_notification(TOKEN, {})
