from fastapi import APIRouter
from fastapi.responses import JSONResponse

router = APIRouter(tags=["Health"])


@router.get("/health")
async def health() -> JSONResponse:
    return JSONResponse({"status": "ok"})
