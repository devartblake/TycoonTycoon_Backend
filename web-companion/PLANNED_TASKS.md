# Web Companion — Planned Tasks & Open Decisions

**Created**: 2026-07-19 · Companion to [WEB_COMPANION_API_AUDIT.md](./WEB_COMPANION_API_AUDIT.md)

Items here are known gaps that need a product or backend decision before (or
instead of) more front-end work. Ordered by impact on the December release.

## 1. Secure channel for web — THE revenue blocker (decision needed)

`POST /store/payments/checkout/session` and `POST /store/purchase` are gated by
`RequireSecureChannel`: the request body must be a KMS-encrypted envelope tied to a
secure session (`/api/v1/security/sessions/start` handshake — ECDH X25519/P-256,
sequence numbers, replay nonces, encrypted responses). The mobile app implements
this; the web client does not.

The Stripe checkout flow (store UI → session create → redirect → success/cancel
pages) is now fully built and will work the moment this is resolved. Until then the
backend answers `400 secure_session_required` and the store shows a clear error
toast. Two options:

- **(a) Implement the secure-channel client in TypeScript** (Web Crypto: ECDH +
  HKDF + AES-GCM, session lifecycle, envelope wrapper in `apiClient`). Significant,
  security-sensitive work; needs a live KMS to verify against. Also unblocks
  coin/diamond `/store/purchase` (same gate).
- **(b) Relax the gate for web checkout-session creation** (e.g. an allowlisted
  plain-JSON path for `payments/checkout/session` only). Defensible because Stripe
  Checkout hosts the card entry — this call carries no payment data and is already
  JWT-authenticated — but it is a deliberate security-boundary change that should go
  through review (see `review-security-boundary`), not be slipped in.

Until decided, **every purchase path on web (virtual currency AND card) is blocked
at runtime** even though the UI and client plumbing are complete.

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
