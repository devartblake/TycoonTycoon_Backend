# Deferred Post-Alpha Work

This file prevents Codex from spending Alpha/Beta time on non-essential work.

| Item | Priority | Reason deferred | Required before revisit |
|---|---|---|---|
| Advanced personalization tuning | P2 | Core backend flows must stabilize first | Auth, wallet, match submit, telemetry |
| Full dashboard parity | P2 | Large scope; not required for Alpha | API contracts stable |
| Multi-region deployment | P3 | Architecture task, not Alpha blocker | Single-region deployment stable |
| Automated economy balancing | P3 | Requires real gameplay data | Beta telemetry |

## Compliance Phase 2 — Deferred Items

These items are blocked on legal review, ad SDK selection, or require Alpha wallet stability before implementation.

| Item | Priority | Reason deferred | Required before revisit |
|---|---|---|---|
| Split wallet (earned vs purchased) | P1 Phase 2 | Invasive `PlayerWallet` migration; risk to core economy during Alpha. `PlayerTransaction.Kind` is sufficient for Alpha. | Alpha wallet stability confirmed + legal direction on chargeback handling |
| Monthly spend limit enforcement | P1 Phase 2 | Limit stored in `ParentalPurchaseControl.MonthlySpendLimitCents` but running spend total not checked. Requires a monthly spend tracking query. | `store_purchases_enabled` flag turned on |
| Per-purchase parent notification email | P1 Phase 2 | Parental consent email is built; per-transaction email requires purchase webhook integration. | Store purchases enabled + parent account linking built |
| Parent account linking (`parent_user_id` FK) | P1 Phase 2 | Current `ParentalPurchaseControl` links by child only. True parent-account system requires identity design. | Legal review of parent account model |
| `GET/PUT /parent/child/{id}/controls` endpoints | P1 Phase 2 | Parent-facing API requires parent account system. | Parent account linking built |
| Daily/weekly spend caps + purchase cooldowns | P2 Phase 2 | Consumer protection nice-to-have. Requires spend accumulator service. | Store purchases enabled |
| Chargeback auto-lock of wallet balance | P2 Phase 2 | Requires Stripe/PayPal dispute webhook wiring + balance lock mechanism. | Store purchases enabled |
| `StoreItem.IsPlatformRestricted` (iOS/Android/Web) | P2 Phase 2 | Platform eligibility column not yet on StoreItem. | Store catalog mature |
| `StoreItem.IsRegionRestricted` + region eligibility | P2 Phase 2 | Per-SKU region gating not implemented. | Legal/region mapping done |
| `StoreItem.IsRankAdvantage` flag | P2 Phase 2 | Not on entity yet; no rank-advantage items exist in catalog. | Before any ranked-mode items are added to catalog |
| `GET /users/me/entitlements` endpoint | P2 Phase 2 | Entitlement concept not formalised beyond wallet/inventory. | Entitlement model designed |
| Age-band / region feature flag service | P2 Phase 2 | Per-country/state/age-band feature flag dimensions require legal mapping. | Legal review + state-by-state analysis |
| Ad policy enforcement (no targeted ads under 13) | P2 Phase 2 | No ad SDK selected yet. Compliance policy is defined; enforcement is deferred. | Ad SDK selection + integration |
| Paid randomized rewards (loot boxes, spin wheels) | Phase 3 / Legal only | Blocked on odds disclosure, Belgium/NL gating, app store approval. Never ship without legal review. | Legal clearance per region + age gate |
| Tournament entry fees | Phase 3 / Legal only | Gambling law analysis required per US state. | Legal review |
| Crypto-linked rewards | Phase 3 / Legal only | FinCEN/VASP decision still pending. | Regulatory decision |
| User-to-user currency transfers | Prohibited | Prohibited by compliance strategy — do not build. | N/A |
