from pydantic_settings import BaseSettings


class Settings(BaseSettings):
    # .NET backend
    backend_base_url: str = "http://localhost:5000"
    admin_ops_key: str = ""

    # MongoDB — settlement log
    mongo_url: str = "mongodb://localhost:27017"
    mongo_db: str = "synaptix_crypto"
    crypto_service_jwt: str = ""
    crypto_service_jwt_file: str = ""
    require_crypto_service_jwt: bool = False
    kms_api_base_url: str = ""
    kms_service_token: str = ""

    # ── Solana (SOL native + SNX SPL token) ──────────────────────────────────
    solana_rpc_url: str = "https://api.devnet.solana.com"
    # Path to the JSON keypair file (array of 64 bytes) — NEVER bake into image
    solana_treasury_keypair_path: str = "/run/secrets/solana-treasury"
    # SNX mint address — empty until the SPL token has been created
    snx_mint_address: str = ""

    # ── XRP Ledger ────────────────────────────────────────────────────────────
    xrpl_node_url: str = "wss://s.altnet.rippletest.net:51233"
    # Family seed (s...) — load from secrets manager in production
    xrpl_treasury_seed: str = ""

    # ── Ethereum / EVM  (Phase 2 — SHIB) ────────────────────────────────────
    ethereum_rpc_url: str = ""
    ethereum_treasury_key_path: str = "/run/secrets/eth-treasury"
    shib_contract_address: str = "0x95aD61b0a150d79219dCF64E1E6Cc01f0B64C4cE"

    # ── Settlement worker ─────────────────────────────────────────────────────
    settlement_poll_interval_seconds: int = 30
    settlement_max_retries: int = 3
    settlement_retry_backoff_seconds: int = 10

    # ── Service ───────────────────────────────────────────────────────────────
    port: int = 8300
    log_level: str = "info"

    class Config:
        env_file = ".env"
        extra = "ignore"


settings = Settings()
