# B + C + E Execution Plan

**Decision:** Run **B (Launch ops)** + **C (Contract integrity)** + **E (Docs/DevEx)** first.  
**Deferred:** **A (Security hardening sprint)** — starts only after B+C+E exit criteria are met.

**Operator dashboard (2026-07-13):** **React replaces Django** as canonical ops UI —  
[`../operator-dashboard/OPERATOR_DASHBOARD_DIRECTION_2026-07-13.md`](../operator-dashboard/OPERATOR_DASHBOARD_DIRECTION_2026-07-13.md).  
React `/admin/*` contract gaps are now **P0/P1**, not optional polish.

**Started:** 2026-07-13  
**Owner trackers:** Update the Status column as work lands; keep exit criteria honest.

---

## Portfolio scope

| Track | Theme | Plan IDs | Goal |
|-------|--------|----------|------|
| **B** | Launch ops | H1, H4, H6 | Staging/prod operable; CI trustworthy; alerts exist |
| **C** | Contract integrity | C5, H3 | Client ↔ server (and micro) path drift fails CI |
| **E** | Docs / DevEx | M2, L3 | One docs map + one local happy path |

Security (C1–C4) is **out of scope** until B+C+E exit.

---

## Exit criteria (when we may start Security hardening)

### Track B — Launch ops
- [ ] **H1** Staging cutover ledger has live evidence **or** an explicit “gates still open / deferred to date X” note signed by ops owner (repo cannot invent staging logs). Template: [`H1_STAGING_EVIDENCE_TEMPLATE.md`](../operator-dashboard/H1_STAGING_EVIDENCE_TEMPLATE.md).
- [x] **H4** Critical path tests are **blocking** in CI: API unit suite, route-parity, KMS suite, EF schema validation, NuGet High/Critical vuln scan. Flaky E2E remains non-blocking (#439).
- [x] **H6** Alpha launch alert set documented + Prometheus `alpha-launch` rules; who-gets-paged table filled (staging validation still recommended).

### Track C — Contracts
- [x] **H3** `RouteParityContractTests` covers core mobile + security + friends surfaces; React admin gap inventory published.
- [x] **C5** Compliance **internal client** paths covered by contract tests.
- [x] CI runs those contract tests on every PR to `main` (plus release-gate secure-session route check).

### Track E — Docs/DevEx
- [x] **M2** `docs/README.md` is the navigation index (status, alpha, security, ops, setup).
- [x] **L3** `docs/setup/LOCAL_DEV_HAPPY_PATH.md` + `make dev` / `make dev-win` happy path.

---

## Work breakdown & status

### Wave 0 — Foundation (repo-only, start immediately)

| ID | Task | Track | Status | Notes |
|----|------|-------|--------|-------|
| E0 | `docs/README.md` docs map | E | **Done** | Navigation only; no content rewrite |
| E1 | `docs/setup/LOCAL_DEV_HAPPY_PATH.md` | E | **Done** | Setup CLI + compose + health |
| B0 | Document H1 open gates pointer | B | **Done** | Points at May cutover guide; no fake pass |
| B1 | Expand CI: RouteParity + KMS tests blocking | B | **Done** | `dotnet-ci.yml` |
| B2 | Alpha launch alerts runbook (H6 minimum) | B | **Done** | `ops/runbooks/alpha-launch-alerts.md` |
| C0 | Expand mobile route-parity catalog | C | **Done** | Friends + core social paths |
| C1 | Compliance client ↔ API path contract | C | **Done** | Static catalog + filter tests project |

### Wave 1 — Deepen

| ID | Task | Track | Status | Notes |
|----|------|-------|--------|-------|
| H1a | Attach staging migration evidence | B | **Template ready; live pending** | [`H1_STAGING_EVIDENCE_TEMPLATE.md`](../operator-dashboard/H1_STAGING_EVIDENCE_TEMPLATE.md) |
| H1b | Staging parallel-run matrix | B | **Pending humans** | Real operator accounts |
| H3b | React dashboard route inventory vs backend | C | **Done** | [`REACT_ADMIN_ROUTE_GAP_INVENTORY.md`](../operator-dashboard/REACT_ADMIN_ROUTE_GAP_INVENTORY.md); ~78 React-only paths |
| H3c | KMS session paths in release-gate | C | **Done** | `release-gate.yml` POST route existence checks |
| H4b | Dependency vulnerability scan in CI | B | **Done** | `dependency-vulnerability-scan` job (fail on High/Critical) |
| H6b | Prometheus alert rules | B | **Done** | `alpha-launch` group in `alert-rules.yml` |
| E2 | Tag stale “production ready” summaries | E | **Done** | Banner on `PRODUCTION_READINESS_SUMMARY.md` |
| E3 | `make dev` happy path | E | **Done** | `make dev` / `make dev-win` → bootstrap-local |

### Wave 1.5 — React replaces Django (contract alignment)

| ID | Task | Status | Notes |
|----|------|--------|-------|
| R0 | Direction recorded: React canonical | **Done** | `OPERATOR_DASHBOARD_DIRECTION_2026-07-13.md` |
| R1 | P0: Align auth + users + moderation + audit client paths | **Done** | See `REACT_DJANGO_PARITY_AND_R1.md`; moderation set-status + users page/q fixed |
| R2 | P1: notifications, store, questions, economy, storage, personalization | **Done** | Django paths; see `REACT_DJANGO_PARITY_AND_R1.md` §5 |
| R3 | Feature-flag / hide installer + diagnostics until APIs exist | **Done** | Default off; unavailable pages; e2e + path smoke |
| R4 | H1 evidence retargeted to React primary URL | **Done (repo)** | `REACT_STAGING_PARALLEL_RUN.md`; readiness script React-primary; live evidence still human |
| R5 | Django decommission plan after rollback window | Pending | Compose + docs |

### Wave 2 — Closeout

| ID | Task | Status |
|----|------|--------|
| X1 | Review exit checklist with owner | **H1 live evidence + React P0 contracts** |
| X2 | Open Security hardening sprint (A): C1–C4 | **Blocked on H1 + React P0 usable login/core ops** |

---

## H1 — Staging cutover (ops gates)

Repo automation exists; **live evidence does not**. React-primary cutover:

- [`../operator-dashboard/REACT_STAGING_PARALLEL_RUN.md`](../operator-dashboard/REACT_STAGING_PARALLEL_RUN.md)  
- [`../operator-dashboard/H1_STAGING_EVIDENCE_TEMPLATE.md`](../operator-dashboard/H1_STAGING_EVIDENCE_TEMPLATE.md)  
- Workflow: `.github/workflows/operator-cutover-readiness.yml` (`dashboard_ui: react`)  
- Release gate: `.github/workflows/release-gate.yml`  
- Migrations process: [`../operator-dashboard/OPERATOR_DASHBOARD_MAY_CUTOVER_COMPLETION_GUIDE.md`](../operator-dashboard/OPERATOR_DASHBOARD_MAY_CUTOVER_COMPLETION_GUIDE.md)

| Gate | Repo status | Live status |
|------|-------------|-------------|
| EF migrations staging/prod | Scripts/artifacts in CI | **Pending evidence** |
| Strict MigrationService readiness | Wired in compose | **Pending evidence** |
| React primary URL | Parallel-run + readiness script | **Pending evidence** |
| Parallel-run (React) | Matrix + probes | **Pending execution** |
| Sign-off | Templates | **Pending humans** |
| Cutover + Django rollback window | Documented | **Pending** |

Do **not** mark H1 complete in this file until evidence links are pasted.

---

## H4 — CI map (target)

| Job / check | Blocking? | Target |
|-------------|-----------|--------|
| `build-test` (Api.Tests) | Yes | Keep |
| `security-contract-tests` (+ RouteParity) | Yes | Expanded |
| `schema-validation` + migration artifacts | Yes | Keep |
| KMS / Compliance unit tests | Yes | Added |
| React unit tests | Yes | Keep |
| Playwright E2E | No until #439 | Keep non-blocking |
| NuGet vuln scan (High/Critical) | Yes | `dependency-vulnerability-scan` |

---

## H6 — Alerts (minimum set)

See [`ops/runbooks/alpha-launch-alerts.md`](../../ops/runbooks/alpha-launch-alerts.md).

Minimum signals: API 5xx rate, auth 401/403 spikes, Redis/DB down, migration job failed, SignalR churn. Admin security metrics already partially defined in `docs/observability/admin-security-metrics.md`.

---

## C — Contract surfaces

| Surface | Test | Location |
|---------|------|----------|
| Mobile core routes | `RouteParityContractTests` | `Synaptix.Backend.Api.Tests/Contracts/` |
| Admin security envelopes | Existing `*ContractTests` | Api.Tests |
| Compliance internal client | `ComplianceClientRouteContractTests` | `Synaptix.Security.Kms.Tests/Contracts/` |
| KMS sessions / hybrid | Existing session tests | `Synaptix.Security.Kms.Tests` |

---

## E — Doc map roots

| Doc | Role |
|-----|------|
| [`docs/README.md`](../README.md) | Index |
| [`docs/setup/LOCAL_DEV_HAPPY_PATH.md`](../setup/LOCAL_DEV_HAPPY_PATH.md) | Local bootstrap |
| [`docs/alpha-beta/REMAINING_TASKS.md`](../alpha-beta/REMAINING_TASKS.md) | Product/tech backlog |
| [`docs/audits/Synaptix_Audit_Report.md`](../audits/Synaptix_Audit_Report.md) | Historical audit + Part 5 status |
| This file | B+C+E program tracker |

---

## Change log

| Date | Change |
|------|--------|
| 2026-07-13 | Plan created; Wave 0 repo slices landed |
| 2026-07-13 | Wave 1: React gap inventory, vuln CI, alpha-launch Prometheus rules, release-gate KMS routes, make dev, H1 template |
| 2026-07-13 | **Direction flip:** React replaces Django as canonical operator dashboard; Wave 1.5 contract alignment elevated to P0/P1 |
| 2026-07-13 | **R4:** React staging parallel-run runbook; readiness script/workflow React-primary; H1 ledger retargeted |
