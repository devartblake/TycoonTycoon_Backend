# Priority Gap Drill-Down — Frontend Wiring Scope

**Date:** 2026-06-14
**Companion to:** `backend_frontend_gap_audit_2026-06-14.md`

> ## Implementation status (2026-06-14, `trivia_tycoon`)
> Frontend integration code added (all files pass `dart analyze`):
> - **Powerups** — `lib/core/dto/powerup_dto.dart`; `SynaptixApiClient.getPowerupState` / `usePowerup`. *(Remaining: call `usePowerup` from the in-quiz powerup buttons keyed by the active match `eventId`.)*
> - **Season-reward claim** — DTOs `RewardEligibilityDto` / `ClaimSeasonRewardResultDto` in `season_dto.dart`; `SynaptixApiClient.getSeasonRewardEligibility` / `claimSeasonReward`; **UI wired** — `season_rewards_preview_screen.dart` now shows eligibility + a working Claim button (idempotent via a per-attempt UUID).
> - **Party** — `lib/core/dto/party_dto.dart`; full REST set on `SynaptixApiClient`; **real-time** `party.matched` / `party.roster.updated` / `party.closed` streams on `MatchHub`; **UI shipped** — `party_providers.dart` (`PartyController`) + `screens/party/party_lobby_screen.dart` (create/invite/accept-decline/queue/leave, navigates to `/multiplayer/match` on `party.matched`), route `/party`.
> - **Dead call fixed** — `mission_service.generateDailyMissions` no longer calls the non-existent `/players/{id}/missions/assign`; it reads the canonical `/missions` set after generation.
> - **Compliance (4b)** — `compliance_consent_api_client.dart` + `compliance_consent_providers.dart` + `EnvConfig.complianceConsentServiceUrl` (defaults to API host); **UI shipped** — `screens/compliance/age_gate_screen.dart` (age verification, Terms/Privacy/Marketing consent, parental-consent email flow for minors), route `/age-gate`.
> - **Compliance (4a)** — unchanged: the external AML/KYC service the existing `compliance_api_client.dart` targets still does not exist in the backend; product/ops decision required.
>
> **Net-new UI (2026-06-14):** Party lobby (`/party`) and Age-gate (`/age-gate`) screens; in-quiz powerup buttons in `question_view_screen.dart` now consume server inventory via `powerup_providers.dart` (`eliminate`→FiftyFifty, `time_boost`→ExtraTime; server denial blocks, offline/unmapped fall through to local). All files pass `dart analyze`. *(Follow-ups: surface `/party` and `/age-gate` entry points in nav/onboarding; multiplayer powerup UI for Skip/DoublePoints reusing `powerupInventoryProvider`.)*

Concrete contracts + integration notes for the priority gaps. Routes are relative to
the API base (`/api/v1`). All bodies are JSON; auth = bearer token unless noted.

---

## 1. Party / Group Play (`/party`) — entirely unconsumed

**Feature flag:** gated behind `social_enabled`. If the flag is off the API returns
`403 FeatureDisabled` — confirm the flag is enabled for alpha before building UI.

**REST surface** (`Synaptix.Backend.Api/Features/Party/PartyEndpoints.cs`):

| Method | Path | Body | Returns |
|---|---|---|---|
| POST | `/party` | `{ leaderPlayerId }` | `PartyRosterDto` |
| GET | `/party/{partyId}` | — | `PartyRosterDto` (404 if gone) |
| POST | `/party/{partyId}/invite` | `{ fromPlayerId, toPlayerId }` | invite |
| POST | `/party/invites/{inviteId}/accept` | `{ playerId }` | invite |
| POST | `/party/invites/{inviteId}/decline` | `{ playerId }` | invite |
| POST | `/party/{partyId}/leave` | `{ playerId }` | 204 |
| GET | `/party/invites?playerId=&box=incoming\|outgoing\|all&page=&pageSize=` | — | paged invites |
| POST | `/party/{partyId}/enqueue` | `{ leaderPlayerId, mode, tier }` | `202 Queued` / `200` |
| POST | `/party/{partyId}/queue/cancel` | `{ leaderPlayerId }` | 204 |

**Real-time (critical):** party events are pushed over **MatchHub (`/ws/match`)** to the
`player:{playerId}` group — *not* the matchmaking hub. The Flutter `MatchHub` adapter
must join the player group and handle:
- `party.roster.updated` → `{ roster, onlinePlayerIds }`
- `party.matched` → `{ ticketId, partyId, opponentPartyId, matchId, mode, tier, scope }`
  (the server auto-joins all member connections to `match:{matchId}`)
- `party.closed` → `{ partyId, matchId, reason }`

**Frontend work:** new `PartyService` (REST) + `PartyController`/provider for roster &
invite state; extend the existing MatchHub adapter with the 3 events above; party
lobby + invite UI. Reuse existing friends/presence for the invite picker.

---

## 2. Powerups (`/powerups`) — unconsumed

**Contracts** (`Synaptix.Shared.Contracts/Dtos/PowerupDtos.cs`):

```
GET  /powerups/state/{playerId}  -> PowerupStateDto { playerId, powerups: [ { type, quantity, cooldownUntilUtc } ] }
POST /powerups/use               body UsePowerupRequest { eventId, playerId, type }
                                 -> UsePowerupResultDto { eventId, playerId, type, status, remaining, cooldownUntilUtc }
```

- `type` enum: `FiftyFifty=1, Skip=2, DoublePoints=3, ExtraTime=4`.
- `eventId` ties a use to a specific match/game event (idempotency + server validation).
- `status`: `Used | Duplicate | Insufficient | Cooldown`.

**Frontend work:** the existing `lib/core/services/store/power_up_service.dart` appears
local-only — point it at these endpoints (or add a thin remote source). Fetch state on
match start; call `/use` from the in-quiz powerup buttons keyed by the active `eventId`;
honor `cooldownUntilUtc` + `remaining` in the UI.

---

## 3. Season-reward claim (`/seasons/rewards`) — preview wired, claim missing

Frontend currently calls only `preview/{playerId}`. The two endpoints that complete the
loop are unused:

```
GET  /seasons/rewards/eligibility/{playerId}?seasonId=  -> RewardEligibilityDto
       { seasonId, playerId, eligible, reason, tier, tierRank, rankPoints, rewardCoins, rewardXp, nextClaimAtUtc }
POST /seasons/rewards/claim/{playerId}  body { eventId, seasonId? }
       -> ClaimSeasonRewardResponseDto { eventId, seasonId, playerId, status, awardedCoins, awardedXp }
```

- `eventId` is the idempotency key — generate a client GUID per claim attempt.
- `reason`: `Eligible | Placement | NotInTop20 | AlreadyClaimed | ...`
- `status`: `Applied | Duplicate | NotEligible`. `nextClaimAtUtc` gates daily re-claim.

**Frontend work:** switch the season-rewards screen from `preview` to `eligibility` for
display, add a Claim button calling `/claim/{playerId}`, apply awarded coins/xp to the
wallet, and disable until `nextClaimAtUtc`.

---

## 4. Compliance — two separate problems

**(a) Frontend's compliance client points at a service that does not exist in this repo.**
`compliance_api_client.dart` calls `/api/compliance/status`, `/api/kyc/initiate`,
`/api/transaction/{check,geo-check,age-verify}`, `/api/privacy/{export,consent,delete}`.
A grep across the whole backend solution (main API, CryptoService, Sidecar, KMS,
Compliance.Api) finds **none** of these paths. The client is wired to an external
AML/KYC/geo compliance service via `COMPLIANCE_SERVICE_URL`, and per
`service_manager.dart` it is **optional and fails closed** — when the URL is unset, all
crypto/prize gate calls return false/block. So crypto cash-out / prize flows are
effectively disabled until that external service exists and the env var is set.
→ **Decision needed:** build/point to the AML service, or descope the crypto/prize
gating for alpha. Not a path typo — a missing dependency.

**(b) `Synaptix.Compliance.Api` (age-verification / consent / parental-consent /
privacy-requests) has no frontend consumer at all.** This is the COPPA/GDPR consent
surface and is a likely alpha requirement (age gating, parental consent for minors,
data-subject requests). It is unrelated to the crypto AML client above.
→ Needs a frontend client + onboarding/age-gate UI against
`/compliance/age-verification`, `/compliance/consent`, `/compliance/parental-consent`,
`/compliance/privacy-requests`.

---

## 5. Dead call: `/players/{userId}/missions/assign`

Frontend calls this but neither `/players` nor `/missions` exposes an `assign` route on
the backend. Either remove the call or confirm the intended endpoint (mission
assignment is otherwise driven by `/missions/generate-daily`). Low effort, but it's a
guaranteed 404 in the client today.
