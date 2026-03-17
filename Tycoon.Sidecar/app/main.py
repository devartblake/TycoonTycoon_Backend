import contextlib
import logging

import httpx
from fastapi import FastAPI

from app.config import settings
from app.routers import analytics, ml, utilities, webhooks

logging.basicConfig(level=settings.log_level.upper())
logger = logging.getLogger(__name__)


@contextlib.asynccontextmanager
async def lifespan(app: FastAPI):
    # Shared async HTTP client for calls to tycoon-api
    app.state.backend = httpx.AsyncClient(
        base_url=settings.backend_base_url,
        timeout=10.0,
    )
    logger.info("Sidecar started. Backend: %s", settings.backend_base_url)
    yield
    await app.state.backend.aclose()


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
