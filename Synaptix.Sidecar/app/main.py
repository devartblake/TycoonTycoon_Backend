import contextlib
import logging
import asyncio

import httpx
from fastapi import FastAPI
from motor.motor_asyncio import AsyncIOMotorClient
from elasticsearch import AsyncElasticsearch
from pymongo.errors import OperationFailure

from app.config import settings
from app.grpc_client import GrpcClientManager
from app.routers import analytics, ml, utilities, webhooks, personalization as personalization_router
from app.routes import auth as auth_routes, config as config_routes

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
    # Shared async HTTP client for REST calls to synaptix-api
    app.state.backend = httpx.AsyncClient(
        base_url=settings.backend_base_url,
        timeout=10.0,
    )
    app.state.mongo_client = AsyncIOMotorClient(settings.mongo_url)
    app.state.mongo_db = app.state.mongo_client[settings.mongo_db]
    es_compat_version = max(0, int(settings.elasticsearch_compatibility_version))
    es_headers = None
    if es_compat_version > 0:
        media_type = f"application/vnd.elasticsearch+json; compatible-with={es_compat_version}"
        es_headers = {
            "accept": media_type,
            "content-type": media_type,
        }
    app.state.elasticsearch = AsyncElasticsearch(
        settings.elasticsearch_url,
        basic_auth=(settings.elasticsearch_user, settings.elasticsearch_password),
        verify_certs=False,
        headers=es_headers,
    )

    # Ensure analytics_events collection has a unique index on event_id for idempotency
    try:
        await app.state.mongo_db["analytics_events"].create_index(
            [("event_id", 1)], unique=True, background=True
        )
    except OperationFailure as exc:
        logger.warning("Could not create analytics_events index (may already exist): %s", exc)

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
    # Shared gRPC channel for high-throughput internal calls (port 5001)
    app.state.grpc = GrpcClientManager()
    await app.state.grpc.connect()
    logger.info("Synaptix Sidecar started. Backend: %s  gRPC: %s", settings.backend_base_url, settings.backend_grpc_url)
    yield
    stop_event.set()
    await scheduler_task
    await app.state.backend.aclose()
    app.state.mongo_client.close()
    await app.state.elasticsearch.close()
    await app.state.grpc.close()


app = FastAPI(
    title="Synaptix Sidecar",
    version="1.0.0",
    description="AI/ML inference, analytics, webhooks, and game balance utilities for Synaptix.",
    lifespan=lifespan,
)

app.include_router(ml.router,                          prefix="/ml",             tags=["ML"])
app.include_router(analytics.router,                   prefix="/analytics",      tags=["Analytics"])
app.include_router(webhooks.router,                    prefix="/webhooks",       tags=["Webhooks"])
app.include_router(utilities.router,                   prefix="/utilities",      tags=["Utilities"])
app.include_router(auth_routes.router,                 prefix="/auth",           tags=["Auth"])
app.include_router(config_routes.router,               prefix="/config",         tags=["Config"])
app.include_router(personalization_router.router,      tags=["Personalization"])


@app.get("/health", tags=["Health"])
def health():
    return {"status": "ok"}
