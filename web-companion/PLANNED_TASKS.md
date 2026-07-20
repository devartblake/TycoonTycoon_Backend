# Web Companion — Planned Tasks & Open Decisions

**Created**: 2026-07-19 · Companion to [WEB_COMPANION_API_AUDIT.md](./WEB_COMPANION_API_AUDIT.md)

Items here are known gaps that need a product or backend decision before (or
instead of) more front-end work. Ordered by impact on the December release.

## 1. Secure channel for web — ✅ DECIDED (a) and implemented 2026-07-19

Decision: implement the KMS secure-channel client in TypeScript (option a), for
full parity with mobile across every `RequireSecureChannel` endpoint rather than
per-endpoint exemptions.

Implemented in `src/core/security/secureChannel.ts` (syn-sec-v1: ECDH X25519 with
P-256 fallback → HKDF-SHA256 → AES-256-GCM envelopes, transcript-signature
verification, sequence + replay-nonce headers), wired into `apiClient.securePost`
and used by `/store/purchase` and `/store/payments/checkout/session`. Interop is
verified by `npm run test:secure-channel` — an independent Node implementation of
the server role written from the C# sources (both suites, tamper/AAD/downgrade
rejection, .NET encoding vectors).

**Remaining before calling it done:**
- One end-to-end run against a real KMS in staging (this environment has no live
  KMS; the harness proves protocol consistency, not deployment config).
- Session renewal is not used (fresh handshake near the 30-min expiry instead) —
  fine functionally; revisit only if handshake volume matters.
- A genuine JWT expiry mid-secure-call surfaces as an error toast rather than a
  silent token refresh (deliberate: replaying an encrypted envelope after refresh
  would trip replay protection). Low impact; revisit with the auth-refresh UX.

## 2. Stripe subscriptions on web

`/store/subscription/checkout/session` needs `{tier, billingPeriod}` from the
`/store/premium` surface. The store now shows subscription items ("coming soon"
toast). Build a proper premium/subscription page against `/store/premium` +
subscription checkout + billing portal. Same secure-channel gate as #1.

## 3. Missions: no per-player progress read endpoint

`GET /missions` returns global definitions (`{id, type, key, goal, rewardXp}`) —
no title/description strings, no per-player progress/claimed state. The web page
shows `0/goal` and claims 409 until a "my missions state" read endpoint exists
(the write side `/missions/progress/*` already tracks it). Backend work.

## 4. DTO gaps the UI papers over with zeros

- `FriendDto` has no `level`/`xp` — friends list shows 0s.
- `MyTierDto` has no `level` — "Your Rank" card shows Level 0.
- Dashboard TODOs (level, xp, rank, streak, quizzes, accuracy) — candidates exist:
  `/users/{id}/career-summary`, `/leaderboards/me/{id}`, `/seasons/state/{id}`.

Either extend the DTOs/wire the extra calls, or drop the fields from the UI.

## 5. Store itemType vocabulary

The store filter tabs assume `power-up | skill-boost | cosmetic`; actual backend
`ItemType` values are whatever the catalog seeds use (plus `premium-subscription`).
Align the vocabulary or drive tabs from the data.

## 6. Skill effects are display-only on web

The skill tree hub unlocks/respecs correctly, but quiz gameplay does not yet apply
unlocked effects (`timeBonusSec`, `streakMult`, …) client-side, and `/skills/use`
(activation audit) is not called. Decide which effects the server already applies
vs. what the client must honor, then wire them into the quiz session.

## 7. Carry-over from the roadmap (unchanged)

- Quiz celebrations (Phase 3.3), Dexie offline cache, SignalR real-time — deps
  installed, nothing wired.
- Google OAuth button is a TODO on the login page.
- Stale plan mirror in `trivia_tycoon/docs/web-companion/` (React 18/Router 6) —
  delete or refresh in that repo.
