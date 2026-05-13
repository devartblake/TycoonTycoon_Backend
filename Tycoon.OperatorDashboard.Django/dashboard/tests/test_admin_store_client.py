from unittest.mock import MagicMock, patch

import httpx
from django.test import TestCase, override_settings

from dashboard.services.admin_store_client import (
    bulk_reset_stock,
    cancel_flash_sale,
    get_flash_sales,
    get_player_stock,
    get_purchase_analytics,
    get_stock_policies,
    override_player_stock,
)

DOTNET_BASE = "http://backend-api:5000"
SETTINGS = {
    "DOTNET_API_BASE_URL": DOTNET_BASE,
    "ADMIN_OPS_KEY": "test-ops-key",
    "ADMIN_OPS_HEADER": "X-Admin-Ops-Key",
    "API_REQUEST_TIMEOUT_SECONDS": 5,
}

TOKEN = "test-access-token"


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
class TestGetStockPolicies(TestCase):
    @patch("dashboard.services.admin_store_client.httpx.get")
    def test_returns_policies(self, mock_get):
        payload = {"policies": [{"sku": "powerup:skip", "maxQuantityPerUser": 5, "resetInterval": "daily", "isActive": True}]}
        mock_get.return_value = _mock_response(200, payload)

        result = get_stock_policies(TOKEN)

        mock_get.assert_called_once()
        call_url = mock_get.call_args[0][0]
        assert call_url == f"{DOTNET_BASE}/admin/store/stock-policies"
        assert result["policies"][0]["sku"] == "powerup:skip"

    @patch("dashboard.services.admin_store_client.httpx.get")
    def test_passes_params(self, mock_get):
        mock_get.return_value = _mock_response(200, {"policies": []})
        get_stock_policies(TOKEN, {"activeOnly": "true", "sku": "powerup:skip"})
        _, kwargs = mock_get.call_args
        assert kwargs["params"]["activeOnly"] == "true"
        assert kwargs["params"]["sku"] == "powerup:skip"

    @patch("dashboard.services.admin_store_client.httpx.get")
    def test_strips_empty_params(self, mock_get):
        mock_get.return_value = _mock_response(200, {"policies": []})
        get_stock_policies(TOKEN, {"activeOnly": None, "sku": ""})
        _, kwargs = mock_get.call_args
        assert "activeOnly" not in kwargs["params"]
        assert "sku" not in kwargs["params"]

    @patch("dashboard.services.admin_store_client.httpx.get")
    def test_raises_on_http_error(self, mock_get):
        mock_get.return_value = _mock_response(403)
        with self.assertRaises(httpx.HTTPStatusError):
            get_stock_policies(TOKEN)


@override_settings(**SETTINGS)
class TestPlayerStock(TestCase):
    @patch("dashboard.services.admin_store_client.httpx.get")
    def test_get_player_stock_calls_endpoint(self, mock_get):
        player_id = "00000000-0000-0000-0000-000000000001"
        payload = {"playerId": player_id, "items": [{"sku": "powerup:skip"}]}
        mock_get.return_value = _mock_response(200, payload)

        result = get_player_stock(TOKEN, player_id)

        call_url = mock_get.call_args[0][0]
        _, kwargs = mock_get.call_args
        assert call_url == f"{DOTNET_BASE}/admin/store/player-stock/{player_id}"
        assert kwargs["headers"]["Authorization"] == f"Bearer {TOKEN}"
        assert kwargs["headers"]["X-Admin-Ops-Key"] == "test-ops-key"
        assert result["items"][0]["sku"] == "powerup:skip"

    @patch("dashboard.services.admin_store_client.httpx.get")
    def test_get_player_stock_raises_on_http_error(self, mock_get):
        mock_get.return_value = _mock_response(404)
        with self.assertRaises(httpx.HTTPStatusError):
            get_player_stock(TOKEN, "00000000-0000-0000-0000-000000000001")

    @patch("dashboard.services.admin_store_client.httpx.post")
    def test_override_player_stock_posts_integer_override(self, mock_post):
        player_id = "00000000-0000-0000-0000-000000000001"
        mock_post.return_value = _mock_response(200, {"sku": "powerup:skip", "effectiveMaxQuantity": 9})

        result = override_player_stock(TOKEN, player_id, "powerup:skip", 9, "ticket-1")

        call_url = mock_post.call_args[0][0]
        _, kwargs = mock_post.call_args
        assert call_url == f"{DOTNET_BASE}/admin/store/player-stock/{player_id}/override"
        assert kwargs["json"] == {
            "sku": "powerup:skip",
            "effectiveMaxQuantity": 9,
            "reason": "ticket-1",
        }
        assert result["effectiveMaxQuantity"] == 9

    @patch("dashboard.services.admin_store_client.httpx.post")
    def test_override_player_stock_posts_null_to_clear_override(self, mock_post):
        player_id = "00000000-0000-0000-0000-000000000001"
        mock_post.return_value = _mock_response(200, {"sku": "powerup:skip", "effectiveMaxQuantity": None})

        override_player_stock(TOKEN, player_id, "powerup:skip", None, "")

        _, kwargs = mock_post.call_args
        assert kwargs["json"]["effectiveMaxQuantity"] is None
        assert kwargs["json"]["reason"] is None

    @patch("dashboard.services.admin_store_client.httpx.post")
    def test_bulk_reset_stock_posts_skus(self, mock_post):
        mock_post.return_value = _mock_response(200, {"playersAffected": 2})

        result = bulk_reset_stock(TOKEN, ["powerup:skip", "powerup:hint"], "support reset")

        call_url = mock_post.call_args[0][0]
        _, kwargs = mock_post.call_args
        assert call_url == f"{DOTNET_BASE}/admin/store/stock-policies/bulk-reset"
        assert kwargs["json"] == {
            "skus": ["powerup:skip", "powerup:hint"],
            "reason": "support reset",
        }
        assert result["playersAffected"] == 2


@override_settings(**SETTINGS)
class TestGetFlashSales(TestCase):
    @patch("dashboard.services.admin_store_client.httpx.get")
    def test_returns_sales(self, mock_get):
        payload = {"sales": [{"id": "abc", "sku": "powerup:hint", "discountPercent": 30, "isActive": True}]}
        mock_get.return_value = _mock_response(200, payload)

        result = get_flash_sales(TOKEN)

        call_url = mock_get.call_args[0][0]
        assert call_url == f"{DOTNET_BASE}/admin/store/flash-sales"
        assert result["sales"][0]["sku"] == "powerup:hint"

    @patch("dashboard.services.admin_store_client.httpx.get")
    def test_raises_on_http_error(self, mock_get):
        mock_get.return_value = _mock_response(500)
        with self.assertRaises(httpx.HTTPStatusError):
            get_flash_sales(TOKEN)


@override_settings(**SETTINGS)
class TestCancelFlashSale(TestCase):
    @patch("dashboard.services.admin_store_client.httpx.delete")
    def test_calls_delete_endpoint(self, mock_delete):
        mock_delete.return_value = _mock_response(204)
        cancel_flash_sale(TOKEN, "sale-uuid-123")
        call_url = mock_delete.call_args[0][0]
        assert call_url == f"{DOTNET_BASE}/admin/store/flash-sales/sale-uuid-123"

    @patch("dashboard.services.admin_store_client.httpx.delete")
    def test_raises_on_not_found(self, mock_delete):
        mock_delete.return_value = _mock_response(404)
        with self.assertRaises(httpx.HTTPStatusError):
            cancel_flash_sale(TOKEN, "missing-id")

    @patch("dashboard.services.admin_store_client.httpx.delete")
    def test_raises_on_already_cancelled(self, mock_delete):
        mock_delete.return_value = _mock_response(409)
        with self.assertRaises(httpx.HTTPStatusError):
            cancel_flash_sale(TOKEN, "already-cancelled-id")


@override_settings(**SETTINGS)
class TestGetPurchaseAnalytics(TestCase):
    @patch("dashboard.services.admin_store_client.httpx.get")
    def test_returns_analytics(self, mock_get):
        payload = {
            "totalPurchases": 1842,
            "totalCoinsSpent": 94100,
            "topSkus": [{"sku": "powerup:skip", "purchaseCount": 731}],
        }
        mock_get.return_value = _mock_response(200, payload)

        result = get_purchase_analytics(TOKEN)

        call_url = mock_get.call_args[0][0]
        assert call_url == f"{DOTNET_BASE}/admin/store/analytics/purchases"
        assert result["totalPurchases"] == 1842

    @patch("dashboard.services.admin_store_client.httpx.get")
    def test_passes_date_range(self, mock_get):
        mock_get.return_value = _mock_response(200, {})
        get_purchase_analytics(TOKEN, {"from": "2026-04-01T00:00:00Z", "to": "2026-04-28T23:59:59Z"})
        _, kwargs = mock_get.call_args
        assert kwargs["params"]["from"] == "2026-04-01T00:00:00Z"

    @patch("dashboard.services.admin_store_client.httpx.get")
    def test_raises_on_http_error(self, mock_get):
        mock_get.return_value = _mock_response(500)
        with self.assertRaises(httpx.HTTPStatusError):
            get_purchase_analytics(TOKEN)
