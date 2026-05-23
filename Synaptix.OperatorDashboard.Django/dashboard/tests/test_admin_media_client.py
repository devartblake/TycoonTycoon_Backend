from unittest import mock

from django.core.files.uploadedfile import SimpleUploadedFile
from django.test import SimpleTestCase, override_settings

from dashboard.services.admin_media_client import create_upload_intent, get_storage_diagnostics, upload_file_to_intent


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

    @override_settings(DOTNET_API_BASE_URL="http://backend-api:5000", ADMIN_OPS_KEY="abc123")
    @mock.patch("dashboard.services.admin_media_client.httpx.get")
    def test_get_storage_diagnostics(self, mock_get):
        response = mock.Mock()
        response.raise_for_status.return_value = None
        response.json.return_value = {"overallStatus": "healthy"}
        mock_get.return_value = response

        payload = get_storage_diagnostics("access-token")

        self.assertEqual("healthy", payload["overallStatus"])
        self.assertEqual("http://backend-api:5000/admin/media/storage-diagnostics", mock_get.call_args.args[0])

    @override_settings(API_REQUEST_TIMEOUT_SECONDS=3)
    @mock.patch("dashboard.services.admin_media_client.httpx.put")
    def test_upload_file_to_intent_uses_absolute_presigned_put(self, mock_put):
        response = mock.Mock(status_code=200, is_success=True)
        response.raise_for_status.return_value = None
        response.json.side_effect = ValueError()
        mock_put.return_value = response
        uploaded = SimpleUploadedFile("avatar.png", b"\x89PNG", content_type="image/png")

        result = upload_file_to_intent("access-token", "http://minio/upload", uploaded, "image/png")

        self.assertTrue(result["ok"])
        self.assertEqual(200, result["statusCode"])
        self.assertEqual("http://minio/upload", mock_put.call_args.args[0])
        self.assertEqual("image/png", mock_put.call_args.kwargs["headers"]["Content-Type"])

    @override_settings(DOTNET_API_BASE_URL="http://backend-api:5000", ADMIN_OPS_KEY="abc123", API_REQUEST_TIMEOUT_SECONDS=3)
    @mock.patch("dashboard.services.admin_media_client.httpx.post")
    def test_upload_file_to_intent_uses_relative_backend_fallback(self, mock_post):
        response = mock.Mock(status_code=200, is_success=True)
        response.raise_for_status.return_value = None
        response.json.return_value = {"assetKey": "uploads/images/k.png"}
        mock_post.return_value = response
        uploaded = SimpleUploadedFile("avatar.png", b"\x89PNG", content_type="image/png")

        result = upload_file_to_intent("access-token", "/admin/media/upload/uploads%2Fk.png", uploaded, "image/png")

        self.assertTrue(result["ok"])
        self.assertEqual("http://backend-api:5000/admin/media/upload/uploads%2Fk.png", mock_post.call_args.args[0])
        self.assertIn("file", mock_post.call_args.kwargs["files"])
