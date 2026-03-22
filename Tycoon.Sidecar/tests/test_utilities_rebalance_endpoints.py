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
        self.balance_status_code = 200
        self.patch_status_code = 200

    async def get(self, path: str, **kwargs):  # noqa: ARG002
        if path == "/admin/economy/balance":
            return _FakeResponse(self.balance_status_code, {
                "maxEnergy": 20,
                "regenMinutesPerEnergy": 10,
                "modes": [{"mode": "casual", "energyCost": 3}],
            })
        return _FakeResponse(200, {})

    async def patch(self, path: str, json: dict):
        self.patch_payloads.append({"path": path, "json": json})
        return _FakeResponse(self.patch_status_code, {"updated": self.patch_status_code < 300})


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
    utilities._rebalance_metrics.update({  # noqa: SLF001
        "totalApplyAttempts": 0,
        "blockedCount": 0,
        "successCount": 0,
        "errorCount": 0,
        "lastAttemptAtUtc": None,
        "lastSuccessAtUtc": None,
        "lastErrorAtUtc": None,
    })
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


def test_apply_rebalance_returns_error_when_balance_fetch_fails():
    client, app = _make_client()
    app.state.backend.balance_status_code = 503

    resp = client.post("/utilities/economy/rebalance/apply", json={
        "approved": True,
        "approvedBy": "operator-3",
        "reason": "attempt while backend down",
        "payload": {"maxEnergy": 21},
    })

    assert resp.status_code == 200
    body = resp.json()
    assert body["status"] == "error"
    assert body["backend_status"] == 503
    assert len(app.state.mongo_db.economy_rebalance_audit.docs) == 0


def test_apply_rebalance_patch_failure_persists_error_audit():
    client, app = _make_client()
    app.state.backend.patch_status_code = 500

    resp = client.post("/utilities/economy/rebalance/apply", json={
        "approved": True,
        "approvedBy": "operator-4",
        "reason": "test backend patch failure",
        "payload": {"maxEnergy": 21},
    })

    assert resp.status_code == 200
    body = resp.json()
    assert body["status"] == "error"
    assert body["backend_status"] == 500
    assert "auditId" in body
    assert len(app.state.mongo_db.economy_rebalance_audit.docs) == 1
    assert app.state.mongo_db.economy_rebalance_audit.docs[0]["status"] == "error"


def test_get_rebalance_audit_history_respects_limit_and_order():
    client, app = _make_client()

    for i in range(4):
        app.state.mongo_db.economy_rebalance_audit.docs.append({
            "auditId": f"a-{i}",
            "status": "ok",
            "createdAtUtc": f"2026-03-2{i}T00:00:00Z",
        })

    resp = client.get("/utilities/economy/rebalance/audit?limit=2")

    assert resp.status_code == 200
    body = resp.json()
    assert body["status"] == "ok"
    assert body["count"] == 2
    assert [x["auditId"] for x in body["items"]] == ["a-3", "a-2"]


def test_rebalance_metrics_reflect_apply_attempt_outcomes():
    client, app = _make_client()

    # Blocked (no approved flag)
    client.post("/utilities/economy/rebalance/apply", json={"payload": {"maxEnergy": 21}})
    # Successful apply
    client.post("/utilities/economy/rebalance/apply", json={
        "approved": True,
        "approvedBy": "operator-ok",
        "reason": "small update",
        "payload": {"maxEnergy": 21},
    })
    # Error apply (backend patch failure)
    app.state.backend.patch_status_code = 500
    client.post("/utilities/economy/rebalance/apply", json={
        "approved": True,
        "approvedBy": "operator-error",
        "reason": "force patch failure",
        "payload": {"maxEnergy": 21},
    })

    metrics_resp = client.get("/utilities/economy/rebalance/metrics")
    assert metrics_resp.status_code == 200
    metrics_body = metrics_resp.json()
    assert metrics_body["status"] == "ok"
    metrics = metrics_body["metrics"]
    assert metrics["totalApplyAttempts"] == 3
    assert metrics["blockedCount"] == 1
    assert metrics["successCount"] == 1
    assert metrics["errorCount"] == 1
    assert metrics["lastAttemptAtUtc"] is not None
    assert metrics["lastSuccessAtUtc"] is not None
    assert metrics["lastErrorAtUtc"] is not None


def test_rebalance_metrics_prometheus_endpoint_exposes_counters():
    client, _ = _make_client()
    client.post("/utilities/economy/rebalance/apply", json={
        "approved": True,
        "approvedBy": "operator-ok",
        "reason": "small update",
        "payload": {"maxEnergy": 21},
    })

    resp = client.get("/utilities/economy/rebalance/metrics/prometheus")
    assert resp.status_code == 200
    assert resp.headers["content-type"].startswith("text/plain")
    body = resp.text
    assert "tycoon_rebalance_apply_attempts_total 1" in body
    assert "tycoon_rebalance_apply_success_total 1" in body
    assert "tycoon_rebalance_apply_error_total 0" in body
