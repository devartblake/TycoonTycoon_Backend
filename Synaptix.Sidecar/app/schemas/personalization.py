from typing import Any, Dict, List, Optional
from pydantic import BaseModel, Field


class BehaviorEvent(BaseModel):
    eventType: str
    eventSource: str
    category: Optional[str] = None
    difficulty: Optional[str] = None
    mode: Optional[str] = None
    metadata: Dict[str, Any] = Field(default_factory=dict)


class PlayerProfileSnapshot(BaseModel):
    confidenceLevel: float = 0.5
    churnRiskScore: float = 0.0
    frustrationRiskScore: float = 0.0
    notificationFatigueScore: float = 0.0
    archetype: str = "new_player"


class ScorePlayerRequest(BaseModel):
    playerId: str
    recentEvents: List[BehaviorEvent] = Field(default_factory=list)
    currentProfile: PlayerProfileSnapshot


class ScorePlayerResponse(BaseModel):
    churnRiskScore: float
    frustrationRiskScore: float
    confidenceLevel: float
    recommendedArchetype: str
    categoryStrengths: Dict[str, float] = Field(default_factory=dict)
    categoryWeaknesses: Dict[str, float] = Field(default_factory=dict)
    signals: Dict[str, Any] = Field(default_factory=dict)


class RecommendationCandidate(BaseModel):
    type: str
    targetId: Optional[str] = None
    score: float
    reason: str
    payload: Dict[str, Any] = Field(default_factory=dict)


class RecommendationCandidateRequest(BaseModel):
    playerId: str
    profile: PlayerProfileSnapshot
    recentEvents: List[BehaviorEvent] = Field(default_factory=list)


class RecommendationCandidateResponse(BaseModel):
    candidates: List[RecommendationCandidate]


class CategoryProfileRequest(BaseModel):
    playerId: str
    recentEvents: List[BehaviorEvent] = Field(default_factory=list)


class CategoryProfileResponse(BaseModel):
    strengths: Dict[str, float] = Field(default_factory=dict)
    weaknesses: Dict[str, float] = Field(default_factory=dict)
    topCategory: Optional[str] = None
    weakestCategory: Optional[str] = None


class NotificationScoreRequest(BaseModel):
    playerId: str
    currentProfile: PlayerProfileSnapshot
    recentEvents: List[BehaviorEvent] = Field(default_factory=list)


class NotificationScoreResponse(BaseModel):
    notificationFatigueScore: float
    canReceiveNotification: bool
    recommendedFrequencyHours: int


class MissionFitRequest(BaseModel):
    playerId: str
    currentProfile: PlayerProfileSnapshot
    missionArchetype: str
    recentEvents: List[BehaviorEvent] = Field(default_factory=list)


class MissionFitResponse(BaseModel):
    fitScore: float
    reason: str
    recommended: bool
