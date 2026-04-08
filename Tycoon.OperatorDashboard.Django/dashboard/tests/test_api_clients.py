from unittest import mock

import httpx
from django.test import SimpleTestCase, override_settings

from dashboard.services.api_clients import get_overall_status, list_service_statuses


class ApiClientsTests(SimpleTestCase):
    @override_settings(
        DOTNET_API_BASE_URL="http://dotnet",
        FASTAPI_BASE_URL="http://fastapi",
        MINIO_BASE_URL="http://minio",
    )
    @mock.patch("dashboard.services.api_clients.httpx.get")
    def test_list_service_statuses_all_healthy(self, mock_get):
        dotnet_response = mock.Mock()
        dotnet_response.text = "{}"
        dotnet_response.json.return_value = {"status": "ok"}
        dotnet_response.raise_for_status.return_value = None

        fastapi_response = mock.Mock()
        fastapi_response.text = "{}"
        fastapi_response.json.return_value = {"status": "ok"}
        fastapi_response.raise_for_status.return_value = None

        minio_response = mock.Mock()
        minio_response.text = "OK"
        minio_response.raise_for_status.return_value = None

        mock_get.side_effect = [dotnet_response, fastapi_response, minio_response]

        statuses = list_service_statuses()

        self.assertEqual(3, len(statuses))
        self.assertEqual("healthy", statuses[0].status)
        self.assertEqual("healthy", statuses[1].status)
        self.assertEqual("healthy", statuses[2].status)
        self.assertEqual("healthy", get_overall_status(statuses))

    @override_settings(
        DOTNET_API_BASE_URL="http://dotnet",
        FASTAPI_BASE_URL="http://fastapi",
        MINIO_BASE_URL="http://minio",
    )
    @mock.patch("dashboard.services.api_clients.httpx.get")
    def test_list_service_statuses_when_one_service_offline(self, mock_get):
        dotnet_response = mock.Mock()
        dotnet_response.text = "{}"
        dotnet_response.json.return_value = {"status": "ok"}
        dotnet_response.raise_for_status.return_value = None

        fastapi_response = mock.Mock()
        fastapi_response.text = "{}"
        fastapi_response.json.return_value = {"status": "ok"}
        fastapi_response.raise_for_status.return_value = None

        mock_get.side_effect = [
            dotnet_response,
            fastapi_response,
            httpx.RequestError("network down"),
        ]

        statuses = list_service_statuses()

        self.assertEqual("healthy", statuses[0].status)
        self.assertEqual("healthy", statuses[1].status)
        self.assertEqual("offline", statuses[2].status)
        self.assertEqual("offline", get_overall_status(statuses))
