# React Operator Dashboard — Staging Parallel-Run (R4)

**Program:** Wave 1.5 / R4 — [`docs/status/BCE_EXECUTION_PLAN.md`](../status/BCE_EXECUTION_PLAN.md)  
**Direction:** React is primary — [`OPERATOR_DASHBOARD_DIRECTION_2026-07-13.md`](OPERATOR_DASHBOARD_DIRECTION_2026-07-13.md)  
**Evidence ledger:** [`H1_STAGING_EVIDENCE_TEMPLATE.md`](H1_STAGING_EVIDENCE_TEMPLATE.md)  
**API contract notes:** [`REACT_DJANGO_PARITY_AND_R1.md`](REACT_DJANGO_PARITY_AND_R1.md)

This runbook **replaces** Django-vs-Blazor parallel-run as the operator cutover vehicle.  
Django may run as **fallback only** during the rollback window.

---

## Objective

Prove that **real operators** can complete critical workflows on **React** against staging `Synaptix.Backend.Api`, with evidence that can pass H1 gates.

---

## Environments & URLs

| Surface | Typical staging URL | Notes |
|---------|---------------------|--------|
| Backend API | `https://api.<DOMAIN>` | Health + `/admin/*` |
| **React (primary)** | `https://admin.<DOMAIN>` *or* `https://admin-react.<DOMAIN>` / `:8300` until cutover swap | SPA; `/admin` proxied with ops key |
| Django (fallback) | `https://admin-legacy.<DOMAIN>` or current `admin.` until swap | No new feature work |

Record the exact URLs used in the evidence pack.

### Feature flags (must stay off unless explicitly testing)

- Installer / Setup UI: `VITE_ENABLE_INSTALLER` default **false**  
- Diagnostics probes: `VITE_ENABLE_DIAGNOSTICS` default **false**  

Deep links should show **unavailable** pages, not 404 spam against missing APIs.

---

## Pre-flight (automation)

```bash
# From repo root — React-primary readiness (read-only probes)
python scripts/operator-cutover-readiness.py \
  --environment staging \
  --dashboard-ui react \
  --api-url "https://api.EXAMPLE" \
  --dashboard-url "https://admin-react.EXAMPLE" \
  --fallback-dashboard-url "https://admin.EXAMPLE" \
  --operator-email "ops@EXAMPLE"

# Or GitHub Actions: operator-cutover-readiness.yml
#  - dashboard_ui: react
#  - dashboard_url: React base URL
#  - fallback_dashboard_url: Django (optional)
```

Also run:

```text
release-gate.yml  → api_url = staging API
```

---

## Workflow matrix (operators execute on **React**)

Use **pass / fail / blocked / N/A**. Compare to Django only if fallback is still online.

| # | Workflow | React | Django fallback (optional) | Notes / evidence link |
|---|----------|:-----:|:--------------------------:|------------------------|
| 1 | Login / logout / refresh | | | `/auth/login`, session survives reload |
| 2 | Dashboard home loads | | | No unhandled error |
| 3 | Users list + search (`q`) | | | `/users` |
| 4 | User detail + ban / unban | | | Confirm reason required on ban |
| 5 | Moderation logs | | | `/moderation/logs` |
| 6 | Moderation player profile + set-status actions | | | ban/suspend/warn map to set-status |
| 7 | Security audit list + event detail | | | `/audit/security` |
| 8 | Notifications hub (templates/channels) | | | |
| 9 | Store catalog + flash sales list | | | flash uses `showAll` API |
| 10 | Economy player search + history | | | |
| 11 | Questions queue review | | | approve/reject |
| 12 | Storage browser (prefixes + list) | | | upload optional |
| 13 | Personalization overview/rules | | | |
| 14 | Setup deep link → unavailable | | | `/settings/setup` |
| 15 | Diagnostics deep link → unavailable | | | `/diagnostics` |
| 16 | Sidebar: no Setup / Diagnostics | | | |

### Out of scope for this parallel-run (known gaps)

Escalations, powerups, email ACL, full installer, probe diagnostics — track as follow-up, not P0 blockers unless product requires them for Alpha.

---

## Operator sign-off

| Role | Name | Date | Approved (Y/N) | Notes |
|------|------|------|----------------|-------|
| QA Lead | | | | |
| Backend Lead | | | | |
| On-call Operator | | | | |
| Second operator | | | | |

**Minimum:** 2 operator approvals + QA + Backend.

---

## Evidence pack layout

Store under `artifacts/operator-cutover/` (gitignored runtime) or attach to the ticket:

```text
artifacts/operator-cutover/
  operator-cutover-readiness.json   # from readiness script
  operator-cutover-readiness.md
  parallel-run-matrix.md            # copy of matrix above with results
  sign-off.md
  screenshots/                      # optional
  RELEASE_NOTES.txt                 # image tags, compose SHA, React URL
```

Paste links into [`H1_STAGING_EVIDENCE_TEMPLATE.md`](H1_STAGING_EVIDENCE_TEMPLATE.md).

---

## Exit criteria (R4 repo vs live)

| Criterion | Repo | Live |
|-----------|------|------|
| Parallel-run matrix exists (React-primary) | **Yes** (this file) | Fill results |
| Readiness script probes React SPA + admin API | **Yes** | Run against staging |
| H1 ledger has React URL gate | **Yes** | Mark pass/waive |
| Operators signed off on React | Template only | Required for H1 pass |
| Traefik/host points `admin.` to React | Documented | Deploy change |

---

## Cutover sequence (ops)

1. Deploy React with ops-key proxy to staging (`operator-dashboard-react`).  
2. Complete parallel-run matrix on React URL.  
3. Sign-off.  
4. Point `admin.${DOMAIN}` Traefik router at React service (port 8300); move Django to `admin-legacy` or internal-only.  
5. Keep Django warm for rollback window; then R5 decommission.

---

## Related automation

| Asset | Role |
|-------|------|
| `scripts/operator-cutover-readiness.py` | Read-only probes; `--dashboard-ui react` |
| `.github/workflows/operator-cutover-readiness.yml` | Manual dispatch |
| `e2e/operator-r1-r2-smoke.spec.ts` | Local mock smoke |
| `src/lib/admin-api-paths.smoke.spec.ts` | Path contract unit smoke |
