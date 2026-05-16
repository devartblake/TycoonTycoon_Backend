# Alpha Release — Known Issues

**Release:** alpha-beta-2026  
**Last updated:** 2026-05-16

Issues are classified by severity:
- **P0** — Blocks launch; must be resolved before Alpha ships
- **P1** — Significant but has a mitigation; must be documented before launch
- **P2** — Minor; tracked for Beta resolution

No P0 issues are currently open. P1 items have documented mitigations.

---

## P1 — Has Mitigation

### KI-001: Tournaments and Advanced Seasons have no dedicated feature flag

**Severity:** P1  
**Component:** Feature Flags, Seasons, Matchmaking  
**Status:** Known limitation, tracked for Beta

**Description:** The `ALPHA_DISABLED_FEATURES.md` lists Tournaments and Advanced Seasons as disabled, but they have no dedicated `tournaments_enabled` or `advanced_seasons_enabled` flag. Tournament access is controlled indirectly by `matchmaking_enabled = false`. If matchmaking is enabled for Beta before tournament balance is ready, tournaments would become accessible automatically.

**Mitigation:** Do not enable `matchmaking_enabled` until tournaments have been reviewed. Dedicated flags for `tournaments_enabled` will be added before enabling matchmaking in Beta.

**Resolution target:** Beta sprint 1

---

### KI-002: CryptoEconomyEndpoints contains legacy per-handler 503 CRYPTO_DISABLED responses

**Severity:** P1  
**Component:** Crypto, Feature Flags  
**Status:** Functionally correct, cosmetically stale

**Description:** `CryptoEconomyEndpoints.cs` has a group-level `FeatureDisabled` 403 filter (added in session 3). However, per-handler checks using `cfg.GetValue<bool>("Crypto:Enabled")` and returning `503 CRYPTO_DISABLED` still exist in each handler body (lines 55, 147, 199, 239, 280, 309). These are never reached when `crypto_enabled = false` because the group filter fires first, but they remain in the code.

**Mitigation:** The group filter is the authoritative gate. Legacy per-handler checks are dead code when the group filter is active. They do not affect correctness.

**Resolution target:** Beta cleanup sprint — remove legacy per-handler checks

---

### KI-003: SignalR hubs gated at path-based middleware level, not hub method level

**Severity:** P1  
**Component:** Realtime Multiplayer, Feature Flags  
**Status:** Functionally correct for Alpha

**Description:** The `realtime_multiplayer_enabled` flag is enforced via inline path-based middleware in `Program.cs` that intercepts all `/ws/*` requests. This fires before the WebSocket upgrade, correctly rejecting connections with `403`. However, if an authorized connection somehow establishes (e.g., via internal network bypass or during a rolling restart where middleware hasn't loaded), hub methods themselves have no per-method flag check.

**Mitigation:** The path-based gate is the correct approach for connection-level blocking and is sufficient for Alpha where all traffic goes through the standard HTTP stack. Hub method-level filters (`IHubFilter`) would add defense-in-depth for Beta if the SignalR service is exposed directly.

**Resolution target:** Beta — add `IHubFilter` for defense-in-depth

---

### KI-004: PostgreSQL advisory lock not tested under concurrent-container scenario

**Severity:** P1  
**Component:** MigrationService  
**Status:** Implemented, untested in concurrent scenario

**Description:** `pg_advisory_lock(987654321)` is added to `MigrationWorker.ExecuteAsync` around `MigrateAsync`. The lock is functional but has not been tested with two simultaneously-started migration containers to confirm only one proceeds. For Alpha with single-container deployments, this is a non-issue.

**Mitigation:** Alpha uses single-container migration (Docker Compose with `restart: "no"`). The advisory lock is a precautionary measure for future blue-green deployments.

**Resolution target:** Before blue-green production deployment; validate with `docker-compose scale migration=2` on staging

---

### KI-005: Store purchase flows (Stripe/PayPal) exist but are not Alpha scope

**Severity:** P1  
**Component:** Store  
**Status:** Built, not in Alpha scope

**Description:** `StoreEndpoints.cs` contains full Stripe and PayPal purchase flows. These are not behind a feature flag and will respond if called. Stripe/PayPal credentials are not configured for staging, so purchase attempts will return `503 STRIPE_NOT_READY` or `503 PAYPAL_NOT_READY`.

**Mitigation:** Store purchase flows return 503 if payment provider is not configured — safe for unconfigured staging. Alpha testers are internal only and are not expected to attempt purchases.

**Resolution target:** Before public Beta — add `store_purchases_enabled` feature flag gate; configure payment sandbox credentials

---

## P2 — Track for Beta

### KI-006: Mission progress tracking uses ProcessedGameplayEvent but not all event types are covered

**Severity:** P2  
**Component:** Missions  
**Description:** `ProcessedGameplayEvent` records `solo-quiz-complete` events for deduplication. Other mission trigger types (e.g., study session completed, friend match won) are not yet wired to mission progress tracking.

**Resolution target:** Beta milestone 1

---

### KI-007: Hangfire dashboard exposed without authentication in Development

**Severity:** P2  
**Component:** Admin, Hangfire  
**Description:** `/hangfire` dashboard is accessible without additional authentication in the Development environment. In Production/Staging this is gated behind admin auth, but developers who run the API locally should be aware.

**Resolution target:** Non-issue in deployed environments; no action needed

---

### KI-008: ArcadeSpinClaims reset schedule is daily but time zone is not configurable

**Severity:** P2  
**Component:** Arcade Spins  
**Description:** Daily arcade spin reset runs at midnight UTC. Players in UTC+10 to UTC+14 time zones experience the reset mid-afternoon rather than overnight, which may feel inconsistent.

**Resolution target:** Beta UX review — consider player local time zone offset for reset scheduling

---

## Resolved (for reference)

| ID | Issue | Resolved |
|----|-------|---------|
| — | 503 `FEATURE_DISABLED` responses (Flutter `FeatureDisabledException` mismatch) | Session 3 — all handlers return 403 `FeatureDisabled` |
| — | Public `GET /api/v1/app/config` missing | Session 3 — `AppConfigEndpoints.cs` created |
| — | No server-side reward grant for quiz completion | Session 3 — `POST /quiz/complete` + `CompleteQuizHandler` created |
| — | 10+ endpoint groups had no feature flag gate | Session 3 & 4 — all systems gated |
| — | Migration runner had no concurrent-run protection | Session 5 — `pg_advisory_lock` added |
