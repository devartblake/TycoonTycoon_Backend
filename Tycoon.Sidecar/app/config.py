from pydantic_settings import BaseSettings, SettingsConfigDict


class Settings(BaseSettings):
    model_config = SettingsConfigDict(env_file=".env", extra="ignore")

    # Connectivity to tycoon-api (injected by Aspire service discovery or Docker)
    backend_base_url: str = "http://localhost:5000"

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


settings = Settings()
