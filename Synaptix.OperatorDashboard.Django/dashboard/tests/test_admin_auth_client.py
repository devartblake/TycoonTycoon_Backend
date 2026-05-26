from unittest import mock

from django.test import SimpleTestCase, override_settings

from dashboard.services.admin_auth_client import admin_login, admin_me, admin_refresh


class AdminAuthClientTests(SimpleTestCase):
    @override_settings(DOTNET_API_BASE_URL="http://backend-api:5000", ADMIN_OPS_KEY="abc123", ADMIN_AUTH_TRANSPORT="plain")
    @mock.patch("dashboard.services.admin_auth_client.httpx.post")
    def test_admin_login_uses_admin_ops_header(self, mock_post):
        response = mock.Mock()
        response.raise_for_status.return_value = None
        response.json.return_value = {
            "accessToken": "at",
            "refreshToken": "rt",
            "expiresIn": 3600,
            "admin": {"email": "ops@example.com"},
        }
        mock_post.return_value = response

        result = admin_login("ops@example.com", "secret")

        self.assertEqual("at", result.access_token)
        _, kwargs = mock_post.call_args
        self.assertEqual("abc123", kwargs["headers"]["X-Admin-Ops-Key"])

    @override_settings(DOTNET_API_BASE_URL="http://backend-api:5000", ADMIN_OPS_KEY="abc123", ADMIN_AUTH_TRANSPORT="plain")
    @mock.patch("dashboard.services.admin_auth_client.httpx.post")
    def test_admin_refresh_returns_access_token(self, mock_post):
        response = mock.Mock()
        response.raise_for_status.return_value = None
        response.json.return_value = {
            "accessToken": "new-token",
            "expiresIn": 1800,
            "tokenType": "Bearer",
        }
        mock_post.return_value = response

        result = admin_refresh("refresh-token")

        self.assertEqual("new-token", result.access_token)
        self.assertEqual(1800, result.expires_in)

    @override_settings(DOTNET_API_BASE_URL="http://backend-api:5000", ADMIN_OPS_KEY="abc123")
    @mock.patch("dashboard.services.admin_auth_client.httpx.get")
    def test_admin_me_uses_bearer_token(self, mock_get):
        response = mock.Mock()
        response.raise_for_status.return_value = None
        response.json.return_value = {"email": "ops@example.com"}
        mock_get.return_value = response

        payload = admin_me("at")

        self.assertEqual("ops@example.com", payload["email"])
        _, kwargs = mock_get.call_args
        self.assertEqual("Bearer at", kwargs["headers"]["Authorization"])
        self.assertEqual("abc123", kwargs["headers"]["X-Admin-Ops-Key"])

    @override_settings(
        DOTNET_API_BASE_URL="http://backend-api:5000",
        ADMIN_OPS_HEADER="X-Custom-Ops",
        ADMIN_OPS_KEY="custom-key",
        ADMIN_AUTH_TRANSPORT="plain",
    )
    @mock.patch("dashboard.services.admin_auth_client.httpx.post")
    def test_admin_login_uses_custom_admin_ops_header_name(self, mock_post):
        response = mock.Mock()
        response.raise_for_status.return_value = None
        response.json.return_value = {
            "accessToken": "at",
            "refreshToken": "rt",
            "expiresIn": 3600,
            "admin": {"email": "ops@example.com"},
        }
        mock_post.return_value = response

        admin_login("ops@example.com", "secret")

        _, kwargs = mock_post.call_args
        self.assertEqual("custom-key", kwargs["headers"]["X-Custom-Ops"])

    @override_settings(
        DOTNET_API_BASE_URL="http://backend-api:5000",
        ADMIN_OPS_KEY="abc123",
        ADMIN_AUTH_TRANSPORT="auto",
        KMS_API_BASE_URL="",
        KMS_SERVICE_TOKEN="",
    )
    @mock.patch("dashboard.services.admin_auth_client.httpx.post")
    def test_admin_login_auto_without_kms_uses_plain_transport(self, mock_post):
        response = mock.Mock()
        response.raise_for_status.return_value = None
        response.json.return_value = {
            "accessToken": "at",
            "refreshToken": "rt",
            "expiresIn": 3600,
            "admin": {"email": "ops@example.com"},
        }
        mock_post.return_value = response

        admin_login("ops@example.com", "secret")

        self.assertEqual(1, mock_post.call_count)
        self.assertEqual("http://backend-api:5000/admin/auth/login", mock_post.call_args[0][0])

    @override_settings(
        DOTNET_API_BASE_URL="http://backend-api:5000",
        ADMIN_OPS_KEY="abc123",
        ADMIN_AUTH_TRANSPORT="secure-channel",
        KMS_API_BASE_URL="http://kms-api:5050",
        KMS_SERVICE_TOKEN="svc-token",
    )
    @mock.patch("dashboard.services.admin_auth_client.httpx.post")
    def test_admin_login_secure_channel_encrypts_and_decrypts(self, mock_post):
        start_response = mock.Mock()
        start_response.raise_for_status.return_value = None
        start_response.json.return_value = {"sessionId": "11111111-1111-1111-1111-111111111111"}

        encrypt_response = mock.Mock()
        encrypt_response.raise_for_status.return_value = None
        encrypt_response.json.return_value = {
            "ciphertext": "cipher",
            "nonce": "nonce",
            "mac": "mac",
            "contentType": "application/json",
            "encryptedAtUtc": "2026-05-12T00:00:00Z",
        }

        backend_response = mock.Mock()
        backend_response.raise_for_status.return_value = None
        backend_response.json.return_value = {
            "ciphertext": "response-cipher",
            "nonce": "response-nonce",
            "mac": "response-mac",
            "contentType": "application/json",
            "encryptedAtUtc": "2026-05-12T00:00:01Z",
        }

        decrypt_response = mock.Mock()
        decrypt_response.raise_for_status.return_value = None
        decrypt_response.json.return_value = {
            "plaintext": "eyJhY2Nlc3NUb2tlbiI6ICJhdCIsICJyZWZyZXNoVG9rZW4iOiAicnQiLCAiZXhwaXJlc0luIjogMzYwMCwgImFkbWluIjogeyJlbWFpbCI6ICJvcHNAZXhhbXBsZS5jb20ifX0=",
            "contentType": "application/json",
        }

        mock_post.side_effect = [start_response, encrypt_response, backend_response, decrypt_response]

        result = admin_login("ops@example.com", "secret")

        self.assertEqual("at", result.access_token)
        self.assertEqual("http://kms-api:5050/internal/security/sessions/start", mock_post.call_args_list[0][0][0])
        self.assertEqual("http://kms-api:5050/internal/security/encrypt", mock_post.call_args_list[1][0][0])
        self.assertEqual("http://backend-api:5000/admin/auth/login", mock_post.call_args_list[2][0][0])
        encrypt_payload = mock_post.call_args_list[1].kwargs["json"]
        self.assertEqual(
            "syn-sec-v1|request|POST|/admin/auth/login|11111111111111111111111111111111|1|",
            encrypt_payload["aad"],
        )
        self.assertEqual("client-to-server", encrypt_payload["direction"])
        backend_headers = mock_post.call_args_list[2].kwargs["headers"]
        self.assertEqual("11111111-1111-1111-1111-111111111111", backend_headers["X-Syn-Sec-Session"])
        self.assertEqual("1", backend_headers["X-Syn-Sec-Seq"])
        self.assertTrue(backend_headers["X-Syn-Sec-Nonce"])
        self.assertEqual("http://kms-api:5050/internal/security/decrypt", mock_post.call_args_list[3][0][0])
        decrypt_payload = mock_post.call_args_list[3].kwargs["json"]
        self.assertEqual(
            "syn-sec-v1|response|POST|/admin/auth/login|11111111111111111111111111111111|1|",
            decrypt_payload["aad"],
        )
        self.assertEqual("server-to-client", decrypt_payload["direction"])
        self.assertFalse(decrypt_payload["enforceReplay"])
        self.assertEqual(1, decrypt_payload["sequenceNumber"])
        self.assertTrue(decrypt_payload["replayNonce"])
