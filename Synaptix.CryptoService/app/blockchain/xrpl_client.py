"""
XRP Ledger client.

Sends XRP Payment transactions from the treasury wallet to player wallets.

Important XRPL-specific behaviours handled here:
  - Destination accounts must have a minimum reserve (~10 XRP). The transfer
    will fail if the destination account does not exist and the payment amount
    is below the base reserve. We let the exception propagate so the settlement
    worker can retry or reject appropriately.
  - XRP amounts are in drops (1 XRP = 1_000_000 drops).
  - The client uses a WebSocket connection via xrpl.asyncio.
"""
from __future__ import annotations

import logging

from xrpl.asyncio.clients import AsyncWebsocketClient
from xrpl.asyncio.transaction import autofill_and_sign, submit_and_wait
from xrpl.models.transactions import Payment
from xrpl.wallet import Wallet

from app.blockchain.base import BlockchainClient
from app.blockchain.address_utils import validate_xrp_address

log = logging.getLogger(__name__)

DROPS_PER_XRP = 1_000_000


class XrplClient(BlockchainClient):
    def __init__(self, node_url: str, treasury_seed: str) -> None:
        self._node_url = node_url
        self._wallet = Wallet.from_seed(treasury_seed)

    @property
    def network_name(self) -> str:
        return "xrp"

    def validate_address(self, address: str) -> bool:
        return validate_xrp_address(address)

    async def get_balance(self, address: str) -> float:
        from xrpl.asyncio.account import get_balance as xrpl_get_balance
        async with AsyncWebsocketClient(self._node_url) as client:
            drops = await xrpl_get_balance(address, client)
        return drops / DROPS_PER_XRP

    async def transfer(self, to_address: str, amount: float) -> str:
        drops = str(int(amount * DROPS_PER_XRP))
        payment = Payment(
            account=self._wallet.address,
            destination=to_address,
            amount=drops,
        )
        async with AsyncWebsocketClient(self._node_url) as client:
            signed = await autofill_and_sign(payment, client, self._wallet)
            result = await submit_and_wait(signed, client)

        tx_hash: str = result.result["hash"]
        log.info("XRP payment sent: → %s (%s XRP) hash=%s", to_address, amount, tx_hash)
        return tx_hash
