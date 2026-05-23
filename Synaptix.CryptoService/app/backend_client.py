"""
HTTP wrapper for .NET /crypto/* admin endpoints.
All calls include the X-Admin-Ops-Key header.
"""
from __future__ import annotations

from dataclasses import dataclass
from typing import Any

import httpx

from app.config import settings


def _admin_headers() -> dict[str, str]:
    headers: dict[str, str] = {"Content-Type": "application/json"}
    if settings.admin_ops_key:
        headers["X-Admin-Ops-Key"] = settings.admin_ops_key
    return headers


@dataclass
class PendingWithdrawal:
    transaction_id: str
    player_id: str
    units: float
    to_wallet_address: str
    network: str
    requested_at_utc: str


@dataclass
class PendingWithdrawalsPage:
    page: int
    page_size: int
    total: int
    items: list[PendingWithdrawal]


class BackendClient:
    def __init__(self, client: httpx.AsyncClient) -> None:
        self._client = client
        self._base = settings.backend_base_url.rstrip("/")

    async def get_pending_withdrawals(
        self, page: int = 1, page_size: int = 50
    ) -> PendingWithdrawalsPage:
        resp = await self._client.get(
            f"{self._base}/crypto/withdraw/pending",
            params={"page": page, "pageSize": page_size},
            headers=_admin_headers(),
        )
        resp.raise_for_status()
        data = resp.json()
        items = [
            PendingWithdrawal(
                transaction_id=i["transactionId"],
                player_id=i["playerId"],
                units=i["units"],
                to_wallet_address=i["toWalletAddress"],
                network=i.get("network", "solana"),
                requested_at_utc=i["requestedAtUtc"],
            )
            for i in data.get("items", [])
        ]
        return PendingWithdrawalsPage(
            page=data["page"],
            page_size=data["pageSize"],
            total=data["total"],
            items=items,
        )

    async def approve_withdrawal(self, transaction_id: str) -> dict[str, Any]:
        resp = await self._client.post(
            f"{self._base}/crypto/withdraw/{transaction_id}/approve",
            headers=_admin_headers(),
        )
        resp.raise_for_status()
        return resp.json()

    async def reject_withdrawal(self, transaction_id: str) -> dict[str, Any]:
        resp = await self._client.post(
            f"{self._base}/crypto/withdraw/{transaction_id}/reject",
            headers=_admin_headers(),
        )
        resp.raise_for_status()
        return resp.json()
