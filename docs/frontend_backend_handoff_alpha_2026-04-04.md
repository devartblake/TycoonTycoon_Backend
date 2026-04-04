# Frontend Handoff — Backend Alpha Alignment (2026-04-04)

## Purpose
This handoff summarizes what backend contracts are now available for frontend integration and what is still pending to reach full production behavior.

---

## Backend capabilities confirmed for frontend use

## 1) Questions gameplay
- `GET /questions/set?category=&difficulty=&count=`
- `POST /questions/check`
- `POST /questions/check-batch`
- Only `Approved` questions are served.

**Frontend action:** wire gameplay question loops to these endpoints as authoritative sources.

## 2) Store / IAP / inventory / subscription
- `GET /store/catalog`
- `POST /store/purchase`
- `POST /store/iap/validate` (strict provider verification capable by config)
- `GET /store/inventory/{playerId}`
- `GET /store/subscription/status/{playerId}`
- `POST /store/subscription/activate`

**Frontend action:** use backend catalog/pricing as source of truth; consume purchase and IAP validation result payloads directly.

## 3) Crypto economy
- `POST /crypto/link-wallet`
- `GET /crypto/balance/{playerId}`
- `GET /crypto/history/{playerId}`
- `POST /crypto/withdraw`
- Prize pool:
  - `POST /crypto/prize-pool/fund`
  - `GET /crypto/prize-pool/{poolId}`
  - `POST /crypto/prize-pool/distribute` (admin/ops flow)
- Staking:
  - `POST /crypto/stake`
  - `POST /crypto/unstake`
  - `GET /crypto/staking/{playerId}`
- Withdrawal settlement admin controls exist for operator flow.

**Frontend action:** build player-facing wallet/history/staking UI now; keep settlement operations out of player client UX.

## 4) Users / profile / social
- `GET /users/search?handle=`
- `GET /users/{userId}/career-summary`
- `GET /users/me/preferences/loadout`
- `PUT /users/me/preferences/loadout`
- `DELETE /friends` (unfriend)

**Frontend action:** wire profile/search/loadout/friend removal directly against backend endpoints.

## 5) ML scoring endpoints (new baseline)
- `POST /ml/churn-risk`
- `POST /ml/match-quality`
- both return `source` (`deployed-model` or `heuristic`) so UI/telemetry can track fallback behavior.

**Frontend action:** consume as optional enhancement signals (do not hard-block primary UX if source is heuristic).

---

## Items still pending (frontend impact)

1. **Runtime deployment evidence (6.1 closeout)**
   - build/migration/live-smoke proof still needs to be executed in .NET-capable runtime environment.
2. **ML model operations hardening**
   - production calibration + monitoring for deployed model health remains pending.
3. **Frontend-only polish priorities (from remaining work)**
   - retention hooks
   - sound cue layer
   - copy/accessibility sweep
   - release QA pass

---

## Recommended frontend sequencing (next sprint)

1. Finalize wallet sync + purchase reconciliation against backend economy/store transactions.
2. Ship profile/loadout/search/career-summary integration.
3. Integrate question set/check/check-batch as the only gameplay grading source.
4. Stage crypto wallet/history/staking screens behind feature flag.
5. Keep ML-driven UX flags optional until churn/quality backend models are promoted.

---

## Integration notes

- Use backend balances/transactions as authoritative state (do not trust local-only wallet cache).
- Treat store purchase responses as source of updated balances.
- For crypto withdrawals and settlement status, poll backend history rather than inferring from local actions.
- Keep strict IAP behavior environment-driven; surface user-friendly retry/error messaging for transient validation failures.
