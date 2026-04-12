"""Per-network address validation utilities."""
from __future__ import annotations

import re

import base58


def validate_solana_address(address: str) -> bool:
    """Solana public keys are base58-encoded 32-byte values (32–44 characters)."""
    try:
        decoded = base58.b58decode(address)
        return len(decoded) == 32
    except Exception:
        return False


def validate_xrp_address(address: str) -> bool:
    """Classic XRP addresses start with 'r' and are 25–34 base58 characters."""
    if not address.startswith("r"):
        return False
    try:
        decoded = base58.b58decode(address)
        return 20 <= len(decoded) <= 30
    except Exception:
        return False


def validate_evm_address(address: str) -> bool:
    """Ethereum-style hex address: 0x followed by 40 hex chars."""
    return bool(re.fullmatch(r"0x[0-9a-fA-F]{40}", address))


def validate_address(address: str, network: str) -> bool:
    network = network.lower()
    if network in ("solana", "snx"):
        return validate_solana_address(address)
    if network == "xrp":
        return validate_xrp_address(address)
    if network in ("shib", "ethereum", "evm"):
        return validate_evm_address(address)
    return False
