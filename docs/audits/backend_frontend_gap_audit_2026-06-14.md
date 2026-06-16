# Backend ↔ Frontend Integration Gap Audit

**Date:** 2026-06-14
**Backend:** `TycoonTycoon_Backend` (Synaptix .NET microservices)
**Frontend:** `trivia_tycoon` (Flutter client)

## Method

Extracted the backend's full HTTP route surface (74 endpoint groups across
`Synaptix.Backend.Api` + `Synaptix.Compliance.Api`), the 5 SignalR hubs, and the
gRPC `MobileMatchService`, then matched each against the Flutter client's actual
call paths in `lib/` (services, repositories, networking clients).

"Missing" = backend-implemented and player-relevant, but no frontend code calls it.

## Verdict

Coverage is broad — auth, store, quiz/questions, leaderboards, rewards, missions,
study, crypto, coach, personalization, guardians, territory, votes, all 5 WS hubs,
and the gRPC match path are wired. The gaps below are real and a few are high-impact.

---

## 🔴 Player-facing backend features NOT consumed by the frontend

| Backend group | Endpoints | Impact |
|---|---|---|
| **`/party`** | create, invite, accept/decline, leave, `enqueue` (party matchmaking), queue/cancel | **Entire party / group-play feature is unconsumed.** Frontend has presence + messaging but no party formation or party-based matchmaking. Largest gap. |
| **`/powerups`** | `GET /state/{playerId}`, `POST /use` | Power-up state & consumption not wired. Frontend has a `power_up_service.dart` but it never calls these — likely local/stub only. |
| **`/seasons/rewards`** | `eligibility/{playerId}`, **`claim/{playerId}`**, `preview/{playerId}` | **Partial — and the important half is missing.** Frontend calls only `preview`. Players can *see* season-tier rewards but there is **no claim/eligibility wiring**. |

---

## 🟡 Partial integrations (consumed, but incomplete)

- **Coach** — frontend uses `daily-brief` and `feedback`, but not `/coach/recommendations/*` (accept/dismiss recommendations).
- **Missions** — heavy frontend usage, but it calls `/players/{userId}/missions/assign`, which **does not exist** on the backend `/players` group (see mismatches below).
- **QR** — frontend uses `/qr/track-scan` only; backend `/qr` generate/metadata side unused (minor).

---

## ⚠️ Likely-broken / mismatched frontend calls (inverse problem)

1. **Compliance path divergence.** Frontend's `compliance_api_client.dart` calls
   `/api/compliance/status/{userId}`, `/api/privacy/{userId}`, `/api/privacy/consent`,
   `/api/privacy/export/{userId}`, `/api/kyc/initiate`,
   `/api/transaction/{check,geo-check,age-verify}`. The backend exposes a
   **completely different shape**: `/compliance/age-verification`, `/compliance/consent`,
   `/compliance/parental-consent`, `/compliance/privacy-requests`. The frontend is
   pointed at a different (Django/legacy?) compliance service or is dead.
   **Needs reconciliation.**
2. **`/players/{userId}/missions/assign`** — called by the frontend, no backend route.
   Dead call or expects an admin/missions route.

---

## 🟢 Admin surface (frontend `lib/admin/` covers a subset)

Frontend admin wires: questions, users, store (flash-sales, stock-policies,
reward-limits, analytics), config, notifications, auth.

Backend admin groups with **no frontend admin UI**: `anti-cheat` (+analytics, +party),
`moderation` & `moderation/escalation`, `player-lookup`, `player-transactions`,
`email-acl`, `event-queue`, `experiments` (admin), admin `seasons`
(lifecycle/points/rewards), admin `skills`, admin `powerups`, admin `personalization`,
admin `learning-modules`, admin `matches`, `mongodb`, `storage`, `setup`, admin `media`.
Expected if the operator dashboard (`Synaptix.OperatorDashboard` / Django) owns these —
worth confirming so they aren't double-built.

---

## Recommended priority

1. **Party** + **Powerups** + **Season-reward claim** — implement frontend wiring
   (shipped backend features with zero/partial client value today).
2. **Compliance path reconciliation** — decide canonical service and fix client base
   path; release-blocker for an alpha with age-gating.
3. Fix/remove the `/players/.../missions/assign` dead call.
