"""
Analytics & reporting endpoints.

Routes:
  GET  /analytics/season/{season_id}/summary   — aggregated season KPIs
  GET  /analytics/events/funnel                — event entry → win funnel
  GET  /analytics/retention/{cohort_date}      — D1/D7/D30 retention for a cohort
"""

import logging
from datetime import date

from fastapi import APIRouter, Request

router = APIRouter()
logger = logging.getLogger(__name__)


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
