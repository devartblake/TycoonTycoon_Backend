"""
ML / AI inference endpoints.

Routes:
  POST /ml/match-quality        — score a match for quality/fairness
  POST /ml/churn-risk           — predict churn probability for a player
  POST /ml/question-difficulty  — estimate difficulty of a trivia question
"""

from fastapi import APIRouter, HTTPException
from pydantic import BaseModel

router = APIRouter()


class MatchQualityRequest(BaseModel):
    player_ids: list[str]
    tier: int
    rank_points: list[int]


class MatchQualityResponse(BaseModel):
    quality_score: float          # 0.0 – 1.0; higher = more balanced
    balance_delta: float          # rank_points spread


class ChurnRiskRequest(BaseModel):
    player_id: str
    days_since_last_match: int
    win_rate_7d: float
    rank_points: int


class ChurnRiskResponse(BaseModel):
    player_id: str
    churn_probability: float      # 0.0 – 1.0
    risk_tier: str                # "low" | "medium" | "high"


class QuestionDifficultyRequest(BaseModel):
    question_text: str
    category: str
    options: list[str]


class QuestionDifficultyResponse(BaseModel):
    estimated_difficulty: float   # 0.0 (easy) – 1.0 (hard)
    confidence: float


@router.post("/match-quality", response_model=MatchQualityResponse)
def score_match_quality(req: MatchQualityRequest) -> MatchQualityResponse:
    if not req.rank_points:
        raise HTTPException(status_code=422, detail="rank_points cannot be empty")
    spread = max(req.rank_points) - min(req.rank_points)
    # Simple heuristic: quality decays as spread grows (replace with real model)
    quality = max(0.0, 1.0 - spread / 2000.0)
    return MatchQualityResponse(quality_score=round(quality, 3), balance_delta=spread)


@router.post("/churn-risk", response_model=ChurnRiskResponse)
def predict_churn(req: ChurnRiskRequest) -> ChurnRiskResponse:
    # Placeholder heuristic — swap out for scikit-learn / ONNX model
    score = min(1.0, req.days_since_last_match / 30.0 * (1.0 - req.win_rate_7d))
    tier = "high" if score > 0.7 else "medium" if score > 0.35 else "low"
    return ChurnRiskResponse(
        player_id=req.player_id,
        churn_probability=round(score, 3),
        risk_tier=tier,
    )


@router.post("/question-difficulty", response_model=QuestionDifficultyResponse)
def estimate_difficulty(req: QuestionDifficultyRequest) -> QuestionDifficultyResponse:
    # Placeholder — replace with NLP model (e.g. sentence-transformers)
    word_count = len(req.question_text.split())
    difficulty = min(1.0, word_count / 40.0)
    return QuestionDifficultyResponse(
        estimated_difficulty=round(difficulty, 3),
        confidence=0.5,
    )
