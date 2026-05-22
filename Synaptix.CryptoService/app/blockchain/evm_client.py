"""
Ethereum / EVM client — Phase 2.

Handles ERC-20 token transfers (initially SHIB on Ethereum mainnet).
Uncomment web3 + eth-account in requirements.txt before using.

This file is intentionally left as a stub so the module structure is in
place for Phase 2 without adding heavyweight EVM dependencies to Phase 1.
"""
from __future__ import annotations

import logging

from app.blockchain.base import BlockchainClient
from app.blockchain.address_utils import validate_evm_address

log = logging.getLogger(__name__)

# Standard ERC-20 ABI for balanceOf and transfer
ERC20_ABI = [
    {
        "name": "balanceOf",
        "type": "function",
        "inputs": [{"name": "account", "type": "address"}],
        "outputs": [{"name": "", "type": "uint256"}],
        "stateMutability": "view",
    },
    {
        "name": "transfer",
        "type": "function",
        "inputs": [
            {"name": "recipient", "type": "address"},
            {"name": "amount", "type": "uint256"},
        ],
        "outputs": [{"name": "", "type": "bool"}],
        "stateMutability": "nonpayable",
    },
    {
        "name": "decimals",
        "type": "function",
        "inputs": [],
        "outputs": [{"name": "", "type": "uint8"}],
        "stateMutability": "view",
    },
]

SHIB_DECIMALS = 18  # SHIB uses 18 decimals


class EvmErc20Client(BlockchainClient):
    """
    Generic ERC-20 token client.
    Pass the contract address for the token you want to transfer (e.g. SHIB).

    Requires: web3>=7.0, eth-account>=0.10 (add to requirements.txt for Phase 2).
    """

    def __init__(
        self,
        rpc_url: str,
        treasury_key_path: str,
        contract_address: str,
        decimals: int = SHIB_DECIMALS,
        asset_name: str = "SHIB",
    ) -> None:
        self._rpc_url = rpc_url
        self._treasury_key_path = treasury_key_path
        self._contract_address = contract_address
        self._decimals = decimals
        self._asset_name = asset_name

        # Lazy imports — only available when web3 is installed
        self._w3 = None
        self._account = None
        self._contract = None

    def _ensure_web3(self):
        if self._w3 is not None:
            return
        try:
            from web3 import Web3
            from eth_account import Account
            from pathlib import Path

            private_key = Path(self._treasury_key_path).read_text().strip()
            self._account = Account.from_key(private_key)
            self._w3 = Web3(Web3.HTTPProvider(self._rpc_url))
            self._contract = self._w3.eth.contract(
                address=Web3.to_checksum_address(self._contract_address),
                abi=ERC20_ABI,
            )
        except ImportError as exc:
            raise RuntimeError(
                "web3 and eth-account must be installed for EVM support. "
                "Uncomment them in requirements.txt."
            ) from exc

    @property
    def network_name(self) -> str:
        return self._asset_name.lower()

    def validate_address(self, address: str) -> bool:
        return validate_evm_address(address)

    async def get_balance(self, address: str) -> float:
        self._ensure_web3()
        raw = self._contract.functions.balanceOf(
            self._w3.to_checksum_address(address)
        ).call()
        return raw / (10 ** self._decimals)

    async def transfer(self, to_address: str, amount: float) -> str:
        self._ensure_web3()
        raw_amount = int(amount * (10 ** self._decimals))
        checksummed = self._w3.to_checksum_address(to_address)
        nonce = self._w3.eth.get_transaction_count(self._account.address)
        gas_price = self._w3.eth.gas_price
        tx = self._contract.functions.transfer(checksummed, raw_amount).build_transaction({
            "from": self._account.address,
            "nonce": nonce,
            "gasPrice": gas_price,
        })
        signed = self._w3.eth.account.sign_transaction(tx, self._account.key)
        tx_hash = self._w3.eth.send_raw_transaction(signed.rawTransaction)
        receipt = self._w3.eth.wait_for_transaction_receipt(tx_hash, timeout=120)
        if receipt["status"] != 1:
            raise RuntimeError(f"{self._asset_name} transfer reverted: {tx_hash.hex()}")
        result = tx_hash.hex()
        log.info("%s transfer sent: → %s (%s) hash=%s",
                 self._asset_name, to_address, amount, result)
        return result
