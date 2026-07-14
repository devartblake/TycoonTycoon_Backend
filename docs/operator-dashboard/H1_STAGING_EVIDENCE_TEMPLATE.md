# H1 Staging Evidence Template (React-primary)

**Program:** Track B / H1 + R4 — [`docs/status/BCE_EXECUTION_PLAN.md`](../status/BCE_EXECUTION_PLAN.md)  
**Parallel-run:** [`REACT_STAGING_PARALLEL_RUN.md`](REACT_STAGING_PARALLEL_RUN.md)  
**Direction:** React replaces Django — [`OPERATOR_DASHBOARD_DIRECTION_2026-07-13.md`](OPERATOR_DASHBOARD_DIRECTION_2026-07-13.md)  
**Process heritage:** May cutover guide still useful for migrations — [`OPERATOR_DASHBOARD_MAY_CUTOVER_COMPLETION_GUIDE.md`](OPERATOR_DASHBOARD_MAY_CUTOVER_COMPLETION_GUIDE.md)

Repo automation **cannot invent** live staging logs. Paste links here when gates pass.

---

## Staging identifiers

| Field | Value |
|-------|-------|
| Environment | staging |
| API URL | |
| **React primary URL** | |
| Django fallback URL (if any) | |
| Git SHA / image tags | |
| Compose revision | |
| Date | |

---

## How to collect (automation)

```bash
# Read-only probes (React primary)
python scripts/operator-cutover-readiness.py \
  --environment staging \
  --dashboard-ui react \
  --api-url "<API>" \
  --dashboard-url "<REACT>" \
  --fallback-dashboard-url "<DJANGO optional>" \
  --operator-email "<ops@…>"

# GitHub Actions: workflow_dispatch operator-cutover-readiness.yml
# release-gate.yml with api_url = staging API
```

Set gate env vars when recording status (`GATE_*` — see workflow inputs).

---

## Evidence ledger

| Gate | Status (`pending` / `pass` / `waived`) | Evidence link | Owner | Date |
|------|----------------------------------------|---------------|-------|------|
| EF migrations applied (staging) | pending | | | |
| Strict MigrationService readiness | pending | | | |
| **React primary URL live** | pending | | | |
| **Parallel-run matrix on React** | pending | | | |
| Sign-off (QA / Backend / On-call) | pending | | | |
| Cutover executed (admin host → React) | pending | | | |
| Django rollback window | pending | | | |

---

## Parallel-run summary

Copy results from [`REACT_STAGING_PARALLEL_RUN.md`](REACT_STAGING_PARALLEL_RUN.md) matrix:

| Workflow area | Result | Notes |
|---------------|--------|-------|
| Auth + dashboard | | |
| Users + moderation + audit | | |
| Notifications / store / economy / questions | | |
| Storage + personalization | | |
| Installer/diagnostics unavailable (flags off) | | |

---

## Sign-off

| Role | Name | Date | Y/N | Notes |
|------|------|------|-----|-------|
| QA Lead | | | | |
| Backend Lead | | | | |
| On-call | | | | |
| Operator #2 | | | | |

---

## Waivers

| Gate | Reason | Approver | Revisit |
|------|--------|----------|---------|
| | | | |

## Notes

-
