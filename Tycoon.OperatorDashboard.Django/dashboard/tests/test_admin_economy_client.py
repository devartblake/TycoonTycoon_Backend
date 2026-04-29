from unittest.mock import MagicMock, patch

import httpx
from django.test import TestCase, override_settings

from dashboard.services.admin_economy_client import create_economy_transaction, get_economy_history

DOTNET_BASE = "http://backend-api:5000"
SETTINGS = {
    "DOTNET_API_BASE_URL": DOTNET_BASE,
    "ADMIN_OPS_KEY": "test-ops-key",
    "ADMIN_OPS_HEADER": "X-Admin-Ops-Key",
    "API_REQUEST_TIMEOUT_SECONDS": 5,
}
TOKEN = "test-access-token"
PLAYER_ID = "3fa85f64-5717-4562-b3fc-2c963f66afa6"


def _mock_response(status_code: int, body: dict | None = None):
    response = MagicMock(spec=httpx.Response)
    response.status_code = status_code
    response.json.return_value = body or {}
    if status_code >= 400:
        response.raise_for_status.side_effect = httpx.HTTPStatusError(
            "error", request=MagicMock(), response=response
        )
    else:
        response.raise_for_status.return_value = None
    return response


@override_settings(**SETTINGS)
class TestGetEconomyHistory(TestCase):
    @patch("dashboard.services.admin_economy_client.httpx.get")
    def test_returns_history(self, mock_get):
        payload = {"items": [{"type": "grant", "amount": 100}], "page": 1, "total": 1}
        mock_get.return_value = _mock_response(200, payload)
        result = get_economy_history(TOKEN, PLAYER_ID)
        assert mock_get.call_args[0][0] == f"{DOTNET_BASE}/admin/economy/history/{PLAYER_ID}"
        assert result["items"][0]["type"] == "grant"

    @patch("dashboard.services.admin_economy_client.httpx.get")
    def test_passes_pagination_params(self, mock_get):
        mock_get.return_value = _mock_response(200, {"items": []})
        get_economy_history(TOKEN, PLAYER_ID, {"page": 2, "pageSize": 25})
        _, kwargs = mock_get.call_args
        assert kwargs["params"]["page"] == 2

    @patch("dashboard.services.admin_economy_client.httpx.get")
    def test_raises_on_http_error(self, mock_get):
        mock_get.return_value = _mock_response(404)
        with self.assertRaises(httpx.HTTPStatusError):
            get_economy_history(TOKEN, PLAYER_ID)


@override_settings(**SETTINGS)
class TestCreateEconomyTransaction(TestCase):
    @patch("dashboard.services.admin_economy_client.httpx.post")
    def test_posts_transaction(self, mock_post):
        mock_post.return_value = _mock_response(200, {"transactionId": "tx1", "amount": 100})
        payload = {"playerId": PLAYER_ID, "amount": 100, "type": "grant", "reason": "support #1"}
        result = create_economy_transaction(TOKEN, payload)
        assert mock_post.call_args[0][0] == f"{DOTNET_BASE}/admin/economy/transactions"
        assert mock_post.call_args[1]["json"] == payload
        assert result["transactionId"] == "tx1"

    @patch("dashboard.services.admin_economy_client.httpx.post")
    def test_raises_on_validation_error(self, mock_post):
        mock_post.return_value = _mock_response(400)
        with self.assertRaises(httpx.HTTPStatusError):
            create_economy_transaction(TOKEN, {})

    @patch("dashboard.services.admin_economy_client.httpx.post")
    def test_raises_on_server_error(self, mock_post):
        mock_post.return_value = _mock_response(500)
        with self.assertRaises(httpx.HTTPStatusError):
            create_economy_transaction(TOKEN, {"playerId": PLAYER_ID, "amount": 50})
