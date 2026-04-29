from fastapi import APIRouter
from app.schemas.personalization import (
    ScorePlayerRequest,
    ScorePlayerResponse,
    RecommendationCandidateRequest,
    RecommendationCandidateResponse,
    RecommendationCandidate,
)

router = APIRouter(prefix="/personalization", tags=["personalization"])


@router.post("/score-player", response_model=ScorePlayerResponse)
async def score_player(request: ScorePlayerRequest) -> ScorePlayerResponse:
    incorrect = 0
    total_answers = 0
    slow_answers = 0
    category_misses: dict[str, int] = {}

    for event in request.recentEvents:
        if event.eventType == "question_answered":
            total_answers += 1
            correct = bool(event.metadata.get("correct", False))
            answer_time_ms = int(event.metadata.get("answerTimeMs", 0))

            if not correct:
                incorrect += 1
                if event.category:
                    category_misses[event.category] = category_misses.get(event.category, 0) + 1

            if answer_time_ms > 9000:
                slow_answers += 1

    miss_rate = incorrect / total_answers if total_answers else 0.0
    slow_rate = slow_answers / total_answers if total_answers else 0.0

    frustration = min(1.0, (miss_rate * 0.65) + (slow_rate * 0.35))
    confidence = max(0.0, min(1.0, 1.0 - frustration))

    if total_answers < 5:
        archetype = "new_player"
    elif frustration >= 0.70:
        archetype = "confidence_builder"
    elif miss_rate < 0.20 and total_answers >= 20:
        archetype = "mastery_path"
    else:
        archetype = request.currentProfile.archetype

    weaknesses = {
        category: min(1.0, misses / max(1, total_answers))
        for category, misses in category_misses.items()
    }

    churn_risk = min(
        1.0,
        request.currentProfile.churnRiskScore + (frustration * 0.10),
    )

    return ScorePlayerResponse(
        churnRiskScore=round(churn_risk, 4),
        frustrationRiskScore=round(frustration, 4),
        confidenceLevel=round(confidence, 4),
        recommendedArchetype=archetype,
        categoryStrengths={},
        categoryWeaknesses=weaknesses,
        signals={
            "missRate": round(miss_rate, 4),
            "slowRate": round(slow_rate, 4),
            "totalAnswers": total_answers,
        },
    )


@router.post("/recommendation-candidates", response_model=RecommendationCandidateResponse)
async def recommendation_candidates(
    request: RecommendationCandidateRequest,
) -> RecommendationCandidateResponse:
    candidates: list[RecommendationCandidate] = []

    if request.profile.frustrationRiskScore >= 0.65:
        candidates.append(RecommendationCandidate(
            type="learning_module",
            targetId="confidence-warmup",
            score=0.92,
            reason="Player has elevated frustration risk; recommend low-pressure learning.",
            payload={"tone": "encouraging", "difficultyStrategy": "warmup"},
        ))

    if request.profile.churnRiskScore >= 0.60:
        candidates.append(RecommendationCandidate(
            type="mission",
            targetId=None,
            score=0.85,
            reason="Player shows churn risk; a quick mission can re-engage them.",
            payload={"tone": "encouraging", "missionArchetype": "comeback_player"},
        ))

    if request.profile.notificationFatigueScore < 0.50:
        candidates.append(RecommendationCandidate(
            type="coach_tip",
            targetId=None,
            score=0.70,
            reason="Player can receive one helpful coach recommendation.",
            payload={"tone": "supportive"},
        ))

    if request.profile.archetype in ("streak_seeker", "risk_taker", "social_challenger"):
        candidates.append(RecommendationCandidate(
            type="event",
            targetId=None,
            score=0.75,
            reason="Competitive player archetype benefits from event recommendations.",
            payload={"tone": "competitive"},
        ))

    candidates.sort(key=lambda c: c.score, reverse=True)
    return RecommendationCandidateResponse(candidates=candidates)
