#!/usr/bin/env python3
"""
One-time SNX (Synaptix) SPL token creation script.

Run this ONCE on the desired Solana network to create the SNX fungible token.
The output mint address must be set as SNX_MINT_ADDRESS in the crypto service
environment before SNX withdrawals can be processed.

Usage:
    python scripts/create_snx_token.py \\
        --keypair /path/to/treasury.json \\
        --rpc https://api.devnet.solana.com \\
        --decimals 6 \\
        --initial-supply 1000000000

Output: mint address printed to stdout (store in SNX_MINT_ADDRESS env var).

Prerequisites:
    pip install solders solana base58
"""
from __future__ import annotations

import argparse
import json
import sys
from pathlib import Path


def main() -> None:
    parser = argparse.ArgumentParser(description="Create the SNX Synaptix SPL token")
    parser.add_argument("--keypair", required=True, help="Path to treasury keypair JSON")
    parser.add_argument("--rpc", default="https://api.devnet.solana.com")
    parser.add_argument("--decimals", type=int, default=6)
    parser.add_argument(
        "--initial-supply",
        type=int,
        default=1_000_000_000,
        help="Total SNX tokens to mint into the treasury ATA (default: 1 billion)",
    )
    args = parser.parse_args()

    try:
        from solana.rpc.api import Client
        from solders.keypair import Keypair
        from solders.pubkey import Pubkey
        from spl.token.client import Token
        from spl.token.constants import TOKEN_PROGRAM_ID
    except ImportError as exc:
        print(f"ERROR: missing dependencies — {exc}", file=sys.stderr)
        print("Run: pip install solders solana base58", file=sys.stderr)
        sys.exit(1)

    # Load treasury keypair
    keypair_data = json.loads(Path(args.keypair).read_bytes())
    payer = Keypair.from_bytes(bytes(keypair_data))
    print(f"Treasury public key : {payer.pubkey()}")

    client = Client(args.rpc)

    # Create the SPL token mint
    print(f"Creating SNX mint on {args.rpc} (decimals={args.decimals}) ...")
    token = Token.create_mint(
        conn=client,
        payer=payer,
        mint_authority=payer.pubkey(),
        decimals=args.decimals,
        program_id=TOKEN_PROGRAM_ID,
    )
    mint_address = str(token.pubkey)
    print(f"SNX mint address    : {mint_address}")

    # Create the treasury associated token account and mint initial supply
    print(f"Creating treasury ATA and minting {args.initial_supply:,} SNX ...")
    treasury_ata = token.create_associated_token_account(payer.pubkey())
    raw_amount = args.initial_supply * (10 ** args.decimals)
    token.mint_to(
        dest=treasury_ata,
        mint_authority=payer,
        amount=raw_amount,
    )

    print()
    print("=" * 60)
    print("SNX token created successfully.")
    print(f"  Mint address    : {mint_address}")
    print(f"  Treasury ATA    : {treasury_ata}")
    print(f"  Total supply    : {args.initial_supply:,} SNX")
    print(f"  Decimals        : {args.decimals}")
    print()
    print("Next step — set in your environment or .env file:")
    print(f"  SNX_MINT_ADDRESS={mint_address}")
    print("=" * 60)


if __name__ == "__main__":
    main()
