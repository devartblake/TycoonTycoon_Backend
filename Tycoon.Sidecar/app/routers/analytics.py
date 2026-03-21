"""
Analytics & reporting endpoints.

Routes:
  GET  /analytics/season/{season_id}/summary   — aggregated season KPIs
  GET  /analytics/events/funnel                — event entry → win funnel
  GET  /analytics/retention/{cohort_date}      — D1/D7/D30 retention for a cohort
  POST /analytics/behavior-segmentation        — classify player archetype for adaptive balancing
"""

import logging
from datetime import date

from fastapi import APIRouter, Request
from pydantic import BaseModel

router = APIRouter()
logger = logging.getLogger(__name__)


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
    # TODO: query motor (MongoDB async driver) via request.app.state.mongo
    return {
        "season_id": season_id,
        "status": "stub — wire up motor client to tycoon_analytics DB",
        "metrics": {
            "total_matches": 0,
            "active_players": 0,
            "avg_rank_delta": 0.0,
            "events_completed": 0,
        },
    }


@router.get("/events/funnel")
async def event_funnel(request: Request, season_id: str | None = None):
    """
    Entry → survival → top20 → win funnel across all game events.
    """
    return {
        "season_id": season_id,
        "status": "stub — wire up Elasticsearch aggregation",
        "funnel": {
            "entered": 0,
            "survived_first_elimination": 0,
            "top20": 0,
            "won": 0,
        },
    }


@router.get("/retention/{cohort_date}")
async def retention(cohort_date: date, request: Request):
    """
    D1 / D7 / D30 retention for players who first logged in on cohort_date.
    """
    return {
        "cohort_date": cohort_date.isoformat(),
        "status": "stub — wire up MongoDB cohort query",
        "retention": {"d1": 0.0, "d7": 0.0, "d30": 0.0},
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
