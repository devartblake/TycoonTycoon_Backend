from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_file=".env", extra="ignore")

    # Connectivity to tycoon-api (injected by Aspire service discovery or Docker)
    backend_base_url: str = "http://localhost:5000"
    # gRPC endpoint for the backend-api (HTTP/2, port 5001)
    backend_grpc_url: str = "localhost:5001"

    # MongoDB (analytics DB)
    mongo_url: str = "mongodb://tycoon_app_user:tycoon_app_password_123@localhost:27017/tycoon_db?authSource=tycoon_db"
    mongo_db: str = "tycoon_analytics"

    # Elasticsearch
    elasticsearch_url: str = "http://localhost:9200"
    elasticsearch_user: str = "elastic"
    elasticsearch_password: str = "tycoon_elastic_password_123"

    # Service settings
    port: int = 8100
    log_level: str = "info"
    dry_run_job_interval_seconds: int = 3600
    stripe_webhook_secret: str = ""
    rebalance_alert_min_attempts: int = 5
    rebalance_alert_error_rate_threshold: float = 0.3
    rebalance_alert_blocked_rate_threshold: float = 0.6
    rebalance_metrics_index: str = "tycoon_rebalance_metrics"
    rebalance_alert_webhook_url: str = ""
    rebalance_rollout_max_metrics_age_minutes: int = 120
    rebalance_rollout_max_dry_run_age_minutes: int = 180
    rebalance_rollout_max_delivery_age_minutes: int = 180


settings = Settings()
