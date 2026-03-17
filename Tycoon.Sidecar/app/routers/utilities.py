"""
Admin automation & utility endpoints.

Routes:
  POST /utilities/season/snapshot        — snapshot season standings to MongoDB
  POST /utilities/questions/import       — bulk-import questions from CSV/JSON
  GET  /utilities/health/backend         — probe tycoon-api health
"""

import logging

from fastapi import APIRouter, Request, UploadFile, File

router = APIRouter()
logger = logging.getLogger(__name__)


@router.post("/season/snapshot")
async def snapshot_season(season_id: str, request: Request):
    """
    Reads the current season leaderboard from tycoon-api and writes a
    point-in-time snapshot to the analytics MongoDB collection.
    """
    backend = request.app.state.backend
    resp = await backend.get(f"/admin/seasons/{season_id}/leaderboard?page=1&pageSize=1000")
    if resp.status_code != 200:
        return {"status": "error", "backend_status": resp.status_code}

    data = resp.json()
    # TODO: write data to MongoDB via motor
    logger.info("Season %s snapshot: %d entries", season_id, len(data.get("items", [])))
    return {"status": "ok", "entries": len(data.get("items", []))}


@router.post("/questions/import")
async def import_questions(request: Request, file: UploadFile = File(...)):
    """
    Accepts a CSV or JSON file of questions and bulk-imports them via
    the tycoon-api admin questions endpoint.
    """
    import json
    content = await file.read()
    questions = json.loads(content)
    if not isinstance(questions, list):
        return {"status": "error", "detail": "Expected a JSON array of questions"}

    backend = request.app.state.backend
    results = []
    for q in questions:
        resp = await backend.post("/admin/questions", json=q)
        results.append({"status": resp.status_code})

    ok = sum(1 for r in results if r["status"] < 300)
    logger.info("Imported %d/%d questions", ok, len(questions))
    return {"status": "ok", "imported": ok, "total": len(questions)}


@router.get("/health/backend")
async def probe_backend(request: Request):
    """Proxy-probe tycoon-api /healthz and return its status."""
    try:
        backend = request.app.state.backend
        resp = await backend.get("/healthz", timeout=5.0)
        return {"reachable": True, "status_code": resp.status_code}
    except Exception as exc:
        return {"reachable": False, "error": str(exc)}
