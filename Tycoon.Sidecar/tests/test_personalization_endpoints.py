from fastapi import FastAPI
from fastapi.testclient import TestClient

from app.routers.personalization import router


def _make_client() -> TestClient:
    app = FastAPI()
    app.include_router(router)
    return TestClient(app)


# ---------------------------------------------------------------------------
# /personalization/score-player
# ---------------------------------------------------------------------------

def test_score_player_empty_events_returns_defaults():
    client = _make_client()
    resp = client.post("/personalization/score-player", json={
        "playerId": "p1",
        "recentEvents": [],
        "currentProfile": {
            "confidenceLevel": 0.5,
            "churnRiskScore": 0.0,
            "frustrationRiskScore": 0.0,
            "notificationFatigueScore": 0.0,
            "archetype": "new_player",
        },
    })
    assert resp.status_code == 200
    body = resp.json()
    assert body["frustrationRiskScore"] == 0.0
    assert body["confidenceLevel"] == 1.0
    assert body["recommendedArchetype"] == "new_player"
    assert body["signals"]["totalAnswers"] == 0


def test_score_player_high_miss_rate_raises_frustration_and_overrides_archetype():
    client = _make_client()
    # All wrong AND slow (>9000 ms) → frustration = min(1.0, 0.65 + 0.35) = 1.0 ≥ 0.70
    events = [
        {
            "eventType": "question_answered",
            "eventSource": "quiz",
            "category": "science",
            "metadata": {"correct": False, "answerTimeMs": 10000},
        }
        for _ in range(8)
    ]
    resp = client.post("/personalization/score-player", json={
        "playerId": "p2",
        "recentEvents": events,
        "currentProfile": {
            "confidenceLevel": 0.5,
            "churnRiskScore": 0.0,
            "frustrationRiskScore": 0.0,
            "notificationFatigueScore": 0.0,
            "archetype": "mastery_path",
        },
    })
    assert resp.status_code == 200
    body = resp.json()
    assert body["frustrationRiskScore"] >= 0.60
    assert body["recommendedArchetype"] == "confidence_builder"
    assert "science" in body["categoryWeaknesses"]


def test_score_player_mastery_archetype_for_strong_performer():
    client = _make_client()
    events = [
        {
            "eventType": "question_answered",
            "eventSource": "quiz",
            "metadata": {"correct": True, "answerTimeMs": 2000},
        }
        for _ in range(25)
    ]
    resp = client.post("/personalization/score-player", json={
        "playerId": "p3",
        "recentEvents": events,
        "currentProfile": {
            "confidenceLevel": 0.9,
            "churnRiskScore": 0.0,
            "frustrationRiskScore": 0.0,
            "notificationFatigueScore": 0.0,
            "archetype": "mastery_path",
        },
    })
    assert resp.status_code == 200
    body = resp.json()
    assert body["recommendedArchetype"] == "mastery_path"
    assert body["frustrationRiskScore"] == 0.0


# ---------------------------------------------------------------------------
# /personalization/recommendation-candidates
# ---------------------------------------------------------------------------

def test_recommendation_candidates_high_frustration_adds_learning_module():
    client = _make_client()
    resp = client.post("/personalization/recommendation-candidates", json={
        "playerId": "p1",
        "profile": {
            "confidenceLevel": 0.3,
            "churnRiskScore": 0.5,
            "frustrationRiskScore": 0.70,
            "notificationFatigueScore": 0.1,
            "archetype": "confidence_builder",
        },
        "recentEvents": [],
    })
    assert resp.status_code == 200
    types = [c["type"] for c in resp.json()["candidates"]]
    assert "learning_module" in types


def test_recommendation_candidates_sorted_by_score_descending():
    client = _make_client()
    resp = client.post("/personalization/recommendation-candidates", json={
        "playerId": "p2",
        "profile": {
            "confidenceLevel": 0.3,
            "churnRiskScore": 0.65,
            "frustrationRiskScore": 0.70,
            "notificationFatigueScore": 0.1,
            "archetype": "streak_seeker",
        },
        "recentEvents": [],
    })
    assert resp.status_code == 200
    scores = [c["score"] for c in resp.json()["candidates"]]
    assert scores == sorted(scores, reverse=True)


# ---------------------------------------------------------------------------
# /personalization/category-profile
# ---------------------------------------------------------------------------

def test_category_profile_empty_events_returns_empty_maps():
    client = _make_client()
    resp = client.post("/personalization/category-profile", json={
        "playerId": "p1",
        "recentEvents": [],
    })
    assert resp.status_code == 200
    body = resp.json()
    assert body["strengths"] == {}
    assert body["weaknesses"] == {}
    assert body["topCategory"] is None
    assert body["weakestCategory"] is None


def test_category_profile_identifies_strength_and_weakness():
    client = _make_client()
    # 8/10 correct in "history" → strength; 1/10 correct in "science" → weakness
    events = (
        [
            {
                "eventType": "question_answered",
                "eventSource": "quiz",
                "category": "history",
                "metadata": {"correct": True},
            }
            for _ in range(8)
        ]
        + [
            {
                "eventType": "question_answered",
                "eventSource": "quiz",
                "category": "history",
                "metadata": {"correct": False},
            }
            for _ in range(2)
        ]
        + [
            {
                "eventType": "question_answered",
                "eventSource": "quiz",
                "category": "science",
                "metadata": {"correct": True},
            }
        ]
        + [
            {
                "eventType": "question_answered",
                "eventSource": "quiz",
                "category": "science",
                "metadata": {"correct": False},
            }
            for _ in range(9)
        ]
    )
    resp = client.post("/personalization/category-profile", json={
        "playerId": "p2",
        "recentEvents": events,
    })
    assert resp.status_code == 200
    body = resp.json()
    assert "history" in body["strengths"]
    assert "science" in body["weaknesses"]
    assert body["topCategory"] == "history"
    assert body["weakestCategory"] == "science"


def test_category_profile_ignores_non_question_events():
    client = _make_client()
    resp = client.post("/personalization/category-profile", json={
        "playerId": "p3",
        "recentEvents": [
            {"eventType": "match_started", "eventSource": "game", "category": "history", "metadata": {}},
            {"eventType": "question_answered", "eventSource": "quiz", "category": None, "metadata": {"correct": True}},
        ],
    })
    assert resp.status_code == 200
    body = resp.json()
    assert body["strengths"] == {}
    assert body["weaknesses"] == {}


# ---------------------------------------------------------------------------
# /personalization/notification-score
# ---------------------------------------------------------------------------

def test_notification_score_low_fatigue_allows_notifications():
    client = _make_client()
    resp = client.post("/personalization/notification-score", json={
        "playerId": "p1",
        "currentProfile": {
            "confidenceLevel": 0.7,
            "churnRiskScore": 0.1,
            "frustrationRiskScore": 0.1,
            "notificationFatigueScore": 0.1,
            "archetype": "explorer",
        },
        "recentEvents": [],
    })
    assert resp.status_code == 200
    body = resp.json()
    assert body["canReceiveNotification"] is True
    assert body["notificationFatigueScore"] == 0.1
    assert body["recommendedFrequencyHours"] <= 12


def test_notification_score_high_fatigue_blocks_notifications():
    client = _make_client()
    resp = client.post("/personalization/notification-score", json={
        "playerId": "p2",
        "currentProfile": {
            "confidenceLevel": 0.5,
            "churnRiskScore": 0.2,
            "frustrationRiskScore": 0.2,
            "notificationFatigueScore": 0.80,
            "archetype": "new_player",
        },
        "recentEvents": [],
    })
    assert resp.status_code == 200
    body = resp.json()
    assert body["canReceiveNotification"] is False
    assert body["notificationFatigueScore"] >= 0.75
    assert body["recommendedFrequencyHours"] == 48


def test_notification_score_recent_events_increase_fatigue():
    client = _make_client()
    events = [
        {"eventType": "notification_received", "eventSource": "system", "metadata": {}}
        for _ in range(4)
    ]
    resp = client.post("/personalization/notification-score", json={
        "playerId": "p3",
        "currentProfile": {
            "confidenceLevel": 0.5,
            "churnRiskScore": 0.2,
            "frustrationRiskScore": 0.2,
            "notificationFatigueScore": 0.60,
            "archetype": "new_player",
        },
        "recentEvents": events,
    })
    assert resp.status_code == 200
    body = resp.json()
    # base 0.60 + 4 * 0.05 = 0.80 → capped at 0.80, blocked
    assert body["notificationFatigueScore"] == 0.80
    assert body["canReceiveNotification"] is False


# ---------------------------------------------------------------------------
# /personalization/mission-fit
# ---------------------------------------------------------------------------

def test_mission_fit_streak_seeker_matches_streak_mission():
    client = _make_client()
    resp = client.post("/personalization/mission-fit", json={
        "playerId": "p1",
        "currentProfile": {
            "confidenceLevel": 0.7,
            "churnRiskScore": 0.1,
            "frustrationRiskScore": 0.1,
            "notificationFatigueScore": 0.1,
            "archetype": "streak_seeker",
        },
        "missionArchetype": "streak",
        "recentEvents": [],
    })
    assert resp.status_code == 200
    body = resp.json()
    assert body["fitScore"] == 1.0
    assert body["recommended"] is True


def test_mission_fit_low_pressure_learner_bad_fit_for_challenge():
    client = _make_client()
    resp = client.post("/personalization/mission-fit", json={
        "playerId": "p2",
        "currentProfile": {
            "confidenceLevel": 0.4,
            "churnRiskScore": 0.2,
            "frustrationRiskScore": 0.2,
            "notificationFatigueScore": 0.1,
            "archetype": "low_pressure_learner",
        },
        "missionArchetype": "challenge",
        "recentEvents": [],
    })
    assert resp.status_code == 200
    body = resp.json()
    assert body["fitScore"] < 0.60
    assert body["recommended"] is False


def test_mission_fit_high_frustration_penalises_challenge():
    client = _make_client()
    resp = client.post("/personalization/mission-fit", json={
        "playerId": "p3",
        "currentProfile": {
            "confidenceLevel": 0.3,
            "churnRiskScore": 0.3,
            "frustrationRiskScore": 0.70,
            "notificationFatigueScore": 0.1,
            "archetype": "risk_taker",
        },
        "missionArchetype": "challenge",
        "recentEvents": [],
    })
    assert resp.status_code == 200
    body = resp.json()
    # risk_taker + challenge base = 1.0, minus 0.30 penalty = 0.70
    assert body["fitScore"] == 0.70
    assert body["recommended"] is True


def test_mission_fit_high_churn_boosts_comeback_mission():
    client = _make_client()
    resp = client.post("/personalization/mission-fit", json={
        "playerId": "p4",
        "currentProfile": {
            "confidenceLevel": 0.4,
            "churnRiskScore": 0.65,
            "frustrationRiskScore": 0.2,
            "notificationFatigueScore": 0.1,
            "archetype": "comeback_player",
        },
        "missionArchetype": "comeback",
        "recentEvents": [],
    })
    assert resp.status_code == 200
    body = resp.json()
    # comeback_player + comeback base = 1.0, boost = min(1.0, 1.0 + 0.15) = 1.0
    assert body["fitScore"] == 1.0
    assert body["recommended"] is True


def test_mission_fit_unknown_archetype_uses_default_score():
    client = _make_client()
    resp = client.post("/personalization/mission-fit", json={
        "playerId": "p5",
        "currentProfile": {
            "confidenceLevel": 0.5,
            "churnRiskScore": 0.1,
            "frustrationRiskScore": 0.1,
            "notificationFatigueScore": 0.1,
            "archetype": "unknown_archetype",
        },
        "missionArchetype": "daily_focus",
        "recentEvents": [],
    })
    assert resp.status_code == 200
    body = resp.json()
    assert body["fitScore"] == 0.50
