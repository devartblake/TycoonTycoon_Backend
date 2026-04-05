import httpx
from django.test import SimpleTestCase

from dashboard.services.upstream_error import (
    build_upstream_http_error_response,
    build_upstream_unavailable_response,
)


class UpstreamErrorTests(SimpleTestCase):
    def test_build_upstream_http_error_response_uses_backend_payload(self):
        request = httpx.Request("GET", "http://backend-api/admin/users")
        response = httpx.Response(status_code=422, request=request, json={"code": "VALIDATION_ERROR", "message": "bad"})
        ex = httpx.HTTPStatusError("bad", request=request, response=response)

        dj_response = build_upstream_http_error_response(ex, "fallback")

        self.assertEqual(422, dj_response.status_code)
        self.assertIn("VALIDATION_ERROR", dj_response.content.decode())

    def test_build_upstream_unavailable_response(self):
        dj_response = build_upstream_unavailable_response("down")

        self.assertEqual(503, dj_response.status_code)
        self.assertIn("UPSTREAM_UNAVAILABLE", dj_response.content.decode())
