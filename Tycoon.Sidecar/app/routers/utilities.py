"""
Admin automation & utility endpoints.

Routes:
  POST /utilities/season/snapshot        — snapshot season standings to MongoDB
  POST /utilities/questions/import       — bulk-import questions from CSV/JSON
  GET  /utilities/economy/rebalance/recommend — suggest balancing deltas (dry-run)
  POST /utilities/economy/rebalance/apply     — apply approved balancing update
  POST /utilities/economy/jobs/run-dry-run    — execute dry-run rebalance job
  GET  /utilities/economy/jobs/last-report    — view last dry-run report
  GET  /utilities/health/backend         — probe tycoon-api health
"""

import logging
from datetime import datetime, timezone

from fastapi import APIRouter, Request, UploadFile, File

router = APIRouter()
logger = logging.getLogger(__name__)
_last_dry_run_report: dict | None = None


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


@router.get("/economy/rebalance/recommend")
async def recommend_rebalance(request: Request):
    """
    Produces a conservative recommendation payload for Phase 0/1 balancing.
    In this starter implementation, recommendations are deterministic defaults
    intended for operator review.
    """
    backend = request.app.state.backend
    current = await backend.get("/admin/economy/balance")
    if current.status_code >= 300:
        return {"status": "error", "detail": "Unable to fetch current balance config", "backend_status": current.status_code}

    baseline = current.json()
    recommendation = {
        "maxEnergy": baseline.get("maxEnergy", 20),
        "regenMinutesPerEnergy": baseline.get("regenMinutesPerEnergy", 10),
        "dailyFreeEnergy": max(5, baseline.get("dailyFreeEnergy", 5)),
        "modes": [
            {"mode": "casual", "energyCost": 3, "lives": None, "requiresTicket": False, "tierPointsWeight": 0},
            {"mode": "ranked", "energyCost": 4, "lives": None, "requiresTicket": False, "tierPointsWeight": 100},
            {"mode": "jackpot", "energyCost": 0, "lives": 3, "requiresTicket": True, "tierPointsWeight": 25},
            {"mode": "guardian", "energyCost": 5, "lives": 2, "requiresTicket": False, "tierPointsWeight": 150},
        ],
    }
    return {"status": "ok", "dry_run": True, "current": baseline, "recommendation": recommendation}


@router.post("/economy/rebalance/apply")
async def apply_rebalance(request: Request):
    """
    Applies a provided balance patch to backend admin economy endpoint.
    Requires caller payload to include {"approved": true}.
    """
    body = await request.json()
    if not body.get("approved"):
        return {"status": "blocked", "detail": "Approval required. Set approved=true to apply."}

    payload = body.get("payload", {})
    backend = request.app.state.backend
    resp = await backend.patch("/admin/economy/balance", json=payload)
    return {"status": "ok" if resp.status_code < 300 else "error", "backend_status": resp.status_code, "result": resp.json() if resp.content else None}


@router.post("/economy/jobs/run-dry-run")
async def run_dry_run_job(request: Request):
    """
    Manual trigger for Phase 3 dry-run job.
    Computes recommendation but does not apply configuration.
    """
    global _last_dry_run_report
    backend = request.app.state.backend
    current = await backend.get("/admin/economy/balance")
    if current.status_code >= 300:
        return {"status": "error", "detail": "Unable to fetch current balance config", "backend_status": current.status_code}

    baseline = current.json()
    recommendation = {
        "maxEnergy": baseline.get("maxEnergy", 20),
        "regenMinutesPerEnergy": baseline.get("regenMinutesPerEnergy", 10),
        "dailyFreeEnergy": max(5, baseline.get("dailyFreeEnergy", 5)),
    }
    _last_dry_run_report = {
        "generatedAtUtc": datetime.now(timezone.utc).isoformat(),
        "status": "dry_run",
        "baseline": baseline,
        "recommendation": recommendation,
    }
    return _last_dry_run_report


@router.get("/economy/jobs/last-report")
async def get_last_dry_run_report():
    if _last_dry_run_report is None:
        return {"status": "empty", "detail": "No dry-run report has been generated yet."}
    return _last_dry_run_report
