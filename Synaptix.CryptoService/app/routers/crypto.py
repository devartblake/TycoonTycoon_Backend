"""
Crypto service REST API.

Endpoints:
  GET  /crypto/networks                          — list configured networks + status
  GET  /crypto/onchain/balance                   — real on-chain balance for an address
  POST /crypto/validate-address                  — validate address format before wallet-link
  GET  /crypto/settlement/pending                — withdrawals queued in MongoDB
  GET  /crypto/settlement/history                — past settlements with on-chain tx ids
  POST /crypto/settlement/trigger/{withdrawalId} — manually trigger one settlement
"""
from __future__ import annotations

import logging

from fastapi import APIRouter, HTTPException, Request
from pydantic import BaseModel

from app.blockchain.address_utils import validate_address
from app.settlement import store
from app.settlement.router import get_client
from app.settlement.worker import run_settlement_cycle

log = logging.getLogger(__name__)
router = APIRouter(prefix="/crypto", tags=["Crypto"])


# ── Schema models ─────────────────────────────────────────────────────────────

class ValidateAddressRequest(BaseModel):
    address: str
    network: str


class ValidateAddressResponse(BaseModel):
    address: str
    network: str
    valid: bool


class NetworkStatus(BaseModel):
    network: str
    configured: bool
    description: str


# ── Endpoints ─────────────────────────────────────────────────────────────────

@router.get("/networks")
async def list_networks(request: Request) -> list[NetworkStatus]:
    """Return each supported network and whether it is fully configured."""
    state = request.app.state
    networks = [
        NetworkStatus(
            network="solana",
            configured=getattr(state, "solana_client", None) is not None,
            description="Native SOL transfers on Solana",
        ),
        NetworkStatus(
            network="xrp",
            configured=getattr(state, "xrpl_client", None) is not None,
            description="Native XRP payments on the XRP Ledger",
        ),
        NetworkStatus(
            network="snx",
            configured=getattr(state, "snx_client", None) is not None,
            description="Synaptix SPL token on Solana",
        ),
        NetworkStatus(
            network="shib",
            configured=getattr(state, "evm_client", None) is not None,
            description="SHIB ERC-20 token on Ethereum (Phase 2)",
        ),
    ]
    return networks


@router.get("/onchain/balance")
async def onchain_balance(address: str, network: str, request: Request) -> dict:
    """Query the real on-chain balance for a given wallet address and network."""
    try:
        client = get_client(network, request.app.state)
    except ValueError as exc:
        raise HTTPException(status_code=400, detail=str(exc))

    if not client.validate_address(address):
        raise HTTPException(
            status_code=400, detail=f"Invalid {network} address: {address!r}"
        )

    try:
        balance = await client.get_balance(address)
    except Exception as exc:
        log.exception("get_balance failed for %s/%s", network, address)
        raise HTTPException(status_code=502, detail=f"RPC error: {exc}") from exc

    return {"address": address, "network": network, "balance": balance}


@router.post("/validate-address", response_model=ValidateAddressResponse)
async def validate_wallet_address(body: ValidateAddressRequest) -> ValidateAddressResponse:
    """Validate address format before calling .NET /crypto/link-wallet."""
    is_valid = validate_address(body.address, body.network)
    return ValidateAddressResponse(
        address=body.address, network=body.network, valid=is_valid
    )


@router.get("/settlement/pending")
async def settlement_pending(request: Request) -> dict:
    """Withdrawals currently queued in the settlement MongoDB log."""
    items = await store.list_pending(request.app.state.mongo_db)
    return {"total": len(items), "items": items}


@router.get("/settlement/history")
async def settlement_history(
    request: Request, page: int = 1, page_size: int = 20
) -> dict:
    """Paginated list of past settlements with on-chain transaction identifiers."""
    page_size = min(max(page_size, 1), 100)
    return await store.list_history(request.app.state.mongo_db, page=page, page_size=page_size)


@router.post("/settlement/trigger/{withdrawal_id}")
async def trigger_settlement(withdrawal_id: str, request: Request) -> dict:
    """
    Manually trigger the settlement cycle for one specific withdrawal.
    Useful for operator retries without waiting for the next poll interval.
    """
    backend = request.app.state.backend_client
    try:
        page = await backend.get_pending_withdrawals(page=1, page_size=200)
    except Exception as exc:
        raise HTTPException(status_code=502, detail=f"Cannot reach backend: {exc}") from exc

    target = next((w for w in page.items if w.transaction_id == withdrawal_id), None)
    if target is None:
        raise HTTPException(
            status_code=404,
            detail=f"Withdrawal {withdrawal_id!r} not found in pending list.",
        )

    # Run settlement synchronously for this one item
    await run_settlement_cycle(request.app.state)
    return {"triggered": True, "withdrawal_id": withdrawal_id}
