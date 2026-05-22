# Rebalance Operations Runbook

## Purpose

This runbook defines the production process for reviewing, approving, applying, and monitoring economy rebalance changes produced by Sidecar recommendations.

It is intentionally opinionated to prevent unsafe live-tuning and to preserve an auditable trail of every decision.

---

## Scope

- Backend admin economy endpoints (`/admin/economy/*`)
- Sidecar rebalance endpoints (`/utilities/economy/rebalance/*`)
- Operator Dashboard Economy page Sidecar panels (audit/recommendation/metrics/alerts)

---

## Required Roles

- **Rebalance Approver (on-call operator)**: evaluates recommendation and approves/blocks.
- **Secondary Reviewer**: validates change rationale for medium/high-risk adjustments.
- **Incident Commander**: owns rollback decision if production impact is detected.

---

## Preconditions (Before Any Apply)

1. Confirm Sidecar recommendation is current (reload recommendation panel).
2. Confirm no active `high` severity rebalance alerts, or document why proceeding.
3. Validate latest audit history has no unexplained recent `error` applies.
4. Ensure desired patch is within guardrails:
   - `maxEnergy` delta <= ±2
   - per-mode `energyCost` delta <= ±1
5. Capture change ticket/incident reference ID.

---

## Standard Apply Procedure

1. Prepare payload for `/utilities/economy/rebalance/apply`.
2. Set:
   - `approved=true`
   - `approvedBy=<operator-id>`
   - `reason=<ticket-id + concise rationale>`
3. Submit apply request.
4. Verify response:
   - `status=ok`
   - `backend_status` is 2xx
   - `auditId` present
5. Validate post-apply state:
   - `GET /admin/economy/balance` reflects expected values.
   - Sidecar audit endpoint includes the new record.

---

## Guardrail Block Procedure

If apply returns `status=blocked`:

1. Record `auditId` and violation details.
2. Do **not** bypass guardrails ad-hoc.
3. If change is still needed, split into staged increments that satisfy guardrails.
4. Re-submit staged apply with separate rationale entries.

---

## Error Apply Procedure

If apply returns `status=error`:

1. Record `auditId` (if provided), `backend_status`, and response body.
2. Check Sidecar rebalance metrics and alerts for elevated error rate.
3. Validate backend health/readiness and schema state.
4. Retry only once after confirming transient cause.
5. If second attempt fails, escalate to Incident Commander and pause rebalance operations.

---

## Rollback Procedure

If a live rebalance causes regression:

1. Identify affected economy event ID(s).
2. Execute backend rollback endpoint (`POST /admin/economy/rollback`).
3. Verify reversal transaction linkage and resulting balances.
4. Annotate incident timeline with rollback confirmation.
5. Add a postmortem note and follow-up ticket for root-cause correction.

---

## Monitoring Procedure

At minimum every 15 minutes during active tuning:

1. Review Sidecar metrics counters:
   - total attempts
   - blocked count
   - success count
   - error count
2. Review Sidecar alerts endpoint for threshold breaches.
3. Confirm external metrics sink ingestion (Elasticsearch index documents increasing).
4. Confirm no sustained growth in error-rate alerts.

---

## Escalation Matrix

- **Medium**: blocked-rate alert only and no player-impact signals.
  - Action: continue with staged changes + reviewer signoff.
- **High**: error-rate alert threshold reached.
  - Action: freeze applies, investigate backend/infra health, notify Incident Commander.
- **Critical**: player-impact confirmed (entry failures/economy corruption).
  - Action: immediate rollback + incident response protocol.

---

## Change Record Template

Use this template in incident/change tracking:

- Timestamp (UTC):
- Operator:
- Reviewer:
- Ticket/Incident ID:
- Proposed change:
- Reason:
- Apply response status/backend status:
- Audit ID:
- Post-apply validation result:
- Follow-up actions:

