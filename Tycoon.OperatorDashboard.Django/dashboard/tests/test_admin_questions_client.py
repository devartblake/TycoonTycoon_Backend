from unittest.mock import MagicMock, patch

import httpx
from django.test import TestCase, override_settings

from dashboard.services.admin_questions_client import approve_question, list_questions, reject_question

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
class TestListQuestions(TestCase):
    @patch("dashboard.services.admin_questions_client.httpx.get")
    def test_returns_page_envelope(self, mock_get):
        payload = {"items": [{"id": "q1", "text": "Q?", "status": "Pending"}], "page": 1, "total": 1}
        mock_get.return_value = _mock_response(200, payload)
        result = list_questions(TOKEN)
        assert mock_get.call_args[0][0] == f"{DOTNET_BASE}/admin/questions"
        assert result["items"][0]["id"] == "q1"

    @patch("dashboard.services.admin_questions_client.httpx.get")
    def test_passes_params(self, mock_get):
        mock_get.return_value = _mock_response(200, {"items": []})
        list_questions(TOKEN, {"status": "Pending", "category": "science", "page": 2})
        _, kwargs = mock_get.call_args
        assert kwargs["params"]["status"] == "Pending"
        assert kwargs["params"]["category"] == "science"

    @patch("dashboard.services.admin_questions_client.httpx.get")
    def test_raises_on_http_error(self, mock_get):
        mock_get.return_value = _mock_response(403)
        with self.assertRaises(httpx.HTTPStatusError):
            list_questions(TOKEN)


@override_settings(**SETTINGS)
class TestApproveQuestion(TestCase):
    @patch("dashboard.services.admin_questions_client.httpx.post")
    def test_calls_approve_endpoint(self, mock_post):
        mock_post.return_value = _mock_response(200, {"id": "q1", "status": "Approved"})
        result = approve_question(TOKEN, "q1")
        assert mock_post.call_args[0][0] == f"{DOTNET_BASE}/admin/questions/q1/approve"
        assert result["status"] == "Approved"

    @patch("dashboard.services.admin_questions_client.httpx.post")
    def test_raises_on_not_found(self, mock_post):
        mock_post.return_value = _mock_response(404)
        with self.assertRaises(httpx.HTTPStatusError):
            approve_question(TOKEN, "missing")


@override_settings(**SETTINGS)
class TestRejectQuestion(TestCase):
    @patch("dashboard.services.admin_questions_client.httpx.post")
    def test_calls_reject_endpoint(self, mock_post):
        mock_post.return_value = _mock_response(200, {"id": "q1", "status": "Rejected"})
        result = reject_question(TOKEN, "q1")
        assert mock_post.call_args[0][0] == f"{DOTNET_BASE}/admin/questions/q1/reject"
        assert result["status"] == "Rejected"

    @patch("dashboard.services.admin_questions_client.httpx.post")
    def test_raises_on_http_error(self, mock_post):
        mock_post.return_value = _mock_response(500)
        with self.assertRaises(httpx.HTTPStatusError):
            reject_question(TOKEN, "q1")
