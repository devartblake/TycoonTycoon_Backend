"""Abstract base class for all blockchain network clients."""
from __future__ import annotations

from abc import ABC, abstractmethod


class BlockchainClient(ABC):
    """Common interface every network adapter must implement."""

    @abstractmethod
    async def get_balance(self, address: str) -> float:
        """Return the spendable balance for *address* in the network's base unit."""
        ...

    @abstractmethod
    async def transfer(self, to_address: str, amount: float) -> str:
        """
        Sign and broadcast a transfer of *amount* to *to_address*.
        Returns the network-specific transaction identifier
        (Solana signature, XRPL hash, EVM tx hash, etc.).
        Raises on failure.
        """
        ...

    @abstractmethod
    def validate_address(self, address: str) -> bool:
        """Return True if *address* is a syntactically valid address for this network."""
        ...

    @property
    @abstractmethod
    def network_name(self) -> str:
        """Human-readable network identifier, e.g. 'solana', 'xrp', 'shib', 'snx'."""
        ...
