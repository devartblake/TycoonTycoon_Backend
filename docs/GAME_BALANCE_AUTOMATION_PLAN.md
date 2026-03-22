# Game Balance + Automation Implementation Plan

## Goal
Implement the requested **energy/lives economy**, **mode balancing**, **anti-frustration safeguards**, and **automation flows** across:

- `Tycoon.Backend.Api` + application/domain/infrastructure projects (authoritative game rules)
- `Tycoon.Sidecar` (FastAPI automation, adaptive analytics, rule suggestions)
- `Tycoon.OperatorDashboard` (operator controls + visibility)

## 1) Target Launch Baseline (authoritative defaults)

These values should be first-class config (database-backed), not hardcoded in UI:

### Global baseline
- Max energy: `20`
- Start energy: `20`
- Regen rate: `1 energy / 10 min`
- Daily free energy: `+5` (login)
- Ad energy reward: `+2` to `+4`
- Level up reward: `full refill`
- Premium option: `+25% regen` **or** `+5 max energy`

### Mode rules
- Casual: `3` energy, no lives
- Ranked: `4` energy, no lives
- Jackpot: `0` energy, ticket/gems entry, `3` lives
- Guardian: `5` energy, optional ticket, `2–3` lives

### Safeguards
- First 3 sessions reduced energy costs
- Almost-win revive discount
- Daily free jackpot ticket
- Soft pity difficulty adjustments after losses

## 2) Backend Architecture Changes (.NET)

## 2.1 Domain model additions
Add explicit economy/game mode entities/value objects under domain/application layers:

- `EnergyConfig` (cap, start, regen cadence, daily rewards, ad rewards)
- `ModeRule` per mode (`Casual`, `Ranked`, `Jackpot`, `Guardian`)
- `LifeRule` (lives per run, revive constraints, premium modifiers)
- `AntiFrustrationRule` (first sessions discount, pity ladder, revive discount)
- `MonetizationTriggerRule` (out-of-energy, near-promotion, guardian-loss, etc.)

Store these under admin-managed config tables (mirroring existing Admin Config patterns).

## 2.2 Persistence + migrations
Add EF Core migrations for:

- `game_economy_config`
- `mode_balance_rules`
- `player_energy_state`
- `player_mode_run_state` (lives, revives used, run status)
- `player_behavior_profile` (segment tags + confidence)
- `pity_progress` / loss streak support

## 2.3 API endpoints
Add/extend endpoints in `Tycoon.Backend.Api`:

### Operator/admin endpoints
- `GET /admin/economy/balance` (read full policy snapshot)
- `PATCH /admin/economy/balance` (partial updates)
- `POST /admin/economy/simulate` (input sessions, output economy forecast)

### Player-facing endpoints
- `GET /mobile/economy/state` (energy, tickets, active buffs, next regen)
- `POST /mobile/economy/consume` (mode entry consumption)
- `POST /mobile/economy/revive` (Jackpot/Guardian revive flow)
- `POST /mobile/economy/daily-claim` (daily free rewards)

### Mode-aware start/submit enforcement
Wire match start/submit pipeline to validate:
- energy affordability
- ticket affordability
- lives remaining
- premium modifiers
- safeguard rules (reduced early-session costs, pity adjustments)

## 2.4 Rule engine service
Introduce an application service, e.g. `IGameBalancePolicyService`, that:

- resolves active config
- calculates entry cost by mode and player segment
- applies anti-frustration modifiers
- returns deterministic decisions with reason codes

All callers (mobile endpoints, jobs, sidecar callbacks) must use this service.

## 3) Tycoon.Sidecar Automation Plan (FastAPI)

Use sidecar for **automation/orchestration**, while .NET remains source of truth.

## 3.1 New sidecar routers
Add routers (or extend existing):

- `/analytics/behavior-segmentation`
  - derive player archetypes: `casual`, `competitive`, `risk_taker`, `collector`, `struggling`
- `/utilities/economy/rebalance/recommend`
  - produce config recommendations from cohort telemetry
- `/utilities/economy/rebalance/apply`
  - authenticated call to backend admin balance patch endpoint
- `/webhooks/economy/trigger-offers`
  - invoke targeted offers by trigger (out of energy, near promotion, etc.)

## 3.2 Scheduled automation jobs
Add periodic tasks in sidecar (cron/worker pattern):

- hourly: energy pressure + conversion snapshot
- every 6h: mode participation + completion funnel
- daily: safeguard efficacy report (session-1..3 retention, revive acceptance, frustration indicators)
- daily: rebalance recommendation draft (never auto-apply without explicit toggle)

## 3.3 Sidecar safety gates
- dry-run mode enabled by default
- max delta constraints (e.g. energy cost cannot change by >1 per day)
- approvals required for production apply
- full audit event emission into backend/admin audit

## 4) Operator Dashboard Changes

Add a dedicated balance console page in `Tycoon.OperatorDashboard`:

- baseline config editor (global + per-mode)
- interaction matrix view (Energy + Lives)
- launch preset quick-apply (`Recommended Launch Configuration`)
- safeguard toggles + thresholds
- simulation panel (matches per full bar, session length estimate)
- change diff + publish workflow

Also include a read-only “effective rule card” in existing relevant pages so operators can confirm active values quickly.

## 5) Event + Analytics Instrumentation

Emit consistent events from backend on:
- energy spent/regenerated/claimed
- lives consumed/revived/run-eliminated
- ticket consumed/granted
- pity escalation applied
- monetization trigger fired/accepted/rejected

Use these events in sidecar analytics endpoints to drive adaptive recommendations.

## 6) Rollout Phases

## Phase 0 — Foundations (1 sprint)
- Schema + config entities + admin endpoints
- Basic mobile economy state/consume endpoints
- Jackpot/Guardian lives state model

## Phase 1 — Core Balance (1 sprint)
- Casual/Ranked/Guardian energy rules
- Jackpot ticket+lives rules
- daily free energy + level-up refill
- dashboard configuration UI (MVP)

## Phase 2 — Safeguards + Monetization Hooks (1 sprint)
- first 3 sessions discount
- pity system + almost-win revive discount
- daily free jackpot ticket
- trigger-based offer orchestration hooks

## Phase 3 — Sidecar Adaptive Automation (1 sprint)
- behavior segmentation endpoints
- rebalance recommendation engine
- guarded apply flow + audit integration

## 7) Acceptance Criteria (Shipping Tomorrow)

“Recommended Launch Configuration” is live and configurable:

- Energy cap `20`, regen `1/10m`
- Casual `3` energy
- Ranked `4` energy
- Guardian `5` energy + `2` lives
- Jackpot ticket + `3` lives
- Revive `5` gems
- Daily free jackpot ticket `1`

Operational readiness:
- operator can change values without redeploy
- audit trail exists for every config publish
- sidecar can generate (and optionally apply) recommendations safely
- metrics dashboards show funnel: Casual → Ranked → Jackpot

## 8) Immediate Next Implementation Tasks

1. Create DB schema + contracts for economy/mode rules.
2. Implement `IGameBalancePolicyService` + unit tests.
3. Wire match entry validation to policy service.
4. Add admin balance endpoints + dashboard page.
5. Add sidecar behavior segmentation + recommendation endpoints.
6. Add scheduled sidecar jobs with dry-run first.
7. Add end-to-end tests for economy/lives flows and safeguard behavior.

### Sprint A Progress

- ✅ Introduced persistent entities/configuration for game balance and player safeguard state.
- ✅ Introduced `IGameBalancePolicyService` and wired admin/mobile economy endpoints to consume policy service instead of static in-memory stores.
- ✅ Added migration scaffolding for game balance + player safeguard persistence.
- ✅ Added initial mode-entry enforcement in match start flow via policy service.
- ⏳ Pending in Sprint A: apply migration in dev/prod environments and validate schema rollout.

### Sprint C Progress (initial)

- ✅ Added behavior segmentation endpoint in Sidecar (`/analytics/behavior-segmentation`) for adaptive rule suggestions.
- ✅ Added manual dry-run job endpoints in Sidecar for rebalance reporting without auto-apply.
- ✅ Added sidecar background dry-run scheduler loop with configurable interval.
- ✅ Added guarded rebalance apply audit trail in Sidecar (`/utilities/economy/rebalance/audit`) with required approver/reason metadata and persisted apply/block outcomes.

### Sprint B Progress (initial)

- ✅ Added initial Operator Dashboard controls for viewing/updating core balance values and running simulation from Economy page.
- ✅ Added API-level contract coverage for admin and mobile economy endpoints.
- ✅ Added match-entry policy API tests (legacy mode allowance + jackpot no-ticket conflict).

## 10) Remaining Work Snapshot (Post-Latest Implementation)

1. Apply and verify `AddGameBalancePolicyPersistence` migration in each runtime environment (dev/stage/prod) with rollback playbook validation.
2. Add Sidecar automated tests for rebalance guardrails/audit endpoints and scheduler behavior.
3. Add dashboard UI surfacing for Sidecar rebalance audit history and dry-run recommendations.
4. Add production operator runbook covering:
   - required approval metadata for rebalance apply,
   - guardrail override procedure,
   - incident response for failed applies.
5. Add observability wiring (metrics + alerts) for:
   - blocked rebalance attempts,
   - apply success/failure rate,
   - pity activation distribution and revive discount usage.

### Remaining Work Progress Notes

- ✅ Sidecar guardrail and endpoint contract tests now cover success and failure paths for rebalance apply plus audit history pagination/ordering contracts.
- ✅ Operator Dashboard economy page now surfaces Sidecar rebalance audit history (latest 25 entries) when `Sidecar:BaseUrl` is configured.
- ✅ Operator Dashboard economy page now surfaces Sidecar dry-run rebalance recommendation payload (core fields + per-mode table).
- ✅ Sidecar exposes rebalance metrics at `/utilities/economy/rebalance/metrics` (attempts, blocked/success/error counts, last-at timestamps).
- ✅ Sidecar exposes Prometheus scrape-friendly counters at `/utilities/economy/rebalance/metrics/prometheus`.
- ✅ Operator Dashboard economy page now surfaces Sidecar rebalance metrics counters/timestamps for operator visibility.
- ✅ Non-production schema startup gate now supports one-shot `AutoMigrateIfMissing` fallback to reduce startup race failures when MigrationService lags.
- ✅ Sidecar exposes `/utilities/economy/rebalance/alerts` with threshold-based blocked/error-rate signals; Operator Dashboard renders active alerts.
- ✅ Sidecar can publish rebalance metrics snapshots to external Elasticsearch index via `/utilities/economy/rebalance/metrics/publish` (also attempted in scheduled dry-run loop).
- ✅ Production rebalance operations runbook now documented (`docs/REBALANCE_OPERATIONS_RUNBOOK.md`) with apply/rollback/escalation procedures.
- ✅ Operator Dashboard now reads Sidecar metrics history (`/utilities/economy/rebalance/metrics/history`) from the external sink.
- ⏳ Still pending: production alert-rule wiring against the external metrics sink.

## 9) Risks & Mitigations

- **Risk:** over-tuning can hurt fairness.
  - **Mitigation:** cap daily rule deltas + require operator approval.
- **Risk:** automation could apply unstable changes.
  - **Mitigation:** dry-run by default, explicit prod toggle, audit logs.
- **Risk:** duplicated rule logic between sidecar and backend.
  - **Mitigation:** backend remains final authority; sidecar only recommends/calls admin APIs.
