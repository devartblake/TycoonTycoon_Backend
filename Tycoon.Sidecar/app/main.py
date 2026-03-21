import contextlib
import logging

import httpx
from fastapi import FastAPI

from app.config import settings
from app.grpc_client import GrpcClientManager
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
    # Shared async HTTP client for REST calls to tycoon-api
    app.state.backend = httpx.AsyncClient(
        base_url=settings.backend_base_url,
        timeout=10.0,
    )
    # Shared gRPC channel for high-throughput internal calls (port 5001)
    app.state.grpc = GrpcClientManager()
    await app.state.grpc.connect()
    logger.info("Sidecar started. Backend: %s  gRPC: %s", settings.backend_base_url, settings.backend_grpc_url)
    yield
    await app.state.backend.aclose()
    await app.state.grpc.close()


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
