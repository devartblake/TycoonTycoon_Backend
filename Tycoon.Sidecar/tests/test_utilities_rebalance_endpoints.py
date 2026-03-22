from fastapi import FastAPI
from fastapi.testclient import TestClient

from app.routers import utilities


class _FakeResponse:
    def __init__(self, status_code: int, payload: dict):
        self.status_code = status_code
        self._payload = payload
        self.content = b"1"

    def json(self):
        return self._payload


class _FakeBackend:
    def __init__(self):
        self.patch_payloads: list[dict] = []

    async def get(self, path: str, **kwargs):  # noqa: ARG002
        if path == "/admin/economy/balance":
            return _FakeResponse(200, {
                "maxEnergy": 20,
                "regenMinutesPerEnergy": 10,
                "modes": [{"mode": "casual", "energyCost": 3}],
            })
        return _FakeResponse(200, {})

    async def patch(self, path: str, json: dict):
        self.patch_payloads.append({"path": path, "json": json})
        return _FakeResponse(200, {"updated": True})


class _FakeCursor:
    def __init__(self, items):
        self._items = items
        self._limit = len(items)

    def sort(self, *_args, **_kwargs):
        return self

    def limit(self, limit):
        self._limit = limit
        return self

    async def to_list(self, length):
        return self._items[: min(length, self._limit)]


class _FakeCollection:
    def __init__(self):
        self.docs: list[dict] = []

    async def insert_one(self, doc: dict):
        self.docs.append(doc)
        return {"ok": 1}

    def find(self, *_args, **_kwargs):
        return _FakeCursor(list(reversed(self.docs)))


class _FakeMongoDb:
    def __init__(self):
        self.economy_rebalance_audit = _FakeCollection()


def _make_client():
    app = FastAPI()
    app.include_router(utilities.router, prefix="/utilities")
    app.state.backend = _FakeBackend()
    app.state.mongo_db = _FakeMongoDb()
    return TestClient(app), app


def test_apply_rebalance_rejects_non_object_body():
    client, _ = _make_client()

    resp = client.post("/utilities/economy/rebalance/apply", json=["not-an-object"])

    assert resp.status_code == 200
    body = resp.json()
    assert body["status"] == "blocked"
    assert body["detail"] == "Body must be a JSON object."


def test_apply_rebalance_guardrail_violation_writes_audit_record():
    client, app = _make_client()

    resp = client.post("/utilities/economy/rebalance/apply", json={
        "approved": True,
        "approvedBy": "operator-1",
        "reason": "rebalance attempt",
        "payload": {"maxEnergy": 30},
    })

    assert resp.status_code == 200
    body = resp.json()
    assert body["status"] == "blocked"
    assert body["detail"] == "Guardrail violation"
    assert "auditId" in body
    assert len(app.state.mongo_db.economy_rebalance_audit.docs) == 1
    assert app.state.mongo_db.economy_rebalance_audit.docs[0]["status"] == "blocked"
    assert app.state.mongo_db.economy_rebalance_audit.docs[0]["approvedBy"] == "operator-1"


def test_apply_rebalance_success_writes_audit_and_patches_backend():
    client, app = _make_client()

    resp = client.post("/utilities/economy/rebalance/apply", json={
        "approved": True,
        "approvedBy": "operator-2",
        "reason": "small tuning",
        "payload": {"maxEnergy": 21},
    })

    assert resp.status_code == 200
    body = resp.json()
    assert body["status"] == "ok"
    assert body["backend_status"] == 200
    assert "auditId" in body
    assert len(app.state.backend.patch_payloads) == 1
    assert app.state.backend.patch_payloads[0]["path"] == "/admin/economy/balance"
    assert app.state.mongo_db.economy_rebalance_audit.docs[0]["status"] == "ok"
