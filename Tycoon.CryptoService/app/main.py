"""
Synaptix Crypto Settlement Service
===================================
FastAPI application that provides multi-chain cryptocurrency settlement for the
Synaptix platform.

Supported networks (Phase 1): SOL (Solana), XRP (XRP Ledger)
Supported networks (Phase 2): SNX (Synaptix SPL token), SHIB (Ethereum ERC-20)

The service runs a background APScheduler job that polls the .NET backend for
pending withdrawal requests and submits the corresponding on-chain transactions.
"""
from __future__ import annotations

import logging
from contextlib import asynccontextmanager

import httpx
from apscheduler.schedulers.asyncio import AsyncIOScheduler
from fastapi import FastAPI
from motor.motor_asyncio import AsyncIOMotorClient

from app.backend_client import BackendClient
from app.blockchain.evm_client import EvmErc20Client
from app.blockchain.solana_client import SnxSplClient, SolanaClient
from app.blockchain.xrpl_client import XrplClient
from app.config import settings
from app.routers import crypto, health
from app.settlement import store
from app.settlement.worker import run_settlement_cycle

logging.basicConfig(
    level=getattr(logging, settings.log_level.upper(), logging.INFO),
    format="%(asctime)s [%(levelname)s] %(name)s — %(message)s",
)
log = logging.getLogger(__name__)


@asynccontextmanager
async def lifespan(app: FastAPI):
    # ── MongoDB ───────────────────────────────────────────────────────────────
    mongo = AsyncIOMotorClient(settings.mongo_url)
    app.state.mongo_db = mongo[settings.mongo_db]
    await store.ensure_indexes(app.state.mongo_db)

    # ── .NET backend HTTP client ──────────────────────────────────────────────
    http = httpx.AsyncClient(timeout=15.0)
    app.state.backend_client = BackendClient(http)

    # ── Blockchain clients ────────────────────────────────────────────────────
    # SOL — always on (as long as keypair file exists)
    try:
        solana = SolanaClient(settings.solana_rpc_url, settings.solana_treasury_keypair_path)
        await solana.start()
        app.state.solana_client = solana
        log.info("Solana client initialised (%s)", settings.solana_rpc_url)
    except Exception as exc:
        app.state.solana_client = None
        log.warning("Solana client not available: %s", exc)

    # XRP — requires a non-empty seed
    if settings.xrpl_treasury_seed:
        try:
            app.state.xrpl_client = XrplClient(settings.xrpl_node_url, settings.xrpl_treasury_seed)
            log.info("XRPL client initialised (%s)", settings.xrpl_node_url)
        except Exception as exc:
            app.state.xrpl_client = None
            log.warning("XRPL client not available: %s", exc)
    else:
        app.state.xrpl_client = None
        log.info("XRPL not configured (XRPL_TREASURY_SEED not set)")

    # SNX — requires both keypair + mint address
    if settings.snx_mint_address and app.state.solana_client:
        try:
            snx = SnxSplClient(
                settings.solana_rpc_url,
                settings.solana_treasury_keypair_path,
                settings.snx_mint_address,
            )
            await snx.start()
            app.state.snx_client = snx
            log.info("SNX SPL client initialised (mint=%s)", settings.snx_mint_address)
        except Exception as exc:
            app.state.snx_client = None
            log.warning("SNX client not available: %s", exc)
    else:
        app.state.snx_client = None
        log.info("SNX not configured (SNX_MINT_ADDRESS not set)")

    # SHIB (Phase 2) — requires Ethereum RPC URL
    if settings.ethereum_rpc_url:
        try:
            app.state.evm_client = EvmErc20Client(
                rpc_url=settings.ethereum_rpc_url,
                treasury_key_path=settings.ethereum_treasury_key_path,
                contract_address=settings.shib_contract_address,
                asset_name="SHIB",
            )
            log.info("EVM/SHIB client initialised (%s)", settings.ethereum_rpc_url)
        except Exception as exc:
            app.state.evm_client = None
            log.warning("EVM client not available: %s", exc)
    else:
        app.state.evm_client = None
        log.info("EVM/SHIB not configured (ETHEREUM_RPC_URL not set)")

    # ── Background settlement worker ──────────────────────────────────────────
    scheduler = AsyncIOScheduler()
    scheduler.add_job(
        run_settlement_cycle,
        "interval",
        seconds=settings.settlement_poll_interval_seconds,
        args=[app.state],
        id="settlement_worker",
        max_instances=1,        # prevent overlapping runs
        coalesce=True,
    )
    scheduler.start()
    log.info(
        "Settlement worker started (interval=%ds)", settings.settlement_poll_interval_seconds
    )

    yield  # ── app is running ─────────────────────────────────────────────────

    # ── Shutdown ──────────────────────────────────────────────────────────────
    scheduler.shutdown(wait=False)
    if app.state.solana_client:
        await app.state.solana_client.stop()
    if app.state.snx_client:
        await app.state.snx_client.stop()
    await http.aclose()
    mongo.close()
    log.info("Crypto service shut down cleanly.")


app = FastAPI(
    title="Synaptix Crypto Service",
    description=(
        "Multi-chain cryptocurrency settlement service for Synaptix. "
        "Handles SOL, XRP, SHIB, and the native SNX token. "
        "Polls the .NET backend for pending withdrawals and submits on-chain transactions."
    ),
    version="1.0.0",
    lifespan=lifespan,
)

app.include_router(health.router)
app.include_router(crypto.router)
