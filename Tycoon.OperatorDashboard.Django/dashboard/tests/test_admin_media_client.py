from unittest import mock

from django.test import SimpleTestCase, override_settings

from dashboard.services.admin_media_client import create_upload_intent


class AdminMediaClientTests(SimpleTestCase):
    @override_settings(DOTNET_API_BASE_URL="http://backend-api:5000", ADMIN_OPS_KEY="abc123")
    @mock.patch("dashboard.services.admin_media_client.httpx.post")
    def test_create_upload_intent(self, mock_post):
        response = mock.Mock()
        response.raise_for_status.return_value = None
        response.json.return_value = {
            "assetKey": "media/key",
            "uploadUrl": "http://minio/upload",
            "expiresAtUtc": "2026-04-05T00:00:00Z",
        }
        mock_post.return_value = response

        payload = create_upload_intent("access-token", "avatar.png", "image/png", 1234)

        self.assertEqual("media/key", payload["assetKey"])
