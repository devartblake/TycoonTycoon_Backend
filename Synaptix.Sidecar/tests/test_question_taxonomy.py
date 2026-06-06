from fastapi import FastAPI
from fastapi.testclient import TestClient

from app.routers import ml, utilities


def _ml_client():
    app = FastAPI()
    app.include_router(ml.router, prefix="/ml")
    return TestClient(app)


def test_question_taxonomy_maps_known_aliases_and_class_dataset():
    client = _ml_client()

    resp = client.post("/ml/question-taxonomy", json={
        "text": "What force keeps planets in orbit?",
        "category": "natural_science",
        "sourceDataset": "assets/questions/classes/class_6_questions.json",
        "tags": ["physics"],
        "options": ["Gravity", "Evaporation"],
    })

    assert resp.status_code == 200
    body = resp.json()
    assert body["canonicalCategory"] == "science"
    assert body["subject"] == "stem"
    assert body["gradeBand"] == "middle_school"
    assert body["audience"] == "teen"
    assert body["overallConfidence"] >= 0.7


def test_question_taxonomy_maps_math_and_kids_grade_aliases():
    client = _ml_client()

    math_resp = client.post("/ml/question-taxonomy", json={
        "text": "What is the sum of 4 and 5?",
        "category": "math",
        "options": ["9", "10"],
    })
    kids_resp = client.post("/ml/question-taxonomy", json={
        "text": "Which animal says moo?",
        "category": "kidsGrade2",
        "sourceDataset": "assets/questions/classes/class_2_questions.json",
        "options": ["Cow", "Duck"],
    })

    assert math_resp.status_code == 200
    assert math_resp.json()["canonicalCategory"] == "mathematics"
    assert kids_resp.status_code == 200
    assert kids_resp.json()["canonicalCategory"] == "kids"
    assert kids_resp.json()["gradeBand"] == "k_2"


def test_question_taxonomy_ambiguous_input_returns_low_confidence_warning():
    client = _ml_client()

    resp = client.post("/ml/question-taxonomy", json={
        "text": "Choose the best answer.",
        "options": ["A", "B"],
    })

    assert resp.status_code == 200
    body = resp.json()
    assert body["canonicalCategory"] == "general"
    assert body["overallConfidence"] < 0.65
    assert body["warnings"]


def test_question_taxonomy_batch_enforces_limit(monkeypatch):
    client = _ml_client()
    monkeypatch.setattr(ml.settings, "question_taxonomy_batch_limit", 1)

    resp = client.post("/ml/question-taxonomy/batch", json={
        "questions": [
            {"text": "What is 2+2?"},
            {"text": "What is gravity?"},
        ]
    })

    assert resp.status_code == 413


class _FakeResponse:
    def __init__(self, status_code: int, payload: dict):
        self.status_code = status_code
        self._payload = payload
        self.text = str(payload)

    def json(self):
        return self._payload


class _FakeBackend:
    def __init__(self):
        self.posts: list[dict] = []

    async def post(self, path: str, json: dict):
        self.posts.append({"path": path, "json": json})
        return _FakeResponse(200, {"received": len(json["questions"]), "created": 1, "failed": 0})


def test_utilities_question_import_forwards_to_taxonomy_import(tmp_path):
    app = FastAPI()
    app.include_router(utilities.router, prefix="/utilities")
    app.state.backend = _FakeBackend()
    client = TestClient(app)
    file_path = tmp_path / "questions.json"
    file_path.write_text('[{"id":"q1","text":"What is gravity?","options":[{"id":"A","text":"Force","isCorrect":true},{"id":"B","text":"Color"}]}]')

    with file_path.open("rb") as handle:
        resp = client.post("/utilities/questions/import", files={"file": ("questions.json", handle, "application/json")})

    assert resp.status_code == 200
    assert resp.json()["status"] == "ok"
    assert app.state.backend.posts[0]["path"] == "/admin/questions/import-taxonomy"
    assert app.state.backend.posts[0]["json"]["enrichWithSidecar"] is True
