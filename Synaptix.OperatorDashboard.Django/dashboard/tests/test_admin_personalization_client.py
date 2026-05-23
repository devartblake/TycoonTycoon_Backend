from unittest.mock import MagicMock, patch

import httpx
from django.test import TestCase, override_settings

from dashboard.services.admin_personalization_client import (
    get_personalization_archetypes,
    get_personalization_summary,
    get_player_debug,
    get_player_profile,
    get_recommendation_performance,
    list_rules,
    recalculate_player,
    reset_player,
    upsert_rule,
)

DOTNET_BASE = "http://backend-api:5000"
SETTINGS = {
    "DOTNET_API_BASE_URL": DOTNET_BASE,
    "ADMIN_OPS_KEY": "test-ops-key",
    "ADMIN_OPS_HEADER": "X-Admin-Ops-Key",
    "API_REQUEST_TIMEOUT_SECONDS": 5,
}

TOKEN = "test-access-token"


def _mock_response(status_code: int, body=None):
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
class TestAdminPersonalizationClient(TestCase):
    @patch("dashboard.services.admin_personalization_client.httpx.get")
    def test_get_summary_calls_expected_endpoint(self, mock_get):
        mock_get.return_value = _mock_response(200, {"totalProfiles": 4})

        result = get_personalization_summary(TOKEN)

        self.assertEqual(4, result["totalProfiles"])
        self.assertEqual(f"{DOTNET_BASE}/admin/personalization/summary", mock_get.call_args[0][0])
        self.assertEqual(f"Bearer {TOKEN}", mock_get.call_args.kwargs["headers"]["Authorization"])
        self.assertEqual("test-ops-key", mock_get.call_args.kwargs["headers"]["X-Admin-Ops-Key"])

    @patch("dashboard.services.admin_personalization_client.httpx.get")
    def test_get_archetypes_calls_expected_endpoint(self, mock_get):
        mock_get.return_value = _mock_response(200, [{"archetype": "new_player", "count": 1}])

        result = get_personalization_archetypes(TOKEN)

        self.assertEqual("new_player", result[0]["archetype"])
        self.assertEqual(f"{DOTNET_BASE}/admin/personalization/archetypes", mock_get.call_args[0][0])

    @patch("dashboard.services.admin_personalization_client.httpx.get")
    def test_get_recommendation_performance_calls_expected_endpoint(self, mock_get):
        mock_get.return_value = _mock_response(200, [{"type": "mission", "total": 3}])

        result = get_recommendation_performance(TOKEN)

        self.assertEqual(3, result[0]["total"])
        self.assertEqual(f"{DOTNET_BASE}/admin/personalization/recommendations/performance", mock_get.call_args[0][0])

    @patch("dashboard.services.admin_personalization_client.httpx.get")
    def test_player_profile_and_debug_use_player_id(self, mock_get):
        mock_get.side_effect = [
            _mock_response(200, {"playerId": "p1"}),
            _mock_response(200, {"recentEvents": []}),
        ]

        profile = get_player_profile(TOKEN, "p1")
        debug = get_player_debug(TOKEN, "p1")

        self.assertEqual("p1", profile["playerId"])
        self.assertEqual([], debug["recentEvents"])
        self.assertEqual(f"{DOTNET_BASE}/admin/personalization/player/p1", mock_get.call_args_list[0][0][0])
        self.assertEqual(f"{DOTNET_BASE}/admin/personalization/debug/p1", mock_get.call_args_list[1][0][0])

    @patch("dashboard.services.admin_personalization_client.httpx.post")
    def test_recalculate_and_reset_post_to_player_actions(self, mock_post):
        mock_post.side_effect = [
            _mock_response(200, {"playerId": "p1"}),
            _mock_response(200, {"reset": True}),
        ]

        recalculate_player(TOKEN, "p1")
        reset_player(TOKEN, "p1")

        self.assertEqual(f"{DOTNET_BASE}/admin/personalization/player/p1/recalculate", mock_post.call_args_list[0][0][0])
        self.assertEqual(f"{DOTNET_BASE}/admin/personalization/player/p1/reset", mock_post.call_args_list[1][0][0])

    @patch("dashboard.services.admin_personalization_client.httpx.get")
    def test_list_rules_raises_on_http_error(self, mock_get):
        mock_get.return_value = _mock_response(403)

        with self.assertRaises(httpx.HTTPStatusError):
            list_rules(TOKEN)

    @patch("dashboard.services.admin_personalization_client.httpx.put")
    def test_upsert_rule_sends_json_payload(self, mock_put):
        mock_put.return_value = _mock_response(200, {"ruleKey": "fatigue", "isEnabled": True})
        payload = {"isEnabled": True, "rule": {"maxScore": 0.65}}

        result = upsert_rule(TOKEN, "fatigue", payload)

        self.assertEqual("fatigue", result["ruleKey"])
        self.assertEqual(f"{DOTNET_BASE}/admin/personalization/rules/fatigue", mock_put.call_args[0][0])
        self.assertEqual(payload, mock_put.call_args.kwargs["json"])
