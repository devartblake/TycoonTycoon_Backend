"""
Solana blockchain client.

Handles two asset types:
  - SOL  : native Solana coin (system program transfer)
  - SNX  : Synaptix SPL fungible token (spl-token transfer_checked)

The treasury keypair is loaded from a JSON file (array of 64 bytes),
the standard format produced by the Solana CLI (`solana-keygen new`).
"""
from __future__ import annotations

import json
import logging
from pathlib import Path

from solana.rpc.async_api import AsyncClient
from solana.rpc.commitment import Confirmed
from solders.keypair import Keypair
from solders.pubkey import Pubkey
from solders.system_program import TransferParams, transfer
from solders.transaction import Transaction

from app.blockchain.base import BlockchainClient
from app.blockchain.address_utils import validate_solana_address

log = logging.getLogger(__name__)

LAMPORTS_PER_SOL = 1_000_000_000


def _load_keypair(path: str) -> Keypair:
    data = json.loads(Path(path).read_bytes())
    return Keypair.from_bytes(bytes(data))


class SolanaClient(BlockchainClient):
    """Native SOL transfers."""

    def __init__(self, rpc_url: str, keypair_path: str) -> None:
        self._rpc_url = rpc_url
        self._keypair = _load_keypair(keypair_path)
        self._rpc: AsyncClient | None = None

    async def start(self) -> None:
        self._rpc = AsyncClient(self._rpc_url, commitment=Confirmed)

    async def stop(self) -> None:
        if self._rpc:
            await self._rpc.close()

    def _client(self) -> AsyncClient:
        if self._rpc is None:
            raise RuntimeError("SolanaClient not started")
        return self._rpc

    @property
    def network_name(self) -> str:
        return "solana"

    def validate_address(self, address: str) -> bool:
        return validate_solana_address(address)

    async def get_balance(self, address: str) -> float:
        pubkey = Pubkey.from_string(address)
        resp = await self._client().get_balance(pubkey)
        return resp.value / LAMPORTS_PER_SOL

    async def transfer(self, to_address: str, amount: float) -> str:
        lamports = int(amount * LAMPORTS_PER_SOL)
        to_pubkey = Pubkey.from_string(to_address)

        blockhash_resp = await self._client().get_latest_blockhash()
        recent_blockhash = blockhash_resp.value.blockhash

        ix = transfer(TransferParams(
            from_pubkey=self._keypair.pubkey(),
            to_pubkey=to_pubkey,
            lamports=lamports,
        ))
        tx = Transaction.new_signed_with_payer(
            [ix],
            payer=self._keypair.pubkey(),
            signing_keypairs=[self._keypair],
            recent_blockhash=recent_blockhash,
        )
        resp = await self._client().send_transaction(tx)
        sig = str(resp.value)
        log.info("SOL transfer sent: %s → %s (%s lamports) sig=%s",
                 self._keypair.pubkey(), to_address, lamports, sig)
        return sig


class SnxSplClient(BlockchainClient):
    """
    SNX (Synaptix SPL token) transfers.

    Uses the same treasury keypair as the SOL client but routes through the
    SPL token program (transfer_checked instruction).
    """

    def __init__(
        self,
        rpc_url: str,
        keypair_path: str,
        mint_address: str,
        decimals: int = 6,
    ) -> None:
        self._rpc_url = rpc_url
        self._keypair = _load_keypair(keypair_path)
        self._mint = Pubkey.from_string(mint_address)
        self._decimals = decimals
        self._rpc: AsyncClient | None = None

    async def start(self) -> None:
        self._rpc = AsyncClient(self._rpc_url, commitment=Confirmed)

    async def stop(self) -> None:
        if self._rpc:
            await self._rpc.close()

    def _client(self) -> AsyncClient:
        if self._rpc is None:
            raise RuntimeError("SnxSplClient not started")
        return self._rpc

    @property
    def network_name(self) -> str:
        return "snx"

    def validate_address(self, address: str) -> bool:
        return validate_solana_address(address)

    async def get_balance(self, address: str) -> float:
        from spl.token.async_client import AsyncToken
        from spl.token.constants import TOKEN_PROGRAM_ID
        token = AsyncToken(self._client(), self._mint, TOKEN_PROGRAM_ID, self._keypair)
        owner = Pubkey.from_string(address)
        accounts = await token.get_accounts_by_owner(owner)
        if not accounts.value:
            return 0.0
        raw = accounts.value[0].account.data.parsed["info"]["tokenAmount"]["amount"]
        return int(raw) / (10 ** self._decimals)

    async def transfer(self, to_address: str, amount: float) -> str:
        from spl.token.async_client import AsyncToken
        from spl.token.constants import TOKEN_PROGRAM_ID
        from spl.token.instructions import get_associated_token_address

        raw_amount = int(amount * (10 ** self._decimals))
        owner = Pubkey.from_string(to_address)

        source_ata = get_associated_token_address(self._keypair.pubkey(), self._mint)
        dest_ata = get_associated_token_address(owner, self._mint)

        token = AsyncToken(self._client(), self._mint, TOKEN_PROGRAM_ID, self._keypair)
        resp = await token.transfer_checked(
            source=source_ata,
            dest=dest_ata,
            owner=self._keypair,
            amount=raw_amount,
            decimals=self._decimals,
        )
        sig = str(resp.value)
        log.info("SNX transfer sent: → %s (%s SNX) sig=%s", to_address, amount, sig)
        return sig
