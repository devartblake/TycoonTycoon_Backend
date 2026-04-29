from unittest.mock import MagicMock, patch

import httpx
from django.test import TestCase, override_settings

from dashboard.services.admin_store_client import (
    cancel_flash_sale,
    get_flash_sales,
    get_purchase_analytics,
    get_stock_policies,
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
