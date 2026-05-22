from unittest.mock import MagicMock, patch

import httpx
from django.test import TestCase, override_settings

from dashboard.services.admin_store_client import (
    bulk_reset_stock,
    cancel_flash_sale,
    create_flash_sale,
    delete_stock_policy,
    get_flash_sales,
    get_player_stock,
    get_purchase_analytics,
    get_stock_policies,
    override_player_stock,
    update_flash_sale,
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


@override_settings(**SETTINGS)
class TestUpsertStockPolicy(TestCase):
    @patch("dashboard.services.admin_store_client.httpx.put")
    def test_puts_correct_body(self, mock_put):
        payload = {"sku": "powerup:skip", "maxQuantityPerUser": 3, "resetInterval": "daily", "isActive": True}
        mock_put.return_value = _mock_response(200, payload)

        from dashboard.services.admin_store_client import upsert_stock_policy
        result = upsert_stock_policy(TOKEN, "powerup:skip", 3, "daily")

        mock_put.assert_called_once()
        call_kwargs = mock_put.call_args[1]
        self.assertEqual({"maxQuantityPerUser": 3, "resetInterval": "daily"}, call_kwargs["json"])
        self.assertIn("/admin/store/stock-policies/powerup:skip", mock_put.call_args[0][0])
        self.assertEqual(payload, result)

    @patch("dashboard.services.admin_store_client.httpx.put")
    def test_normalises_sku_to_lowercase(self, mock_put):
        mock_put.return_value = _mock_response(200, {})
        from dashboard.services.admin_store_client import upsert_stock_policy
        upsert_stock_policy(TOKEN, "POWER:SKIP", 1, "weekly")
        self.assertIn("/admin/store/stock-policies/power:skip", mock_put.call_args[0][0])

    @patch("dashboard.services.admin_store_client.httpx.put")
    def test_raises_on_http_error(self, mock_put):
        mock_put.return_value = _mock_response(422, None)
        from dashboard.services.admin_store_client import upsert_stock_policy
        with self.assertRaises(httpx.HTTPStatusError):
            upsert_stock_policy(TOKEN, "powerup:skip", 3, "daily")


@override_settings(**SETTINGS)
class TestDeleteStockPolicy(TestCase):
    @patch("dashboard.services.admin_store_client.httpx.delete")
    def test_calls_delete_endpoint(self, mock_delete):
        mock_delete.return_value = _mock_response(204)
        delete_stock_policy(TOKEN, "powerup:skip")
        call_url = mock_delete.call_args[0][0]
        self.assertIn("/admin/store/stock-policies/powerup:skip", call_url)

    @patch("dashboard.services.admin_store_client.httpx.delete")
    def test_normalizes_sku_to_lowercase(self, mock_delete):
        mock_delete.return_value = _mock_response(204)
        delete_stock_policy(TOKEN, "POWER:SKIP")
        call_url = mock_delete.call_args[0][0]
        self.assertIn("/admin/store/stock-policies/power:skip", call_url)

    @patch("dashboard.services.admin_store_client.httpx.delete")
    def test_raises_on_http_error(self, mock_delete):
        mock_delete.return_value = _mock_response(404)
        with self.assertRaises(httpx.HTTPStatusError):
            delete_stock_policy(TOKEN, "missing:sku")


@override_settings(**SETTINGS)
class TestCreateFlashSale(TestCase):
    @patch("dashboard.services.admin_store_client.httpx.post")
    def test_posts_correct_body(self, mock_post):
        payload = {"id": "sale-id", "sku": "powerup:skip", "discountPercent": 25}
        mock_post.return_value = _mock_response(201, payload)
        result = create_flash_sale(TOKEN, "powerup:skip", 25, "2026-06-01T00:00:00Z", "2026-06-02T00:00:00Z", "promo")
        call_url = mock_post.call_args[0][0]
        self.assertIn("/admin/store/flash-sales", call_url)
        _, kwargs = mock_post.call_args
        self.assertEqual(kwargs["json"]["sku"], "powerup:skip")
        self.assertEqual(kwargs["json"]["discountPercent"], 25)
        self.assertEqual(result, payload)

    @patch("dashboard.services.admin_store_client.httpx.post")
    def test_raises_on_http_error(self, mock_post):
        mock_post.return_value = _mock_response(400)
        with self.assertRaises(httpx.HTTPStatusError):
            create_flash_sale(TOKEN, "bad:sku", 200, "2026-06-01T00:00:00Z", "2026-06-02T00:00:00Z")


@override_settings(**SETTINGS)
class TestUpdateFlashSale(TestCase):
    @patch("dashboard.services.admin_store_client.httpx.put")
    def test_puts_correct_body(self, mock_put):
        payload = {"id": "sale-id", "sku": "powerup:skip", "discountPercent": 30}
        mock_put.return_value = _mock_response(200, payload)
        result = update_flash_sale(TOKEN, "sale-id", 30, "2026-06-01T00:00:00Z", "2026-06-02T00:00:00Z")
        call_url = mock_put.call_args[0][0]
        self.assertIn("/admin/store/flash-sales/sale-id", call_url)
        _, kwargs = mock_put.call_args
        self.assertEqual(kwargs["json"]["discountPercent"], 30)
        self.assertEqual(result, payload)

    @patch("dashboard.services.admin_store_client.httpx.put")
    def test_raises_on_already_started(self, mock_put):
        mock_put.return_value = _mock_response(409)
        with self.assertRaises(httpx.HTTPStatusError):
            update_flash_sale(TOKEN, "started-id", 20, "2026-05-01T00:00:00Z", "2026-05-02T00:00:00Z")


@override_settings(**SETTINGS)
class TestGetFlashSalesShowAll(TestCase):
    @patch("dashboard.services.admin_store_client.httpx.get")
    def test_passes_show_all_param(self, mock_get):
        mock_get.return_value = _mock_response(200, {"sales": []})
        get_flash_sales(TOKEN, show_all=True)
        _, kwargs = mock_get.call_args
        self.assertEqual(kwargs["params"], {"showAll": "true"})

    @patch("dashboard.services.admin_store_client.httpx.get")
    def test_no_param_when_show_all_false(self, mock_get):
        mock_get.return_value = _mock_response(200, {"sales": []})
        get_flash_sales(TOKEN, show_all=False)
        _, kwargs = mock_get.call_args
        self.assertEqual(kwargs["params"], {})


@override_settings(**SETTINGS)
class TestGetCatalog(TestCase):
    @patch("dashboard.services.admin_store_client.httpx.get")
    def test_returns_items(self, mock_get):
        payload = {"items": [{"id": "item-1", "sku": "powerup:hint", "name": "Hint Reveal", "isActive": True}], "total": 1}
        mock_get.return_value = _mock_response(200, payload)
        from dashboard.services.admin_store_client import get_catalog
        result = get_catalog(TOKEN)
        call_url = mock_get.call_args[0][0]
        self.assertIn("/admin/store/catalog", call_url)
        self.assertEqual(result["items"][0]["sku"], "powerup:hint")

    @patch("dashboard.services.admin_store_client.httpx.get")
    def test_passes_active_only_param(self, mock_get):
        mock_get.return_value = _mock_response(200, {"items": [], "total": 0})
        from dashboard.services.admin_store_client import get_catalog
        get_catalog(TOKEN, {"activeOnly": "true"})
        _, kwargs = mock_get.call_args
        self.assertEqual(kwargs["params"]["activeOnly"], "true")

    @patch("dashboard.services.admin_store_client.httpx.get")
    def test_raises_on_http_error(self, mock_get):
        mock_get.return_value = _mock_response(500)
        from dashboard.services.admin_store_client import get_catalog
        with self.assertRaises(httpx.HTTPStatusError):
            get_catalog(TOKEN)


@override_settings(**SETTINGS)
class TestCreateStoreItem(TestCase):
    @patch("dashboard.services.admin_store_client.httpx.post")
    def test_posts_correct_body(self, mock_post):
        payload = {"id": "item-uuid", "sku": "powerup:hint"}
        mock_post.return_value = _mock_response(201, payload)
        from dashboard.services.admin_store_client import create_store_item
        result = create_store_item(TOKEN, "powerup:hint", "Hint Reveal", None, "powerup", 30, 0, 1, 0, None, 0)
        call_url = mock_post.call_args[0][0]
        self.assertIn("/admin/store/catalog", call_url)
        _, kwargs = mock_post.call_args
        self.assertEqual(kwargs["json"]["sku"], "powerup:hint")
        self.assertEqual(kwargs["json"]["priceCoins"], 30)
        self.assertEqual(result, payload)

    @patch("dashboard.services.admin_store_client.httpx.post")
    def test_raises_on_conflict(self, mock_post):
        mock_post.return_value = _mock_response(409)
        from dashboard.services.admin_store_client import create_store_item
        with self.assertRaises(httpx.HTTPStatusError):
            create_store_item(TOKEN, "powerup:hint", "Hint", None, None, 0, 0, 1, 0, None, 0)


@override_settings(**SETTINGS)
class TestUpdateStoreItem(TestCase):
    @patch("dashboard.services.admin_store_client.httpx.patch")
    def test_patches_correct_fields(self, mock_patch):
        mock_patch.return_value = _mock_response(200, {"id": "item-uuid", "updatedAt": "2026-05-18"})
        from dashboard.services.admin_store_client import update_store_item
        result = update_store_item(TOKEN, "item-uuid", name="New Name", priceCoins=50)
        call_url = mock_patch.call_args[0][0]
        self.assertIn("/admin/store/catalog/item-uuid", call_url)
        _, kwargs = mock_patch.call_args
        self.assertEqual(kwargs["json"]["name"], "New Name")
        self.assertEqual(kwargs["json"]["priceCoins"], 50)

    @patch("dashboard.services.admin_store_client.httpx.patch")
    def test_raises_on_http_error(self, mock_patch):
        mock_patch.return_value = _mock_response(404)
        from dashboard.services.admin_store_client import update_store_item
        with self.assertRaises(httpx.HTTPStatusError):
            update_store_item(TOKEN, "missing-id", name="x")


@override_settings(**SETTINGS)
class TestDeleteStoreItem(TestCase):
    @patch("dashboard.services.admin_store_client.httpx.delete")
    def test_calls_correct_url(self, mock_delete):
        mock_delete.return_value = _mock_response(204)
        from dashboard.services.admin_store_client import delete_store_item
        delete_store_item(TOKEN, "item-uuid")
        call_url = mock_delete.call_args[0][0]
        self.assertIn("/admin/store/catalog/item-uuid", call_url)

    @patch("dashboard.services.admin_store_client.httpx.delete")
    def test_raises_on_already_inactive(self, mock_delete):
        mock_delete.return_value = _mock_response(409)
        from dashboard.services.admin_store_client import delete_store_item
        with self.assertRaises(httpx.HTTPStatusError):
            delete_store_item(TOKEN, "already-inactive")
