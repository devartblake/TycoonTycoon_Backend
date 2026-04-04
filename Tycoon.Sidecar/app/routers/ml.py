"""
ML / AI inference endpoints.

Routes:
  POST /ml/match-quality        — score a match for quality/fairness
  POST /ml/churn-risk           — predict churn probability for a player
  POST /ml/question-difficulty  — estimate difficulty of a trivia question
  GET  /ml/model-info           — return version strings for all loaded models
"""

import threading
from typing import Literal

import numpy as np
from fastapi import APIRouter, HTTPException
from pydantic import BaseModel
from sklearn.linear_model import LogisticRegression
from sklearn.pipeline import Pipeline
from sklearn.feature_extraction.text import TfidfVectorizer
from sklearn.preprocessing import StandardScaler

router = APIRouter()

# ---------------------------------------------------------------------------
# Bootstrap training data — minimal representative samples used to train
# lightweight models at startup. These are replaced by a deployed model call
# (via .NET MlScoringEndpoints) in production; the sidecar models serve as
# a fallback and for direct sidecar clients.
# ---------------------------------------------------------------------------

_DIFFICULTY_TEXTS = [
    # Easy
    "What is 2 + 2?", "What color is the sky?", "Name the capital of France.",
    "How many days in a week?", "What is the largest planet?",
    # Medium
    "What year did World War II end?",
    "Who wrote Romeo and Juliet?",
    "What is the chemical symbol for gold?",
    "What is the speed of light in km/s?",
    "Which country invented pizza?",
    # Hard
    "What is the Krebs cycle and where does it occur?",
    "Explain the difference between mitosis and meiosis.",
    "Who proposed the heliocentric model before Copernicus?",
    "What is the Heisenberg uncertainty principle?",
    "Describe the function of the hypothalamus in thermoregulation.",
    # Expert
    "Derive the Euler-Lagrange equation from Hamilton's principle of least action.",
    "Explain the role of RNA polymerase II in eukaryotic transcription initiation.",
    "What is the significance of the Yang-Mills mass gap conjecture?",
    "How does quantum entanglement relate to Bell's theorem and local hidden variables?",
    "Describe the mechanism by which CRISPR-Cas9 introduces double-strand breaks.",
]
_DIFFICULTY_LABELS = (
    [0] * 5   # easy
    + [1] * 5  # medium
    + [2] * 5  # hard
    + [3] * 5  # expert
)

_CHURN_FEATURES = np.array([
    # [days_inactive, 1 - win_rate, low_sessions]  → low churn
    [0, 0.1, 0], [1, 0.2, 0], [0, 0.15, 0],
    # medium churn
    [5, 0.5, 1], [7, 0.55, 1], [4, 0.6, 0],
    # high churn
    [14, 0.8, 1], [20, 0.9, 1], [30, 0.95, 1],
])
_CHURN_LABELS = [0, 0, 0, 1, 1, 1, 2, 2, 2]

_models_lock = threading.Lock()
_difficulty_pipeline: Pipeline | None = None
_churn_model: LogisticRegression | None = None
_DIFFICULTY_VERSION = "tfidf-lr-v1"
_CHURN_VERSION = "logreg-v1"
_MATCH_QUALITY_VERSION = "heuristic-v1"


def _get_difficulty_pipeline() -> Pipeline:
    global _difficulty_pipeline
    with _models_lock:
        if _difficulty_pipeline is None:
            pipe = Pipeline([
                ("tfidf", TfidfVectorizer(ngram_range=(1, 2), max_features=500)),
                ("clf", LogisticRegression(max_iter=300, C=1.0, random_state=42)),
            ])
            pipe.fit(_DIFFICULTY_TEXTS, _DIFFICULTY_LABELS)
            _difficulty_pipeline = pipe
    return _difficulty_pipeline


def _get_churn_model() -> LogisticRegression:
    global _churn_model
    with _models_lock:
        if _churn_model is None:
            scaler = StandardScaler()
            X = scaler.fit_transform(_CHURN_FEATURES)
            clf = LogisticRegression(max_iter=300, C=1.0, random_state=42, multi_class="multinomial")
            clf.fit(X, _CHURN_LABELS)
            # Store scaler alongside model via a simple wrapper
            clf._scaler = scaler  # type: ignore[attr-defined]
            _churn_model = clf
    return _churn_model


# ---------------------------------------------------------------------------
# Request / response models
# ---------------------------------------------------------------------------

class MatchQualityRequest(BaseModel):
    player_ids: list[str]
    tier: int
    rank_points: list[int]


class MatchQualityResponse(BaseModel):
    quality_score: float
    balance_delta: float
    model_version: str


class ChurnRiskRequest(BaseModel):
    player_id: str
    days_since_last_match: int
    win_rate_7d: float
    rank_points: int


class ChurnRiskResponse(BaseModel):
    player_id: str
    churn_probability: float
    risk_tier: Literal["low", "medium", "high"]
    model_version: str


class QuestionDifficultyRequest(BaseModel):
    question_text: str
    category: str
    options: list[str]


class QuestionDifficultyResponse(BaseModel):
    estimated_difficulty: float
    confidence: float
    model_version: str


class ModelInfoResponse(BaseModel):
    question_difficulty: str
    churn_risk: str
    match_quality: str


# ---------------------------------------------------------------------------
# Endpoints
# ---------------------------------------------------------------------------

@router.post("/match-quality", response_model=MatchQualityResponse)
def score_match_quality(req: MatchQualityRequest) -> MatchQualityResponse:
    if not req.rank_points:
        raise HTTPException(status_code=422, detail="rank_points cannot be empty")
    spread = max(req.rank_points) - min(req.rank_points)
    quality = max(0.0, 1.0 - spread / 2000.0)
    return MatchQualityResponse(
        quality_score=round(quality, 3),
        balance_delta=float(spread),
        model_version=_MATCH_QUALITY_VERSION,
    )


@router.post("/churn-risk", response_model=ChurnRiskResponse)
def predict_churn(req: ChurnRiskRequest) -> ChurnRiskResponse:
    clf = _get_churn_model()
    scaler = clf._scaler  # type: ignore[attr-defined]

    features = np.array([[
        min(req.days_since_last_match, 30),
        max(0.0, 1.0 - req.win_rate_7d),
        1 if req.days_since_last_match > 7 else 0,
    ]])
    features_scaled = scaler.transform(features)
    probs = clf.predict_proba(features_scaled)[0]
    label = int(clf.predict(features_scaled)[0])

    churn_prob = round(float(probs[min(label, len(probs) - 1)]), 3)
    tier: Literal["low", "medium", "high"] = (
        "high" if label == 2 else "medium" if label == 1 else "low"
    )
    return ChurnRiskResponse(
        player_id=req.player_id,
        churn_probability=churn_prob,
        risk_tier=tier,
        model_version=_CHURN_VERSION,
    )


@router.post("/question-difficulty", response_model=QuestionDifficultyResponse)
def estimate_difficulty(req: QuestionDifficultyRequest) -> QuestionDifficultyResponse:
    pipe = _get_difficulty_pipeline()
    text = req.question_text.strip()
    if not text:
        raise HTTPException(status_code=422, detail="question_text cannot be empty")

    label = int(pipe.predict([text])[0])
    probs = pipe.predict_proba([text])[0]
    confidence = round(float(max(probs)), 3)

    # Map 4-class label (0=easy … 3=expert) to 0–1 float
    difficulty = round(label / 3.0, 3)

    return QuestionDifficultyResponse(
        estimated_difficulty=difficulty,
        confidence=confidence,
        model_version=_DIFFICULTY_VERSION,
    )


@router.get("/model-info", response_model=ModelInfoResponse)
def model_info() -> ModelInfoResponse:
    return ModelInfoResponse(
        question_difficulty=_DIFFICULTY_VERSION,
        churn_risk=_CHURN_VERSION,
        match_quality=_MATCH_QUALITY_VERSION,
    )
