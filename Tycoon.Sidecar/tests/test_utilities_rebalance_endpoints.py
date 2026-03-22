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


class _FakeElastic:
    def __init__(self):
        self.docs: list[dict] = []

    async def index(self, index: str, document: dict, refresh: bool):  # noqa: ARG002
        self.docs.append({"index": index, "document": document})
        return {"result": "created"}

    async def search(self, index: str, body: dict):  # noqa: ARG002
        docs = [d for d in self.docs if d["index"] == index]
        docs = sorted(docs, key=lambda d: d["document"].get("capturedAtUtc", ""), reverse=True)
        size = body.get("size", len(docs))
        hits = [{"_source": d["document"]} for d in docs[:size]]
        return {"hits": {"hits": hits}}


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
    app.state.elasticsearch = _FakeElastic()
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


def test_rebalance_alerts_endpoint_emits_blocked_rate_alert():
    client, _ = _make_client()

    for _ in range(5):
        client.post("/utilities/economy/rebalance/apply", json={"payload": {"maxEnergy": 21}})

    resp = client.get("/utilities/economy/rebalance/alerts")
    assert resp.status_code == 200
    body = resp.json()
    assert body["status"] == "ok"
    assert body["summary"]["totalApplyAttempts"] == 5
    alerts = body["alerts"]
    assert any(a["code"] == "REBALANCE_BLOCKED_RATE_HIGH" for a in alerts)


def test_publish_rebalance_metrics_persists_snapshot_to_external_sink():
    client, app = _make_client()
    client.post("/utilities/economy/rebalance/apply", json={
        "approved": True,
        "approvedBy": "operator-ok",
        "reason": "small update",
        "payload": {"maxEnergy": 21},
    })

    resp = client.post("/utilities/economy/rebalance/metrics/publish")
    assert resp.status_code == 200
    body = resp.json()
    assert body["status"] == "ok"
    assert body["index"] == "tycoon_rebalance_metrics"
    assert len(app.state.elasticsearch.docs) == 1
    assert app.state.elasticsearch.docs[0]["document"]["totalApplyAttempts"] == 1


def test_rebalance_metrics_history_reads_from_external_sink():
    client, app = _make_client()
    # seed two snapshots (newest should be first by capturedAtUtc)
    app.state.elasticsearch.docs.append({
        "index": "tycoon_rebalance_metrics",
        "document": {"capturedAtUtc": "2026-03-22T10:00:00Z", "totalApplyAttempts": 1},
    })
    app.state.elasticsearch.docs.append({
        "index": "tycoon_rebalance_metrics",
        "document": {"capturedAtUtc": "2026-03-22T11:00:00Z", "totalApplyAttempts": 2},
    })

    resp = client.get("/utilities/economy/rebalance/metrics/history?limit=10")
    assert resp.status_code == 200
    body = resp.json()
    assert body["status"] == "ok"
    assert body["count"] == 2
    assert body["items"][0]["totalApplyAttempts"] == 2
    assert body["items"][1]["totalApplyAttempts"] == 1
