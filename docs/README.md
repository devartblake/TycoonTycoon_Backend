# Synaptix Backend — Documentation Map

Start here. Prefer this index over root-level phase markdown when navigating the repo.

**Program tracker (B+C+E):** [`status/BCE_EXECUTION_PLAN.md`](status/BCE_EXECUTION_PLAN.md)

---

## Quick start

| Need | Go to |
|------|--------|
| Local stack (cold clone → healthy API) | [`setup/LOCAL_DEV_HAPPY_PATH.md`](setup/LOCAL_DEV_HAPPY_PATH.md) |
| Dev secrets hygiene | [`dev-secrets.md`](dev-secrets.md) |
| What is still open (alpha backlog) | [`alpha-beta/REMAINING_TASKS.md`](alpha-beta/REMAINING_TASKS.md) |
| Operator dashboard cutover gates | [`operator-dashboard/OPERATOR_DASHBOARD_MAY_CUTOVER_COMPLETION_GUIDE.md`](operator-dashboard/OPERATOR_DASHBOARD_MAY_CUTOVER_COMPLETION_GUIDE.md) |
| React admin ↔ API route gaps | [`operator-dashboard/REACT_ADMIN_ROUTE_GAP_INVENTORY.md`](operator-dashboard/REACT_ADMIN_ROUTE_GAP_INVENTORY.md) |
| React vs Django parity + R1/R2/R3 | [`operator-dashboard/REACT_DJANGO_PARITY_AND_R1.md`](operator-dashboard/REACT_DJANGO_PARITY_AND_R1.md) |
| React staging parallel-run (R4) | [`operator-dashboard/REACT_STAGING_PARALLEL_RUN.md`](operator-dashboard/REACT_STAGING_PARALLEL_RUN.md) |
| Tycoon→Synaptix rename Wave 1 | [`status/TYCOON_RENAME_WAVE1.md`](status/TYCOON_RENAME_WAVE1.md) |
| Tycoon→Synaptix rename Waves 2–3 | [`status/TYCOON_RENAME_WAVE2_3.md`](status/TYCOON_RENAME_WAVE2_3.md) |
| KMS / secure channel run guide | [`security/SYNAPTIX_SECURITY_RUNNING_GUIDE.md`](security/SYNAPTIX_SECURITY_RUNNING_GUIDE.md) |
| Alpha launch alerts | [`../ops/runbooks/alpha-launch-alerts.md`](../ops/runbooks/alpha-launch-alerts.md) |

Public product README: repo root [`README.md`](../README.md) (intentionally non-ops).

---

## By topic

### Alpha / Beta / release
- [`alpha-beta/`](alpha-beta/) — release plans, remaining tasks, soft-launch notes  
- [`releases/`](releases/) — release packages and notes  
- [`staging/`](staging/) — staging deployment runbooks  
- [`PRE_LAUNCH_CHECKLIST.md`](PRE_LAUNCH_CHECKLIST.md)  
- [`PRODUCTION_READINESS_SUMMARY.md`](PRODUCTION_READINESS_SUMMARY.md) — **feature-scoped** readiness (quiz/leaderboard); not whole-platform greenlight  

### Architecture & backend
- [`architecture/`](architecture/)  
- [`backend/`](backend/)  
- [`realtime/`](realtime/)  
- [`migrations/`](migrations/)  
- Root: [`BACKEND_INTEGRATION.md`](../BACKEND_INTEGRATION.md), [`DEPLOYMENT.md`](../DEPLOYMENT.md), [`Docker.md`](../Docker.md)  

### Security & compliance
- [`security/`](security/) — KMS handoff, architecture, staff guides, compliance status  
- [`audits/`](audits/) — audit reports and implementation plans  
- KMS hybrid PQ flag: `Kms:Suites:EnableHybridPq` (default **false** until crypto review)  

### Operator dashboard
- **Canonical UI: React** — [`operator-dashboard/OPERATOR_DASHBOARD_DIRECTION_2026-07-13.md`](operator-dashboard/OPERATOR_DASHBOARD_DIRECTION_2026-07-13.md)  
- Route gap inventory (P0/P1): [`operator-dashboard/REACT_ADMIN_ROUTE_GAP_INVENTORY.md`](operator-dashboard/REACT_ADMIN_ROUTE_GAP_INVENTORY.md)  
- Cutover / evidence runbooks: [`operator-dashboard/`](operator-dashboard/) (process still applies; **target UI is React**, Django is legacy)  
- React status: [`REACT_DASHBOARD_STATUS.md`](../REACT_DASHBOARD_STATUS.md), [`SETUP-SUMMARY.md`](../SETUP-SUMMARY.md)  

### Observability & ops
- [`observability/`](observability/)  
- [`../ops/runbooks/`](../ops/runbooks/)  
- [`../ops/dashboards/`](../ops/dashboards/)  
- Root: monitoring guides (`MONITORING_*.md`, `SENTRY_*.md`)  

### Store, personalization, game
- [`store/`](store/)  
- [`personalization/`](personalization/)  
- [`game-balance/`](game-balance/)  
- [`analytics/`](analytics/)  

### Setup / bootstrap
- [`setup/`](setup/) — Setup CLI architecture and local happy path  
- [`../Synaptix.Setup/`](../Synaptix.Setup/) — `init-local`, provision commands  

### Status & handoffs
- [`status/`](status/) — project status snapshots and **this program’s execution plan**  
- [`handoffs/`](handoffs/)  

### Auth / infrastructure
- [`auth/`](auth/)  
- [`infrastructure/`](infrastructure/)  

---

## CI entry points

| Workflow | Purpose |
|----------|---------|
| `.github/workflows/dotnet-ci.yml` | Build, API tests, security contracts, EF schema, migration artifacts |
| `.github/workflows/release-gate.yml` | Manual release smoke + migration artifact check |
| `.github/workflows/operator-cutover-readiness.yml` | Staging/prod readiness probes |
| `.github/workflows/react-dashboard-ci.yml` | React unit + non-blocking Playwright (#439) |

---

## Conventions

1. **Prefer dated status under `docs/status/`** over scattering new “COMPLETE” files at repo root.  
2. **Operational gates need evidence links** — CI green ≠ staging cutover complete.  
3. **Django Operator Dashboard** is canonical for admin/ops; React is metrics/ops UI; Blazor is rollback only after cutover.  
4. After B+C+E exit criteria in [`status/BCE_EXECUTION_PLAN.md`](status/BCE_EXECUTION_PLAN.md), run the **Security hardening sprint** (auth fallback, presence binding, secrets proof, economy review).
