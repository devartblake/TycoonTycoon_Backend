"""
Routes a withdrawal to the correct blockchain client by network name.
"""
from __future__ import annotations

from app.blockchain.base import BlockchainClient


def get_client(network: str, app_state) -> BlockchainClient:
    """
    Return the blockchain client for *network*.

    app_state attributes expected:
      - solana_client  (SolanaClient)
      - xrpl_client    (XrplClient)
      - snx_client     (SnxSplClient | None)  — None until SNX_MINT_ADDRESS is set
      - evm_client     (EvmErc20Client | None) — None until Phase 2

    Raises ValueError for unknown or unconfigured networks.
    """
    network = network.lower()

    mapping: dict[str, BlockchainClient | None] = {
        "solana": getattr(app_state, "solana_client", None),
        "xrp":    getattr(app_state, "xrpl_client", None),
        "snx":    getattr(app_state, "snx_client", None),
        "shib":   getattr(app_state, "evm_client", None),
    }

    if network not in mapping:
        raise ValueError(f"Unknown network: {network!r}")

    client = mapping[network]
    if client is None:
        raise ValueError(
            f"Network {network!r} is recognised but not configured "
            f"(check the relevant env vars, e.g. SNX_MINT_ADDRESS for 'snx')."
        )
    return client
