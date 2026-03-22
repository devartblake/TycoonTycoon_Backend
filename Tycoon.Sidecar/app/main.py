import contextlib
import logging
import asyncio

import httpx
from fastapi import FastAPI
from motor.motor_asyncio import AsyncIOMotorClient
from elasticsearch import AsyncElasticsearch

from app.config import settings
from app.routers import analytics, ml, utilities, webhooks

# Normalize .NET-style log level names (Information, Warning, Critical) to Python equivalents
_DOTNET_TO_PYTHON_LEVEL: dict[str, str] = {
    "verbose": "DEBUG",
    "information": "INFO",
    "warning": "WARNING",
    "critical": "CRITICAL",
    "fatal": "CRITICAL",
}
_raw_level = settings.log_level.lower()
_py_level = _DOTNET_TO_PYTHON_LEVEL.get(_raw_level, _raw_level.upper())
logging.basicConfig(level=_py_level)
logger = logging.getLogger(__name__)


@contextlib.asynccontextmanager
async def lifespan(app: FastAPI):
    # Shared async HTTP client for calls to tycoon-api
    app.state.backend = httpx.AsyncClient(
        base_url=settings.backend_base_url,
        timeout=10.0,
    )
    app.state.mongo_client = AsyncIOMotorClient(settings.mongo_url)
    app.state.mongo_db = app.state.mongo_client[settings.mongo_db]
    app.state.elasticsearch = AsyncElasticsearch(
        settings.elasticsearch_url,
        basic_auth=(settings.elasticsearch_user, settings.elasticsearch_password),
        verify_certs=False,
    )

    stop_event = asyncio.Event()

    async def dry_run_scheduler():
        while not stop_event.is_set():
            try:
                await utilities.run_scheduled_dry_run(app)
            except Exception:  # best-effort background job; keep service healthy
                logger.exception("Scheduled dry-run job failed")

            try:
                await asyncio.wait_for(stop_event.wait(), timeout=max(30, settings.dry_run_job_interval_seconds))
            except asyncio.TimeoutError:
                pass

    scheduler_task = asyncio.create_task(dry_run_scheduler())
    logger.info("Sidecar started. Backend: %s", settings.backend_base_url)
    yield
    stop_event.set()
    await scheduler_task
    await app.state.backend.aclose()
    app.state.mongo_client.close()
    await app.state.elasticsearch.close()


app = FastAPI(
    title="Tycoon Sidecar",
    version="1.0.0",
    description="AI/ML inference, analytics pipelines, webhooks, and utilities for Tycoon Backend.",
    lifespan=lifespan,
)

app.include_router(ml.router,        prefix="/ml",        tags=["ML"])
app.include_router(analytics.router, prefix="/analytics", tags=["Analytics"])
app.include_router(webhooks.router,  prefix="/webhooks",  tags=["Webhooks"])
app.include_router(utilities.router, prefix="/utilities", tags=["Utilities"])


@app.get("/health", tags=["Health"])
def health():
    return {"status": "ok"}
