"""
ML / AI inference endpoints.

Routes:
  POST /ml/match-quality        — score a match for quality/fairness
  POST /ml/churn-risk           — predict churn probability for a player
  POST /ml/question-difficulty  — estimate difficulty of a trivia question
  GET  /ml/model-info           — return version strings for all loaded models
"""

from __future__ import annotations

import re
import threading
from typing import Any, Literal

import numpy as np
from fastapi import APIRouter, HTTPException
from pydantic import BaseModel
from sklearn.linear_model import LogisticRegression
from sklearn.pipeline import Pipeline
from sklearn.feature_extraction.text import TfidfVectorizer
from sklearn.preprocessing import StandardScaler
from app.config import settings

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

_CATEGORY_DEFS: dict[str, dict[str, Any]] = {
    "general": {"display": "General", "subject": "general", "audience": "general", "aliases": ["general", "general_knowledge", "mixed"]},
    "science": {"display": "Science", "subject": "stem", "audience": "general", "aliases": ["science", "natural_science", "physics", "biology", "chemistry"]},
    "mathematics": {"display": "Mathematics", "subject": "stem", "audience": "general", "aliases": ["math", "maths", "mathematics", "algebra", "geometry"]},
    "history": {"display": "History", "subject": "humanities", "audience": "general", "aliases": ["history", "world_history", "historical"]},
    "geography": {"display": "Geography", "subject": "humanities", "audience": "general", "aliases": ["geography", "world_geography"]},
    "technology": {"display": "Technology", "subject": "stem", "audience": "general", "aliases": ["technology", "tech", "computer_science", "computing"]},
    "arts": {"display": "Arts", "subject": "arts", "audience": "general", "aliases": ["arts", "art", "fine_arts", "creative_arts"]},
    "kids": {"display": "Kids", "subject": "k12", "audience": "kids", "aliases": ["kids", "kids_questions", "kidsgrade2", "kidsGrade2", "class_1", "class_2"]},
    "sports": {"display": "Sports", "subject": "general", "audience": "general", "aliases": ["sports", "sport"]},
    "entertainment": {"display": "Entertainment", "subject": "media", "audience": "general", "aliases": ["entertainment", "movies", "film", "music", "pop_culture"]},
    "law": {"display": "Law", "subject": "civics", "audience": "general", "aliases": ["law", "civics_law"]},
    "business": {"display": "Business", "subject": "business", "audience": "general", "aliases": ["business", "economics", "finance"]},
    "health": {"display": "Health", "subject": "health", "audience": "general", "aliases": ["health", "medicine", "health_medicine"]},
}

_ALIAS_TO_CATEGORY = {
    re.sub(r"[^a-z0-9]+", "_", alias.lower()).strip("_"): key
    for key, definition in _CATEGORY_DEFS.items()
    for alias in [key, definition["display"], *definition["aliases"]]
}


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


def _normalize_key(value: str | None) -> str:
    if not value:
        return ""
    return re.sub(r"[^a-z0-9]+", "_", value.lower()).strip("_")


def _first_non_blank(*values: str | None) -> str | None:
    for value in values:
        if value and value.strip():
            return value.strip()
    return None


def _infer_class_grade(dataset: str | None) -> tuple[str | None, str | None, str | None]:
    key = _normalize_key(dataset)
    match = re.search(r"class_?(k|[0-9]{1,2})", key)
    if not match:
        return None, None, None
    raw = match.group(1)
    grade = 0 if raw == "k" else int(raw)
    if grade <= 2:
        return "k_2", "early_elementary", "kids"
    if grade <= 5:
        return "grades_3_5", "upper_elementary", "kids"
    if grade <= 8:
        return "middle_school", "middle_school", "teen"
    return "high_school", "high_school", "teen"


def _infer_category(req: QuestionTaxonomyRequest) -> tuple[str, float, list[str]]:
    warnings: list[str] = []
    candidates: list[tuple[str, float]] = []
    dataset = _first_non_blank(req.sourceDataset, req.currentTaxonomy.sourceDataset if req.currentTaxonomy else None)
    for value, confidence in [
        (req.currentTaxonomy.canonicalCategory if req.currentTaxonomy else None, 0.95),
        (req.category, 0.90),
        (dataset, 0.82),
        (" ".join(req.tags or []), 0.78),
        (req.text, 0.62),
    ]:
        key = _normalize_key(value)
        for alias, category in _ALIAS_TO_CATEGORY.items():
            if key == alias or f"_{alias}_" in f"_{key}_":
                candidates.append((category, confidence))
                break

    if not candidates:
        text = req.text.lower()
        keyword_map = [
            ("science", ["atom", "planet", "gravity", "biology", "chemistry", "physics"]),
            ("mathematics", ["equation", "sum", "multiply", "fraction", "algebra"]),
            ("history", ["war", "ancient", "empire", "president", "century"]),
            ("geography", ["capital", "country", "river", "continent", "map"]),
            ("arts", ["painting", "music", "artist", "theater", "sculpture"]),
        ]
        for category, keywords in keyword_map:
            if any(word in text for word in keywords):
                candidates.append((category, 0.58))
                break

    if not candidates:
        warnings.append("No strong taxonomy category signal found; defaulted to general.")
        return "general", 0.35, warnings

    categories = {category for category, _ in candidates}
    if len(categories) > 1:
        warnings.append("Multiple taxonomy category signals found; selected highest-confidence candidate.")
    return max(candidates, key=lambda item: item[1])[0], max(score for _, score in candidates), warnings


def _suggest_taxonomy(req: QuestionTaxonomyRequest) -> QuestionTaxonomyResponse:
    text = req.text.strip()
    if not text:
        raise HTTPException(status_code=422, detail="text cannot be empty")

    canonical, category_confidence, warnings = _infer_category(req)
    definition = _CATEGORY_DEFS.get(canonical, _CATEGORY_DEFS["general"])
    dataset = _first_non_blank(req.sourceDataset, req.currentTaxonomy.sourceDataset if req.currentTaxonomy else None)
    grade_band, age_group, audience_from_dataset = _infer_class_grade(dataset)
    current = req.currentTaxonomy
    tags = {
        _normalize_key(tag)
        for tag in (req.tags or []) + (current.taxonomyTags if current and current.taxonomyTags else [])
        if _normalize_key(tag)
    }
    tags.update([canonical, definition["subject"]])
    if dataset:
        tags.add(_normalize_key(dataset.split("/")[-1]))

    subject = _first_non_blank(current.subject if current else None, definition["subject"])
    topic = _first_non_blank(current.topic if current else None)
    if not topic:
        normalized_category = _normalize_key(req.category)
        if normalized_category and normalized_category not in {canonical, "general"}:
            topic = normalized_category

    field_confidences = {
        "canonicalCategory": round(category_confidence, 3),
        "displayCategory": round(category_confidence, 3),
        "subject": 0.86 if subject else 0.5,
        "topic": 0.75 if topic else 0.45,
        "gradeBand": 0.88 if grade_band else 0.45,
        "ageGroup": 0.88 if age_group else 0.45,
        "audience": 0.84 if audience_from_dataset or definition.get("audience") else 0.5,
    }
    overall = round(sum(field_confidences.values()) / len(field_confidences), 3)
    if overall < 0.65:
        warnings.append("Overall taxonomy confidence is low; review before applying.")

    return QuestionTaxonomyResponse(
        canonicalCategory=canonical,
        displayCategory=definition["display"],
        subject=subject,
        topic=topic,
        subtopic=current.subtopic if current else None,
        gradeBand=_first_non_blank(current.gradeBand if current else None, grade_band),
        ageGroup=_first_non_blank(current.ageGroup if current else None, age_group),
        audience=_first_non_blank(current.audience if current else None, audience_from_dataset, definition["audience"]),
        questionType="multiple_choice",
        mediaType="text",
        taxonomyTags=sorted(tags),
        fieldConfidences=field_confidences,
        overallConfidence=overall,
        modelVersion=settings.question_taxonomy_model_version,
        warnings=warnings,
    )


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


class CurrentQuestionTaxonomy(BaseModel):
    canonicalCategory: str | None = None
    displayCategory: str | None = None
    subject: str | None = None
    topic: str | None = None
    subtopic: str | None = None
    gradeBand: str | None = None
    ageGroup: str | None = None
    audience: str | None = None
    sourceDataset: str | None = None
    taxonomyTags: list[str] | None = None


class QuestionTaxonomyRequest(BaseModel):
    text: str
    category: str | None = None
    difficulty: str | int | None = None
    options: list[str] | None = None
    tags: list[str] | None = None
    sourceDataset: str | None = None
    sourceQuestionId: str | None = None
    currentTaxonomy: CurrentQuestionTaxonomy | None = None


class QuestionTaxonomyResponse(BaseModel):
    canonicalCategory: str
    displayCategory: str
    subject: str | None = None
    topic: str | None = None
    subtopic: str | None = None
    gradeBand: str | None = None
    ageGroup: str | None = None
    audience: str | None = None
    questionType: str
    mediaType: str
    taxonomyTags: list[str]
    fieldConfidences: dict[str, float]
    overallConfidence: float
    modelVersion: str
    warnings: list[str]


class QuestionTaxonomyBatchRequest(BaseModel):
    questions: list[QuestionTaxonomyRequest]


class QuestionTaxonomyBatchResponse(BaseModel):
    suggestions: list[QuestionTaxonomyResponse | None]
    received: int
    suggested: int
    failed: int


class ModelInfoResponse(BaseModel):
    question_difficulty: str
    question_taxonomy: str
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


@router.post("/question-taxonomy", response_model=QuestionTaxonomyResponse)
def suggest_question_taxonomy(req: QuestionTaxonomyRequest) -> QuestionTaxonomyResponse:
    return _suggest_taxonomy(req)


@router.post("/question-taxonomy/batch", response_model=QuestionTaxonomyBatchResponse)
def suggest_question_taxonomy_batch(req: QuestionTaxonomyBatchRequest) -> QuestionTaxonomyBatchResponse:
    if len(req.questions) > settings.question_taxonomy_batch_limit:
        raise HTTPException(
            status_code=413,
            detail=f"Batch size exceeds limit of {settings.question_taxonomy_batch_limit}",
        )

    suggestions: list[QuestionTaxonomyResponse | None] = []
    failed = 0
    for item in req.questions:
        try:
            suggestions.append(_suggest_taxonomy(item))
        except HTTPException:
            failed += 1
            suggestions.append(None)

    return QuestionTaxonomyBatchResponse(
        suggestions=suggestions,
        received=len(req.questions),
        suggested=sum(1 for s in suggestions if s is not None),
        failed=failed,
    )


@router.get("/model-info", response_model=ModelInfoResponse)
def model_info() -> ModelInfoResponse:
    return ModelInfoResponse(
        question_difficulty=_DIFFICULTY_VERSION,
        question_taxonomy=settings.question_taxonomy_model_version,
        churn_risk=_CHURN_VERSION,
        match_quality=_MATCH_QUALITY_VERSION,
    )
