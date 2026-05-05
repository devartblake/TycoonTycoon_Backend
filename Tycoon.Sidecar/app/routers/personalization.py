from fastapi import APIRouter
from collections import defaultdict
from app.schemas.personalization import (
    ScorePlayerRequest,
    ScorePlayerResponse,
    RecommendationCandidateRequest,
    RecommendationCandidateResponse,
    RecommendationCandidate,
    CategoryProfileRequest,
    CategoryProfileResponse,
    NotificationScoreRequest,
    NotificationScoreResponse,
    MissionFitRequest,
    MissionFitResponse,
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
        # High-frustration players get only low-pressure missions
        candidates.append(RecommendationCandidate(
            type="mission",
            targetId=None,
            score=0.88,
            reason="High frustration detected; a low-pressure confidence-building mission will help recovery.",
            payload={"tone": "supportive", "missionArchetype": "confidence_builder", "isLowPressure": True},
        ))
    else:
        # Recommend missions based on player archetype
        archetype_mission_recommendations: dict[str, tuple[str, str, float]] = {
            "confidence_builder":   ("confidence_builder", "Low-pressure missions help you rebuild confidence step by step.", 0.87),
            "streak_seeker":        ("streak_seeker",      "Keep the momentum — daily streak missions are your strength.", 0.85),
            "explorer":             ("explorer",           "Explore new categories and broaden your knowledge.", 0.84),
            "comeback_player":      ("comeback_player",    "A quick comeback mission gets you back in the game fast.", 0.86),
            "collector":            ("collector",          "Collect badges and milestones across every topic.", 0.84),
            "risk_taker":           ("risk_taker",         "High-stakes challenge missions are built for you.", 0.85),
            "social_challenger":    ("social_challenger",  "Challenge friends and climb the leaderboard.", 0.85),
            "mastery_path":         ("mastery_path",       "Deep-dive mastery missions will push your expertise to the max.", 0.86),
            "new_player":           ("confidence_builder", "Start with confidence-building missions designed for new players.", 0.83),
            "low_pressure_learner": ("confidence_builder", "Low-pressure missions let you learn at your own pace.", 0.85),
        }
        archetype = request.profile.archetype
        mission_archetype, reason, score = archetype_mission_recommendations.get(
            archetype,
            ("explorer", "Explore a variety of missions to discover what suits you best.", 0.70),
        )
        candidates.append(RecommendationCandidate(
            type="mission",
            targetId=None,
            score=score,
            reason=reason,
            payload={"tone": "motivating", "missionArchetype": mission_archetype, "isLowPressure": False},
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
        if request.profile.frustrationRiskScore >= 0.65:
            notif_tone = "supportive"
            notif_intent = "support"
        elif request.profile.churnRiskScore >= 0.60:
            notif_tone = "encouraging"
            notif_intent = "re_engage"
        else:
            notif_tone = "motivating"
            notif_intent = "daily_check_in"

        candidates.append(RecommendationCandidate(
            type="notification",
            targetId=None,
            score=0.72,
            reason="Player can receive a personalised notification.",
            payload={"tone": notif_tone, "intent": notif_intent},
        ))
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

    # Store offer candidates: paid for engaged players, free support for struggling players
    if request.profile.frustrationRiskScore >= 0.65:
        candidates.append(RecommendationCandidate(
            type="store_free_offer",
            targetId=None,
            score=0.82,
            reason="You seem to be struggling — here is a free hint pack to help you get back on track.",
            payload={"tone": "supportive", "isPaid": False, "offerType": "free_hint_pack"},
        ))
    elif request.profile.archetype in ("risk_taker", "collector", "mastery_path", "premium_power_user"):
        candidates.append(RecommendationCandidate(
            type="store_offer",
            targetId=None,
            score=0.78,
            reason="Your play style suggests you'd benefit from premium power-ups.",
            payload={"tone": "motivating", "isPaid": True, "offerType": "powerup_bundle"},
        ))

    candidates.sort(key=lambda c: c.score, reverse=True)
    return RecommendationCandidateResponse(candidates=candidates)


@router.post("/category-profile", response_model=CategoryProfileResponse)
async def category_profile(request: CategoryProfileRequest) -> CategoryProfileResponse:
    correct_by_category: defaultdict[str, int] = defaultdict(int)
    total_by_category: defaultdict[str, int] = defaultdict(int)

    for event in request.recentEvents:
        if event.eventType != "question_answered" or not event.category:
            continue

        category = event.category
        total_by_category[category] += 1
        if bool(event.metadata.get("correct", False)):
            correct_by_category[category] += 1

    strengths: dict[str, float] = {}
    weaknesses: dict[str, float] = {}

    for category, total in total_by_category.items():
        accuracy = round(correct_by_category[category] / total, 4)
        if accuracy >= 0.60:
            strengths[category] = accuracy
        elif accuracy < 0.40:
            weaknesses[category] = accuracy

    top_category = max(strengths, key=lambda k: strengths[k]) if strengths else None
    weakest_category = min(weaknesses, key=lambda k: weaknesses[k]) if weaknesses else None

    return CategoryProfileResponse(
        strengths=strengths,
        weaknesses=weaknesses,
        topCategory=top_category,
        weakestCategory=weakest_category,
    )


@router.post("/notification-score", response_model=NotificationScoreResponse)
async def notification_score(request: NotificationScoreRequest) -> NotificationScoreResponse:
    recent_notification_count = sum(
        1 for event in request.recentEvents
        if event.eventType == "notification_received"
    )

    base_fatigue = request.currentProfile.notificationFatigueScore
    event_penalty = min(0.30, recent_notification_count * 0.05)
    fatigue = round(min(1.0, base_fatigue + event_penalty), 4)

    can_receive = fatigue < 0.75

    if fatigue >= 0.75:
        frequency_hours = 48
    elif fatigue >= 0.50:
        frequency_hours = 24
    elif fatigue >= 0.25:
        frequency_hours = 12
    else:
        frequency_hours = 6

    return NotificationScoreResponse(
        notificationFatigueScore=fatigue,
        canReceiveNotification=can_receive,
        recommendedFrequencyHours=frequency_hours,
    )


# Archetype compatibility map: higher score = better mission fit
_ARCHETYPE_MISSION_FIT: dict[str, dict[str, float]] = {
    "new_player":          {"onboarding": 1.0, "daily_focus": 0.80, "challenge": 0.30, "comeback": 0.60, "streak": 0.40},
    "confidence_builder":  {"onboarding": 0.80, "daily_focus": 0.90, "challenge": 0.30, "comeback": 0.70, "streak": 0.50},
    "streak_seeker":       {"onboarding": 0.50, "daily_focus": 0.90, "challenge": 0.70, "comeback": 0.60, "streak": 1.0},
    "explorer":            {"onboarding": 0.60, "daily_focus": 0.80, "challenge": 0.70, "comeback": 0.50, "streak": 0.60},
    "collector":           {"onboarding": 0.60, "daily_focus": 0.85, "challenge": 0.60, "comeback": 0.60, "streak": 0.70},
    "risk_taker":          {"onboarding": 0.40, "daily_focus": 0.70, "challenge": 1.0, "comeback": 0.50, "streak": 0.70},
    "social_challenger":   {"onboarding": 0.40, "daily_focus": 0.70, "challenge": 0.90, "comeback": 0.50, "streak": 0.80},
    "mastery_path":        {"onboarding": 0.50, "daily_focus": 0.85, "challenge": 0.90, "comeback": 0.50, "streak": 0.75},
    "comeback_player":     {"onboarding": 0.60, "daily_focus": 0.80, "challenge": 0.40, "comeback": 1.0, "streak": 0.50},
    "premium_power_user":  {"onboarding": 0.40, "daily_focus": 0.80, "challenge": 0.90, "comeback": 0.50, "streak": 0.80},
    "low_pressure_learner":{"onboarding": 0.90, "daily_focus": 0.85, "challenge": 0.30, "comeback": 0.60, "streak": 0.40},
}
_DEFAULT_FIT = 0.50


@router.post("/mission-fit", response_model=MissionFitResponse)
async def mission_fit(request: MissionFitRequest) -> MissionFitResponse:
    archetype = request.currentProfile.archetype
    mission = request.missionArchetype

    base_fit = _ARCHETYPE_MISSION_FIT.get(archetype, {}).get(mission, _DEFAULT_FIT)

    # Penalise high-frustration players for demanding missions
    if request.currentProfile.frustrationRiskScore >= 0.65 and mission == "challenge":
        base_fit = max(0.0, base_fit - 0.30)

    # Boost comeback missions for high-churn players
    if request.currentProfile.churnRiskScore >= 0.60 and mission == "comeback":
        base_fit = min(1.0, base_fit + 0.15)

    fit_score = round(base_fit, 4)
    recommended = fit_score >= 0.60

    if fit_score >= 0.85:
        reason = f"Excellent fit: '{archetype}' players thrive with '{mission}' missions."
    elif fit_score >= 0.60:
        reason = f"Good fit: '{archetype}' players generally enjoy '{mission}' missions."
    else:
        reason = f"Low fit: '{archetype}' players rarely engage well with '{mission}' missions."

    return MissionFitResponse(fitScore=fit_score, reason=reason, recommended=recommended)
