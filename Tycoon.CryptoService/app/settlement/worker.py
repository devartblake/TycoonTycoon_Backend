"""
Settlement worker — background APScheduler job.

Every SETTLEMENT_POLL_INTERVAL_SECONDS:
  1. Fetches all Pending withdrawals from the .NET backend.
  2. For each withdrawal:
     a. Skip if already processed (idempotency via MongoDB log).
     b. Validate the destination address for the target network.
     c. Submit the on-chain transfer.
     d. On success  → call .NET approve, record "settled" in MongoDB.
     e. On failure  → increment retry counter.
                       If max retries reached → call .NET reject, record "failed".
"""
from __future__ import annotations

import logging

from app import settlement
from app.backend_client import BackendClient
from app.config import settings
from app.settlement import store
from app.settlement.router import get_client

log = logging.getLogger(__name__)


async def run_settlement_cycle(app_state) -> None:
    backend: BackendClient = app_state.backend_client
    db = app_state.mongo_db

    try:
        page = await backend.get_pending_withdrawals(page=1, page_size=100)
    except Exception as exc:
        log.error("Failed to fetch pending withdrawals from .NET: %s", exc)
        return

    if not page.items:
        return

    log.info("Settlement cycle: %d pending withdrawal(s) found.", len(page.items))

    for w in page.items:
        tx_id = w.transaction_id

        if await store.is_settled(db, tx_id):
            continue

        # Resolve network client
        try:
            client = get_client(w.network, app_state)
        except ValueError as exc:
            log.warning("Withdrawal %s: %s — rejecting.", tx_id, exc)
            await _reject(backend, db, w, error=str(exc), status="rejected")
            continue

        # Validate address format
        if not client.validate_address(w.to_wallet_address):
            log.warning("Withdrawal %s: invalid %s address %r — rejecting.",
                        tx_id, w.network, w.to_wallet_address)
            await _reject(backend, db, w, error="invalid address", status="rejected")
            continue

        # Attempt on-chain transfer
        try:
            on_chain_id = await client.transfer(w.to_wallet_address, w.units)
        except Exception as exc:
            log.exception("Withdrawal %s: on-chain transfer failed: %s", tx_id, exc)
            retry_count = await store.increment_retry(db, tx_id)
            if retry_count >= settings.settlement_max_retries:
                log.warning("Withdrawal %s: max retries reached — rejecting.", tx_id)
                await _reject(backend, db, w, error=str(exc), status="failed")
            continue

        # Success — approve in .NET and record settlement
        try:
            await backend.approve_withdrawal(tx_id)
        except Exception as exc:
            log.error(
                "Withdrawal %s: on-chain tx %s succeeded but .NET approve failed: %s. "
                "Manual reconciliation required.",
                tx_id, on_chain_id, exc,
            )

        await store.record(
            db,
            withdrawal_id=tx_id,
            player_id=w.player_id,
            units=w.units,
            to_wallet_address=w.to_wallet_address,
            network=w.network,
            on_chain_tx_id=on_chain_id,
            status="settled",
        )
        log.info("Withdrawal %s settled. on_chain_id=%s", tx_id, on_chain_id)


async def _reject(backend: BackendClient, db, w, *, error: str, status: str) -> None:
    try:
        await backend.reject_withdrawal(w.transaction_id)
    except Exception as exc:
        log.error("Failed to reject withdrawal %s in .NET: %s", w.transaction_id, exc)

    await store.record(
        db,
        withdrawal_id=w.transaction_id,
        player_id=w.player_id,
        units=w.units,
        to_wallet_address=w.to_wallet_address,
        network=w.network,
        on_chain_tx_id=None,
        status=status,
        error=error,
    )
