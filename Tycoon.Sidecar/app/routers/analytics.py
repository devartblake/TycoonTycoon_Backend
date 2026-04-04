"""
Analytics & reporting endpoints.

Routes:
  POST /analytics/events                       — ingest a single analytics event (idempotent)
  GET  /analytics/season/{season_id}/summary   — aggregated season KPIs
  GET  /analytics/events/funnel                — event entry → win funnel
  GET  /analytics/retention/{cohort_date}      — D1/D7/D30 retention for a cohort
  POST /analytics/behavior-segmentation        — classify player archetype for adaptive balancing
"""

import hashlib
import json
import logging
from datetime import date, datetime

from fastapi import APIRouter, Request, status
from fastapi.responses import JSONResponse
from pymongo.errors import DuplicateKeyError
from pydantic import BaseModel

router = APIRouter()
logger = logging.getLogger(__name__)


class AnalyticsEventRequest(BaseModel):
    user_id: str
    event_type: str
    payload: dict = {}
    event_id: str | None = None  # client-supplied; generated from content hash if omitted


def _compute_event_hash(user_id: str, event_type: str, payload: dict) -> str:
    canonical = json.dumps(
        {"user_id": user_id, "event_type": event_type, **payload},
        sort_keys=True,
        default=str,
    )
    return hashlib.sha256(canonical.encode()).hexdigest()


@router.post("/events")
async def ingest_event(event: AnalyticsEventRequest, request: Request):
    """
    Ingest a single analytics event. Idempotent: duplicate event_ids are silently
    acknowledged rather than rejected, so clients can safely retry on network failure.
    """
    db = request.app.state.mongo_db
    event_id = event.event_id or _compute_event_hash(event.user_id, event.event_type, event.payload)

    doc = {
        "event_id": event_id,
        "user_id": event.user_id,
        "event_type": event.event_type,
        "payload": event.payload,
        "received_at": datetime.utcnow().isoformat(),
    }

    try:
        await db["analytics_events"].insert_one(doc)
        return JSONResponse(status_code=status.HTTP_202_ACCEPTED, content={"status": "accepted", "event_id": event_id})
    except DuplicateKeyError:
        return JSONResponse(status_code=status.HTTP_200_OK, content={"status": "duplicate", "event_id": event_id})


class BehaviorSegmentationRequest(BaseModel):
    player_id: str
    matches_7d: int
    ranked_share: float
    jackpot_entries_7d: int
    cosmetic_spend_30d: int
    loss_streak: int


class BehaviorSegmentationResponse(BaseModel):
    player_id: str
    segment: str
    confidence: float
    recommendation: dict


@router.get("/season/{season_id}/summary")
async def season_summary(season_id: str, request: Request):
    """
    Aggregates match counts, active players, avg rank points movement
    for a season from the analytics MongoDB database.
    """
    db = request.app.state.mongo_db
    match_filter = {"seasonId": season_id}
    total_matches = await db.matches.count_documents(match_filter)
    active_players = len(await db.matches.distinct("playerId", filter=match_filter))
    events_completed = await db.events.count_documents({"seasonId": season_id, "status": "completed"})

    rank_pipeline = [
        {"$match": {"seasonId": season_id, "rankDelta": {"$type": "number"}}},
        {"$group": {"_id": None, "avgRankDelta": {"$avg": "$rankDelta"}}},
    ]
    rank_stats = await db.matches.aggregate(rank_pipeline).to_list(length=1)
    avg_rank_delta = float(rank_stats[0]["avgRankDelta"]) if rank_stats else 0.0

    return {
        "season_id": season_id,
        "status": "ok",
        "metrics": {
            "total_matches": total_matches,
            "active_players": active_players,
            "avg_rank_delta": round(avg_rank_delta, 2),
            "events_completed": events_completed,
        },
    }


@router.get("/events/funnel")
async def event_funnel(request: Request, season_id: str | None = None):
    """
    Entry → survival → top20 → win funnel across all game events.
    """
    es = request.app.state.elasticsearch
    must = [{"term": {"eventType.keyword": "game_event"}}]
    if season_id:
        must.append({"term": {"seasonId.keyword": season_id}})

    query = {"bool": {"must": must}}
    stages = {
        "entered": {"match_phrase": {"stage.keyword": "entered"}},
        "survived_first_elimination": {"match_phrase": {"stage.keyword": "survived_first_elimination"}},
        "top20": {"match_phrase": {"stage.keyword": "top20"}},
        "won": {"match_phrase": {"stage.keyword": "won"}},
    }
    aggs = {name: {"filter": clause} for name, clause in stages.items()}
    res = await es.search(index="tycoon-events-*", query=query, aggs=aggs, size=0)
    buckets = res.get("aggregations", {})

    return {
        "season_id": season_id,
        "status": "ok",
        "funnel": {
            "entered": int(buckets.get("entered", {}).get("doc_count", 0)),
            "survived_first_elimination": int(buckets.get("survived_first_elimination", {}).get("doc_count", 0)),
            "top20": int(buckets.get("top20", {}).get("doc_count", 0)),
            "won": int(buckets.get("won", {}).get("doc_count", 0)),
        },
    }


@router.get("/retention/{cohort_date}")
async def retention(cohort_date: date, request: Request):
    """
    D1 / D7 / D30 retention for players who first logged in on cohort_date.
    """
    db = request.app.state.mongo_db
    cohort = cohort_date.isoformat()

    users = await db.player_sessions.distinct("playerId", filter={"firstSeenDate": cohort})
    total = len(users)
    if total == 0:
        return {
            "cohort_date": cohort,
            "status": "ok",
            "retention": {"d1": 0.0, "d7": 0.0, "d30": 0.0},
        }

    d1 = await db.player_activity.count_documents({"playerId": {"$in": users}, "daysSinceFirstSeen": 1})
    d7 = await db.player_activity.count_documents({"playerId": {"$in": users}, "daysSinceFirstSeen": 7})
    d30 = await db.player_activity.count_documents({"playerId": {"$in": users}, "daysSinceFirstSeen": 30})

    return {
        "cohort_date": cohort,
        "status": "ok",
        "retention": {
            "d1": round(d1 / total, 4),
            "d7": round(d7 / total, 4),
            "d30": round(d30 / total, 4),
        },
    }


@router.post("/behavior-segmentation", response_model=BehaviorSegmentationResponse)
async def behavior_segmentation(req: BehaviorSegmentationRequest):
    """
    Lightweight rule-based segmentation used as a Phase 3 bridge
    until a model-backed segmentation pipeline is wired in.
    """
    if req.loss_streak >= 3:
        segment = "struggling"
        confidence = 0.82
        recommendation = {"energyCostDelta": -1, "extraLives": 1, "difficultyReduction": 0.1}
    elif req.jackpot_entries_7d >= 3:
        segment = "risk_taker"
        confidence = 0.78
        recommendation = {"jackpotBoost": True, "reviveOfferPriority": "high"}
    elif req.ranked_share >= 0.6 and req.matches_7d >= 10:
        segment = "competitive"
        confidence = 0.8
        recommendation = {"rankedRewardBoost": True, "xpBoostOffer": True}
    elif req.cosmetic_spend_30d > 0:
        segment = "collector"
        confidence = 0.74
        recommendation = {"cosmeticBundleOffer": True, "eventBadgeOffer": True}
    else:
        segment = "casual"
        confidence = 0.7
        recommendation = {"lowerEnergyEvents": True, "missionBundle": True}

    return BehaviorSegmentationResponse(
        player_id=req.player_id,
        segment=segment,
        confidence=confidence,
        recommendation=recommendation,
    )
