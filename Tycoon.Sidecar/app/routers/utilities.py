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
import uuid
from datetime import datetime, timezone
from typing import Any

from fastapi import APIRouter, Request, UploadFile, File

router = APIRouter()
logger = logging.getLogger(__name__)
_last_dry_run_report: dict | None = None


def _validate_rebalance_delta(current: dict[str, Any], proposed: dict[str, Any]) -> tuple[bool, list[str]]:
    """
    Guardrails:
    - maxEnergy delta must be <= 2 per apply
    - per-mode energyCost delta must be <= 1 per apply
    """
    errors: list[str] = []

    current_max = current.get("maxEnergy")
    proposed_max = proposed.get("maxEnergy", current_max)
    if isinstance(current_max, int) and isinstance(proposed_max, int):
        if abs(proposed_max - current_max) > 2:
            errors.append("maxEnergy delta exceeds guardrail (max ±2 per apply)")

    current_modes = {m.get("mode"): m for m in current.get("modes", []) if isinstance(m, dict)}
    for mode in proposed.get("modes", []) if isinstance(proposed.get("modes"), list) else []:
        if not isinstance(mode, dict):
            continue
        mode_name = mode.get("mode")
        if mode_name not in current_modes:
            continue
        old_cost = current_modes[mode_name].get("energyCost")
        new_cost = mode.get("energyCost", old_cost)
        if isinstance(old_cost, int) and isinstance(new_cost, int):
            if abs(new_cost - old_cost) > 1:
                errors.append(f"{mode_name}: energyCost delta exceeds guardrail (max ±1 per apply)")

    return (len(errors) == 0), errors


def _extract_delta_summary(current: dict[str, Any], proposed: dict[str, Any]) -> dict[str, Any]:
    summary: dict[str, Any] = {}

    current_max = current.get("maxEnergy")
    proposed_max = proposed.get("maxEnergy")
    if isinstance(current_max, int) and isinstance(proposed_max, int) and current_max != proposed_max:
        summary["maxEnergy"] = {"from": current_max, "to": proposed_max, "delta": proposed_max - current_max}

    current_regen = current.get("regenMinutesPerEnergy")
    proposed_regen = proposed.get("regenMinutesPerEnergy")
    if isinstance(current_regen, int) and isinstance(proposed_regen, int) and current_regen != proposed_regen:
        summary["regenMinutesPerEnergy"] = {"from": current_regen, "to": proposed_regen, "delta": proposed_regen - current_regen}

    mode_changes = []
    current_modes = {
        m.get("mode"): m
        for m in current.get("modes", [])
        if isinstance(m, dict) and isinstance(m.get("mode"), str)
    }
    for mode in proposed.get("modes", []) if isinstance(proposed.get("modes"), list) else []:
        if not isinstance(mode, dict):
            continue
        name = mode.get("mode")
        if name not in current_modes:
            continue
        old_cost = current_modes[name].get("energyCost")
        new_cost = mode.get("energyCost")
        if isinstance(old_cost, int) and isinstance(new_cost, int) and old_cost != new_cost:
            mode_changes.append({"mode": name, "energyCost": {"from": old_cost, "to": new_cost, "delta": new_cost - old_cost}})
    if mode_changes:
        summary["modes"] = mode_changes

    return summary


async def _write_rebalance_audit(app, record: dict[str, Any]) -> str:
    audit_id = str(uuid.uuid4())
    doc = {
        "auditId": audit_id,
        "createdAtUtc": datetime.now(timezone.utc).isoformat(),
        **record,
    }
    await app.state.mongo_db.economy_rebalance_audit.insert_one(doc)
    return audit_id


async def _generate_dry_run_report(app) -> dict:
    backend = app.state.backend
    current = await backend.get("/admin/economy/balance")
    if current.status_code >= 300:
        return {"status": "error", "detail": "Unable to fetch current balance config", "backend_status": current.status_code}

    baseline = current.json()
    recommendation = {
        "maxEnergy": baseline.get("maxEnergy", 20),
        "regenMinutesPerEnergy": baseline.get("regenMinutesPerEnergy", 10),
        "dailyFreeEnergy": max(5, baseline.get("dailyFreeEnergy", 5)),
    }
    return {
        "generatedAtUtc": datetime.now(timezone.utc).isoformat(),
        "status": "dry_run",
        "baseline": baseline,
        "recommendation": recommendation,
    }


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
    db = request.app.state.mongo_db
    snapshot_doc = {
        "seasonId": season_id,
        "snapshotAtUtc": datetime.now(timezone.utc).isoformat(),
        "leaderboard": data.get("items", []),
        "total": len(data.get("items", [])),
    }
    await db.season_leaderboard_snapshots.insert_one(snapshot_doc)
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
    try:
        body = await request.json()
    except Exception:
        return {"status": "blocked", "detail": "Invalid JSON body."}

    if not isinstance(body, dict):
        return {"status": "blocked", "detail": "Body must be a JSON object."}
    if not body.get("approved"):
        return {"status": "blocked", "detail": "Approval required. Set approved=true to apply."}

    payload = body.get("payload", {})
    if not isinstance(payload, dict) or not payload:
        return {"status": "blocked", "detail": "payload is required and must be a non-empty object."}

    approved_by = body.get("approvedBy")
    reason = body.get("reason")
    if not isinstance(approved_by, str) or not approved_by.strip():
        return {"status": "blocked", "detail": "approvedBy is required for auditability."}
    if not isinstance(reason, str) or not reason.strip():
        return {"status": "blocked", "detail": "reason is required for auditability."}

    baseline_resp = await request.app.state.backend.get("/admin/economy/balance")
    if baseline_resp.status_code >= 300:
        return {"status": "error", "detail": "Unable to fetch current balance config", "backend_status": baseline_resp.status_code}
    baseline = baseline_resp.json()
    delta_summary = _extract_delta_summary(baseline, payload)

    ok, errors = _validate_rebalance_delta(baseline, payload if isinstance(payload, dict) else {})
    if not ok:
        audit_id = await _write_rebalance_audit(request.app, {
            "status": "blocked",
            "approvedBy": approved_by.strip(),
            "reason": reason.strip(),
            "violations": errors,
            "baseline": baseline,
            "payload": payload,
            "deltaSummary": delta_summary,
        })
        return {"status": "blocked", "detail": "Guardrail violation", "violations": errors, "auditId": audit_id}

    backend = request.app.state.backend
    resp = await backend.patch("/admin/economy/balance", json=payload)
    status = "ok" if resp.status_code < 300 else "error"
    result = resp.json() if resp.content else None
    audit_id = await _write_rebalance_audit(request.app, {
        "status": status,
        "approvedBy": approved_by.strip(),
        "reason": reason.strip(),
        "baseline": baseline,
        "payload": payload,
        "deltaSummary": delta_summary,
        "backendStatus": resp.status_code,
        "backendResult": result,
    })
    return {"status": status, "backend_status": resp.status_code, "result": result, "auditId": audit_id}


@router.post("/economy/jobs/run-dry-run")
async def run_dry_run_job(request: Request):
    """
    Manual trigger for Phase 3 dry-run job.
    Computes recommendation but does not apply configuration.
    """
    global _last_dry_run_report
    _last_dry_run_report = await _generate_dry_run_report(request.app)
    return _last_dry_run_report


@router.get("/economy/jobs/last-report")
async def get_last_dry_run_report():
    if _last_dry_run_report is None:
        return {"status": "empty", "detail": "No dry-run report has been generated yet."}
    return _last_dry_run_report


@router.get("/economy/rebalance/audit")
async def get_rebalance_audit_history(request: Request, limit: int = 25):
    size = max(1, min(limit, 200))
    cursor = request.app.state.mongo_db.economy_rebalance_audit.find({}, {"_id": 0}).sort("createdAtUtc", -1).limit(size)
    items = await cursor.to_list(length=size)
    return {"status": "ok", "items": items, "count": len(items)}


async def run_scheduled_dry_run(app) -> dict:
    """
    Used by sidecar background scheduler in main lifespan.
    """
    global _last_dry_run_report
    _last_dry_run_report = await _generate_dry_run_report(app)
    return _last_dry_run_report
