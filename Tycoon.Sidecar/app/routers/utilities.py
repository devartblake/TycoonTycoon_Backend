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
from datetime import datetime, timedelta, timezone
from typing import Any

import httpx
from fastapi import APIRouter, Request, UploadFile, File, Response
from app.config import settings

router = APIRouter()
logger = logging.getLogger(__name__)
_last_dry_run_report: dict | None = None
_rebalance_metrics: dict[str, Any] = {
    "totalApplyAttempts": 0,
    "blockedCount": 0,
    "successCount": 0,
    "errorCount": 0,
    "lastAttemptAtUtc": None,
    "lastSuccessAtUtc": None,
    "lastErrorAtUtc": None,
}
_last_alert_delivery: dict[str, Any] = {
    "lastAttemptAtUtc": None,
    "lastStatus": "never",
    "lastDetail": None,
}


async def _publish_rebalance_metrics_snapshot(app) -> dict[str, Any]:
    elastic = getattr(app.state, "elasticsearch", None)
    if elastic is None:
        return {"status": "skipped", "detail": "elasticsearch client unavailable"}

    payload = {
        "capturedAtUtc": datetime.now(timezone.utc).isoformat(),
        "totalApplyAttempts": int(_rebalance_metrics["totalApplyAttempts"]),
        "blockedCount": int(_rebalance_metrics["blockedCount"]),
        "successCount": int(_rebalance_metrics["successCount"]),
        "errorCount": int(_rebalance_metrics["errorCount"]),
        "lastAttemptAtUtc": _rebalance_metrics["lastAttemptAtUtc"],
        "lastSuccessAtUtc": _rebalance_metrics["lastSuccessAtUtc"],
        "lastErrorAtUtc": _rebalance_metrics["lastErrorAtUtc"],
    }

    await elastic.index(index=settings.rebalance_metrics_index, document=payload, refresh=False)
    return {"status": "ok", "index": settings.rebalance_metrics_index, "document": payload}


async def _load_rebalance_metrics_history(app, limit: int) -> list[dict[str, Any]]:
    elastic = getattr(app.state, "elasticsearch", None)
    if elastic is None:
        return []

    size = max(1, min(limit, 500))
    query = {
        "size": size,
        "sort": [{"capturedAtUtc": {"order": "desc"}}],
        "query": {"match_all": {}},
    }
    resp = await elastic.search(index=settings.rebalance_metrics_index, body=query)
    hits = resp.get("hits", {}).get("hits", [])
    return [h.get("_source", {}) for h in hits if isinstance(h, dict)]


def _build_sink_alerts_from_snapshot(latest: dict[str, Any]) -> list[dict[str, Any]]:
    total = int(latest.get("totalApplyAttempts", 0))
    blocked = int(latest.get("blockedCount", 0))
    error = int(latest.get("errorCount", 0))
    error_rate = (error / total) if total > 0 else 0.0
    blocked_rate = (blocked / total) if total > 0 else 0.0

    alerts: list[dict[str, Any]] = []
    if total >= settings.rebalance_alert_min_attempts and error_rate >= settings.rebalance_alert_error_rate_threshold:
        alerts.append({
            "severity": "high",
            "code": "SINK_REBALANCE_ERROR_RATE_HIGH",
            "message": f"Sink snapshot error rate is {error_rate:.1%} ({error}/{total}).",
        })
    if total >= settings.rebalance_alert_min_attempts and blocked_rate >= settings.rebalance_alert_blocked_rate_threshold:
        alerts.append({
            "severity": "medium",
            "code": "SINK_REBALANCE_BLOCKED_RATE_HIGH",
            "message": f"Sink snapshot blocked rate is {blocked_rate:.1%} ({blocked}/{total}).",
        })
    return alerts


async def _dispatch_alert_webhook(url: str, payload: dict[str, Any], dispatcher=None) -> dict[str, Any]:
    if dispatcher is not None:
        return await dispatcher(url, payload)

    async with httpx.AsyncClient(timeout=10.0) as client:
        resp = await client.post(url, json=payload)
        return {"status_code": resp.status_code, "ok": resp.status_code < 300}


async def _build_rollout_readiness(app) -> dict[str, Any]:
    checks: list[dict[str, Any]] = []

    elastic_available = getattr(app.state, "elasticsearch", None) is not None
    checks.append({
        "name": "elastic_client_configured",
        "ok": elastic_available,
        "detail": "Elasticsearch client is available." if elastic_available else "Elasticsearch client is not configured.",
    })

    webhook_configured = bool(settings.rebalance_alert_webhook_url.strip())
    checks.append({
        "name": "alert_webhook_configured",
        "ok": webhook_configured,
        "detail": "rebalance_alert_webhook_url is configured." if webhook_configured else "rebalance_alert_webhook_url is empty.",
    })

    history_items = await _load_rebalance_metrics_history(app, 1) if elastic_available else []
    checks.append({
        "name": "sink_metrics_available",
        "ok": len(history_items) > 0,
        "detail": "At least one metrics snapshot exists in sink." if history_items else "No metrics snapshots found in sink yet.",
    })

    ready = all(c["ok"] for c in checks)
    return {"ready": ready, "checks": checks}


def _parse_utc_timestamp(value: Any) -> datetime | None:
    if not isinstance(value, str) or not value.strip():
        return None
    normalized = value.replace("Z", "+00:00")
    try:
        parsed = datetime.fromisoformat(normalized)
    except ValueError:
        return None
    if parsed.tzinfo is None:
        return parsed.replace(tzinfo=timezone.utc)
    return parsed.astimezone(timezone.utc)


def _is_recent_enough(value: Any, max_age_minutes: int, now_utc: datetime) -> tuple[bool, str]:
    parsed = _parse_utc_timestamp(value)
    if parsed is None:
        return False, "timestamp missing or invalid"
    cutoff = now_utc - timedelta(minutes=max_age_minutes)
    if parsed < cutoff:
        return False, f"timestamp too old (older than {max_age_minutes} minutes)"
    return True, "timestamp within allowed freshness window"


async def _build_rollout_validation_report(app) -> dict[str, Any]:
    now_utc = datetime.now(timezone.utc)
    readiness = await _build_rollout_readiness(app)
    latest_history = await _load_rebalance_metrics_history(app, 1)
    latest_snapshot = latest_history[0] if latest_history else {}
    sink_alerts = _build_sink_alerts_from_snapshot(latest_snapshot) if latest_snapshot else []
    local_alerts = (await get_rebalance_alerts()).get("alerts", [])

    checks: list[dict[str, Any]] = []
    checks.append({
        "name": "readiness_checks_passed",
        "ok": readiness["ready"],
        "detail": "All rollout readiness checks passed." if readiness["ready"] else "One or more rollout readiness checks failed.",
    })

    metrics_ok, metrics_detail = _is_recent_enough(
        latest_snapshot.get("capturedAtUtc"),
        settings.rebalance_rollout_max_metrics_age_minutes,
        now_utc,
    )
    checks.append({
        "name": "metrics_snapshot_fresh",
        "ok": metrics_ok,
        "detail": metrics_detail,
        "capturedAtUtc": latest_snapshot.get("capturedAtUtc"),
    })

    dry_run_timestamp = (_last_dry_run_report or {}).get("generatedAtUtc")
    dry_run_ok, dry_run_detail = _is_recent_enough(
        dry_run_timestamp,
        settings.rebalance_rollout_max_dry_run_age_minutes,
        now_utc,
    )
    checks.append({
        "name": "dry_run_report_recent",
        "ok": dry_run_ok,
        "detail": dry_run_detail,
        "generatedAtUtc": dry_run_timestamp,
    })

    checks.append({
        "name": "no_active_rebalance_alerts",
        "ok": len(local_alerts) == 0 and len(sink_alerts) == 0,
        "detail": "No local or sink-derived alerts are active." if len(local_alerts) == 0 and len(sink_alerts) == 0 else "Local or sink-derived alerts are active.",
        "localAlertCount": len(local_alerts),
        "sinkAlertCount": len(sink_alerts),
    })

    webhook_configured = bool(settings.rebalance_alert_webhook_url.strip())
    if webhook_configured:
        delivery = _last_alert_delivery
        delivery_ok = delivery.get("lastStatus") == "ok"
        recency_ok, recency_detail = _is_recent_enough(
            delivery.get("lastAttemptAtUtc"),
            settings.rebalance_rollout_max_delivery_age_minutes,
            now_utc,
        )
        checks.append({
            "name": "alert_delivery_healthy",
            "ok": bool(delivery_ok and recency_ok),
            "detail": "last delivery is successful and recent." if delivery_ok and recency_ok else f"delivery unhealthy: status={delivery.get('lastStatus')}; {recency_detail}",
            "lastDelivery": delivery,
        })
    else:
        checks.append({
            "name": "alert_delivery_healthy",
            "ok": False,
            "detail": "rebalance_alert_webhook_url is empty; cannot verify production alert delivery.",
            "lastDelivery": _last_alert_delivery,
        })

    passed = all(c["ok"] for c in checks)
    return {
        "status": "ok",
        "passed": passed,
        "generatedAtUtc": now_utc.isoformat(),
        "checks": checks,
        "readiness": readiness,
        "latestMetricsSnapshot": latest_snapshot if latest_snapshot else None,
        "activeAlerts": {
            "local": local_alerts,
            "sink": sink_alerts,
        },
        "runbook": "docs/REBALANCE_OPERATIONS_RUNBOOK.md",
    }


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

    _rebalance_metrics["totalApplyAttempts"] = int(_rebalance_metrics["totalApplyAttempts"]) + 1
    _rebalance_metrics["lastAttemptAtUtc"] = datetime.now(timezone.utc).isoformat()

    if not body.get("approved"):
        _rebalance_metrics["blockedCount"] = int(_rebalance_metrics["blockedCount"]) + 1
        return {"status": "blocked", "detail": "Approval required. Set approved=true to apply."}

    payload = body.get("payload", {})
    if not isinstance(payload, dict) or not payload:
        _rebalance_metrics["blockedCount"] = int(_rebalance_metrics["blockedCount"]) + 1
        return {"status": "blocked", "detail": "payload is required and must be a non-empty object."}

    approved_by = body.get("approvedBy")
    reason = body.get("reason")
    if not isinstance(approved_by, str) or not approved_by.strip():
        _rebalance_metrics["blockedCount"] = int(_rebalance_metrics["blockedCount"]) + 1
        return {"status": "blocked", "detail": "approvedBy is required for auditability."}
    if not isinstance(reason, str) or not reason.strip():
        _rebalance_metrics["blockedCount"] = int(_rebalance_metrics["blockedCount"]) + 1
        return {"status": "blocked", "detail": "reason is required for auditability."}

    baseline_resp = await request.app.state.backend.get("/admin/economy/balance")
    if baseline_resp.status_code >= 300:
        _rebalance_metrics["errorCount"] = int(_rebalance_metrics["errorCount"]) + 1
        _rebalance_metrics["lastErrorAtUtc"] = datetime.now(timezone.utc).isoformat()
        return {"status": "error", "detail": "Unable to fetch current balance config", "backend_status": baseline_resp.status_code}
    baseline = baseline_resp.json()
    delta_summary = _extract_delta_summary(baseline, payload)

    ok, errors = _validate_rebalance_delta(baseline, payload if isinstance(payload, dict) else {})
    if not ok:
        _rebalance_metrics["blockedCount"] = int(_rebalance_metrics["blockedCount"]) + 1
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
    if status == "ok":
        _rebalance_metrics["successCount"] = int(_rebalance_metrics["successCount"]) + 1
        _rebalance_metrics["lastSuccessAtUtc"] = datetime.now(timezone.utc).isoformat()
    else:
        _rebalance_metrics["errorCount"] = int(_rebalance_metrics["errorCount"]) + 1
        _rebalance_metrics["lastErrorAtUtc"] = datetime.now(timezone.utc).isoformat()

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


@router.get("/economy/rebalance/metrics")
async def get_rebalance_metrics():
    return {"status": "ok", "metrics": _rebalance_metrics}


@router.get("/economy/rebalance/metrics/prometheus")
async def get_rebalance_metrics_prometheus():
    lines = [
        "# HELP tycoon_rebalance_apply_attempts_total Total number of rebalance apply attempts.",
        "# TYPE tycoon_rebalance_apply_attempts_total counter",
        f"tycoon_rebalance_apply_attempts_total {int(_rebalance_metrics['totalApplyAttempts'])}",
        "# HELP tycoon_rebalance_apply_blocked_total Number of blocked rebalance apply attempts.",
        "# TYPE tycoon_rebalance_apply_blocked_total counter",
        f"tycoon_rebalance_apply_blocked_total {int(_rebalance_metrics['blockedCount'])}",
        "# HELP tycoon_rebalance_apply_success_total Number of successful rebalance apply attempts.",
        "# TYPE tycoon_rebalance_apply_success_total counter",
        f"tycoon_rebalance_apply_success_total {int(_rebalance_metrics['successCount'])}",
        "# HELP tycoon_rebalance_apply_error_total Number of errored rebalance apply attempts.",
        "# TYPE tycoon_rebalance_apply_error_total counter",
        f"tycoon_rebalance_apply_error_total {int(_rebalance_metrics['errorCount'])}",
    ]
    return Response(content="\n".join(lines) + "\n", media_type="text/plain; version=0.0.4")


@router.get("/economy/rebalance/alerts")
async def get_rebalance_alerts():
    total = int(_rebalance_metrics["totalApplyAttempts"])
    blocked = int(_rebalance_metrics["blockedCount"])
    success = int(_rebalance_metrics["successCount"])
    error = int(_rebalance_metrics["errorCount"])

    error_rate = (error / total) if total > 0 else 0.0
    blocked_rate = (blocked / total) if total > 0 else 0.0

    alerts: list[dict[str, Any]] = []
    if total >= settings.rebalance_alert_min_attempts and error_rate >= settings.rebalance_alert_error_rate_threshold:
        alerts.append({
            "severity": "high",
            "code": "REBALANCE_ERROR_RATE_HIGH",
            "message": f"Rebalance apply error rate is {error_rate:.1%} ({error}/{total}).",
        })

    if total >= settings.rebalance_alert_min_attempts and blocked_rate >= settings.rebalance_alert_blocked_rate_threshold:
        alerts.append({
            "severity": "medium",
            "code": "REBALANCE_BLOCKED_RATE_HIGH",
            "message": f"Rebalance apply blocked rate is {blocked_rate:.1%} ({blocked}/{total}).",
        })

    return {
        "status": "ok",
        "summary": {
            "totalApplyAttempts": total,
            "blockedCount": blocked,
            "successCount": success,
            "errorCount": error,
            "errorRate": error_rate,
            "blockedRate": blocked_rate,
        },
        "thresholds": {
            "minAttempts": settings.rebalance_alert_min_attempts,
            "errorRateThreshold": settings.rebalance_alert_error_rate_threshold,
            "blockedRateThreshold": settings.rebalance_alert_blocked_rate_threshold,
        },
        "alerts": alerts,
    }


@router.post("/economy/rebalance/metrics/publish")
async def publish_rebalance_metrics(request: Request):
    result = await _publish_rebalance_metrics_snapshot(request.app)
    return result


@router.get("/economy/rebalance/metrics/history")
async def get_rebalance_metrics_history(request: Request, limit: int = 50):
    elastic = getattr(request.app.state, "elasticsearch", None)
    if elastic is None:
        return {"status": "skipped", "detail": "elasticsearch client unavailable", "items": []}
    items = await _load_rebalance_metrics_history(request.app, limit)
    return {"status": "ok", "items": items, "count": len(items)}


@router.get("/economy/rebalance/alerts/sink")
async def get_rebalance_alerts_from_sink(request: Request):
    items = await _load_rebalance_metrics_history(request.app, 1)
    if not items:
        return {"status": "empty", "detail": "No metrics snapshots available in sink.", "alerts": []}

    latest = items[0]
    alerts = _build_sink_alerts_from_snapshot(latest)

    return {
        "status": "ok",
        "capturedAtUtc": latest.get("capturedAtUtc"),
        "alerts": alerts,
    }


@router.post("/economy/rebalance/alerts/dispatch")
async def dispatch_rebalance_alerts(request: Request):
    items = await _load_rebalance_metrics_history(request.app, 1)
    if not items:
        return {"status": "empty", "detail": "No metrics snapshots available in sink.", "dispatched": 0}

    latest = items[0]
    alerts = _build_sink_alerts_from_snapshot(latest)
    if not alerts:
        return {"status": "ok", "detail": "No active alerts to dispatch.", "dispatched": 0}

    webhook = settings.rebalance_alert_webhook_url.strip()
    if not webhook:
        return {"status": "skipped", "detail": "rebalance_alert_webhook_url is not configured.", "dispatched": 0}

    payload = {
        "source": "tycoon-sidecar",
        "event": "rebalance_alerts",
        "capturedAtUtc": latest.get("capturedAtUtc"),
        "alerts": alerts,
    }
    dispatcher = getattr(request.app.state, "alert_dispatcher", None)
    _last_alert_delivery["lastAttemptAtUtc"] = datetime.now(timezone.utc).isoformat()
    result = await _dispatch_alert_webhook(webhook, payload, dispatcher)
    dispatched = len(alerts) if result.get("ok") else 0
    _last_alert_delivery["lastStatus"] = "ok" if result.get("ok") else "error"
    _last_alert_delivery["lastDetail"] = result
    return {"status": "ok" if result.get("ok") else "error", "dispatched": dispatched, "delivery": result}


@router.get("/economy/rebalance/alerts/delivery-health")
async def get_rebalance_alert_delivery_health():
    webhook = settings.rebalance_alert_webhook_url.strip()
    return {
        "status": "ok",
        "configured": bool(webhook),
        "webhookHost": webhook.split("/")[2] if webhook.startswith("http") and "/" in webhook else None,
        "lastDelivery": _last_alert_delivery,
    }


@router.get("/economy/rebalance/rollout-readiness")
async def get_rebalance_rollout_readiness(request: Request):
    readiness = await _build_rollout_readiness(request.app)
    return {
        "status": "ok",
        "ready": readiness["ready"],
        "checks": readiness["checks"],
        "runbook": "docs/REBALANCE_OPERATIONS_RUNBOOK.md",
    }


@router.get("/economy/rebalance/rollout-validation-report")
async def get_rebalance_rollout_validation_report(request: Request):
    return await _build_rollout_validation_report(request.app)


async def run_scheduled_dry_run(app) -> dict:
    """
    Used by sidecar background scheduler in main lifespan.
    """
    global _last_dry_run_report
    _last_dry_run_report = await _generate_dry_run_report(app)
    try:
        await _publish_rebalance_metrics_snapshot(app)
    except Exception:
        logger.exception("Failed to publish rebalance metrics snapshot to external sink")

    try:
        webhook = settings.rebalance_alert_webhook_url.strip()
        if webhook:
            items = await _load_rebalance_metrics_history(app, 1)
            if items:
                alerts = _build_sink_alerts_from_snapshot(items[0])
                if alerts:
                    payload = {
                        "source": "tycoon-sidecar",
                        "event": "rebalance_alerts",
                        "capturedAtUtc": items[0].get("capturedAtUtc"),
                        "alerts": alerts,
                    }
                    dispatcher = getattr(app.state, "alert_dispatcher", None)
                    _last_alert_delivery["lastAttemptAtUtc"] = datetime.now(timezone.utc).isoformat()
                    result = await _dispatch_alert_webhook(webhook, payload, dispatcher)
                    _last_alert_delivery["lastStatus"] = "ok" if result.get("ok") else "error"
                    _last_alert_delivery["lastDetail"] = result
    except Exception:
        logger.exception("Failed to dispatch rebalance alerts")
    return _last_dry_run_report
