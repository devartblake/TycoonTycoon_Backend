# Synaptix Compliance Implementation Status

**Last updated:** 2026-06-16  
**Branch:** `claude/server-compliance-assessment-9di1q7`  
**Source document:** `synaptix_microtransaction_compliance_strategy.md`

> This document is the authoritative record of what has been built, what is deferred, and what is prohibited. Update it whenever compliance-related code changes ship.

---

## Summary

| Area | Status |
|---|---|
| Compliance microservice (COPPA / CCPA / PCI audit) | ✅ Built |
| Parental consent email dispatch | ✅ Built |
| CCPA privacy request fulfillment (anonymize / export / opt-out) | ✅ Built |
| StoreItem compliance metadata (randomized / age gate / parent approval / refundable) | ✅ Built |
| Purchase eligibility pre-check at POST /store/purchase | ✅ Built |
| ParentalPurchaseControl entity (per-child purchase gates + spend limit) | ✅ Built |
| Purchase history endpoint | ✅ Built |
| Server-authoritative receipts (Apple IAP / Google Play / Stripe / PayPal) | ✅ Pre-existing |
| Immutable transaction audit (PlayerTransaction + EconomyTransaction ledger) | ✅ Pre-existing |
| Split wallet (earned vs purchased) | ⏸ Deferred Phase 2 |
| Monthly spend limit enforcement | ⏸ Deferred Phase 2 |
| Parent account linking / parent-facing endpoints | ⏸ Deferred Phase 2 |
| Age/region feature flag service | ⏸ Deferred Phase 2 |
| Ad policy enforcement | ⏸ Deferred — no ad SDK selected |
| Paid randomized rewards (loot boxes, spin wheels) | 🚫 Prohibited until legal review |
| Crypto-linked rewards | 🚫 Prohibited until legal review |
| User-to-user currency transfers | 🚫 Permanently prohibited |

---

## What Was Built

### Synaptix.Compliance Microservice

Port 5070, auto-migrates on startup, `compliance` PostgreSQL schema, service-token protected internal endpoints.

**Entities:**
- `AgeVerification` — declared age, `IsMinor`, verification method, IP
- `ParentalConsent` — token hash, parent email hash, status, 72-hour expiry
- `PrivacyRequest` — type (Know / Delete / OptOut / DataPortability), status, fulfillment notes
- `ConsentRecord` — per-consent-type records (ToS, Privacy, Marketing, Analytics, DoNotSell)
- `ComplianceAuditEvent` — immutable event log for COPPA/CCPA/PCI actions

**Services:**
- `AgeVerificationService` — creates and retrieves age verification records
- `ParentalConsentService` — initiates consent (SHA256 token, 72-hour expiry), verifies via token, tracks status
- `PrivacyRequestService` — submits, retrieves, and updates privacy requests
- `ConsentService` — records and queries consent per type
- `ComplianceAuditService` — appends and queries audit events

**Public endpoints (JWT auth):**
- `POST /compliance/age-verification`
- `POST /compliance/consent`, `GET /compliance/consent/status`
- `POST /compliance/parental-consent/initiate`, `GET /compliance/parental-consent/status`, `POST /compliance/parental-consent/verify`
- `POST /compliance/privacy-requests`

**Internal endpoints (X-Service-Token auth):**
- `GET /internal/compliance/users/{id}/restrictions` — returns COPPA restriction list
- `GET /internal/compliance/users/{id}/consent-status`
- `POST /internal/compliance/parental-consent/initiate` — server-initiated, returns raw token
- `GET /internal/compliance/privacy-requests/pending`
- `PATCH /internal/compliance/privacy-requests/{id}`
- `POST /internal/compliance/audit`

---

### Email Service

- `IEmailService` interface in `Synaptix.Backend.Application.Email`
- `SmtpEmailService` — `System.Net.Mail.SmtpClient`, no external packages, async send
- `NullEmailService` — logs email details to `ILogger`; used when `Email:Smtp:Host` is not configured
- Auto-selected in `Synaptix.Backend.Infrastructure.DependencyInjection`
- Config keys: `Email:FromAddress`, `Email:FromName`, `Email:Smtp:Host`, `Email:Smtp:Port`, `Email:Smtp:Username`, `Email:Smtp:Password`, `Email:Smtp:UseSsl`

---

### Parental Consent Email Dispatch

**Endpoint:** `POST /users/me/parental-consent/request` (JWT auth required)

**Flow:**
1. Authenticated user submits `{ "parentEmail": "parent@example.com" }`
2. Main backend calls `POST /internal/compliance/parental-consent/initiate` with service token
3. Compliance service generates 32-byte random token, SHA256-hashes for storage, returns raw token
4. Main backend sends branded HTML email to parent with link `{ConsentVerifyUrl}?token={rawToken}`
5. Returns `202 Accepted` with `{ consentId, expiresAt }`

**Config:** `Compliance:ConsentVerifyUrl` (default: `https://app.synaptix.gg/parental-consent`)

---

### CCPA Privacy Request Fulfillment

**Hangfire job:** `PrivacyRequestFulfillmentJob` — runs every 15 minutes  
**Manual trigger:** `POST /admin/privacy-requests/{id}/process` (admin ops key required)

**Fulfillment actions by request type:**

| Type | Action |
|---|---|
| `Delete` | `AnonymizeUserAsync`: email → `deleted-{id8}@anon.synaptix`, handle → `deleted-{id8}`, password hash → dead hash, country/avatar → null, `IsActive` → false. Hard-deletes refresh tokens and redacts DM content. **Preserves** PlayerTransaction, EconomyTransaction, PlayerWallet. Records audit event `user_anonymized`. |
| `Know` / `DataPortability` | `ExportUserDataAsync`: serialises user, player, wallet, 90-day mission claims, 12-month transactions to JSON; uploads to MinIO `compliance-exports/{userId}/{timestamp}.json`; returns presigned URL. |
| `OptOut` | `ApplyOptOutAsync`: records audit event `opt_out_applied`. |

All fulfilled requests are marked `Completed` (or `Failed` with error note) via compliance client.

---

### Store Compliance Gates

**StoreItem compliance fields** (added to entity + EF config + migration):

| Field | Type | Purpose |
|---|---|---|
| `IsRandomized` | bool | Blocks purchase for any user with `minor_purchase_restricted` restriction |
| `AgeMin` | int | Triggers compliance check when > 0 |
| `RequiresParentApproval` | bool | Requires active `ParentalPurchaseControl.PurchasesEnabled = true` |
| `IsRefundable` | bool | Disclosed to client; used in refund eligibility logic |

**ParentalPurchaseControl entity:**

| Column | Type | Purpose |
|---|---|---|
| `ChildUserId` | Guid (unique) | Links to the child's user account |
| `PurchasesEnabled` | bool | Whether purchases are allowed at all |
| `MonthlySpendLimitCents` | int | Cap (stored; enforcement deferred to Phase 2) |
| `AdsEnabled` | bool | Whether rewarded/contextual ads are shown |
| `LootBoxesEnabled` | bool | Whether randomized rewards are accessible |

**Purchase eligibility check** (`StorePurchaseEligibilityService.CheckAsync`):
1. If `AgeMin <= 0` and `!RequiresParentApproval` → allow immediately (no compliance call).
2. Call `IComplianceClient.GetUserRestrictionsAsync`. If unreachable → fail-open (allow).
3. If `minor_purchase_restricted` in restrictions → `403 MINOR_PURCHASE_RESTRICTED`.
4. If `RequiresParentApproval` and no `ParentalPurchaseControl` or `PurchasesEnabled = false` → `403 PARENTAL_APPROVAL_REQUIRED`.

---

## Strategy Document Gap Analysis (Section 17 Checklist)

### Product Catalog

| Item | Status |
|---|---|
| Every SKU is server-defined | ✅ `StoreItem` entity, backend-controlled catalog |
| Every SKU has a clear display name | ✅ `StoreItem.Name` |
| Platform eligibility per SKU | ⏸ Not on `StoreItem` — deferred Phase 2 |
| Age eligibility per SKU | ✅ `StoreItem.AgeMin` |
| Region eligibility per SKU | ⏸ Not on `StoreItem` — deferred Phase 2 |
| Declares whether randomized | ✅ `StoreItem.IsRandomized` |
| Declares whether affects ranked play | ⏸ `IsRankAdvantage` not on entity — deferred Phase 2 |
| Declares whether parent approval required | ✅ `StoreItem.RequiresParentApproval` |
| Refund/reversal behavior | ✅ `StoreItem.IsRefundable` + `ReversePlayerTransactionRequest` |

### Wallet

| Item | Status |
|---|---|
| Purchased currency separated from earned | ⏸ Deferred Phase 2 — `PlayerTransaction.Kind` distinguishes source |
| Refund-locked balances | ⏸ Deferred Phase 2 |
| `/users/me/wallet` is authoritative | ✅ |
| Client cannot grant currency | ✅ Server-authoritative only |
| Wallet events audit-logged | ✅ `EconomyTransaction` double-entry ledger |

### Purchases

| Item | Status |
|---|---|
| Mobile digital goods use Apple/Google billing | ✅ Apple IAP + Google Play verification |
| Web purchases use Stripe/PayPal | ✅ |
| Receipts verified server-side | ✅ |
| Purchase records are immutable | ✅ `PlayerTransaction` append-only |
| Refunds can revoke entitlements | ✅ `ReversePlayerTransactionRequest` |
| Chargebacks can lock/reverse balances | ⏸ Deferred Phase 2 |

### Minors

| Item | Status |
|---|---|
| Age-band system exists | ✅ `AgeVerification` in compliance service |
| Parent account system exists | ✅ `ParentalConsent` + `ParentalPurchaseControl` |
| Parent approval can be required per item | ✅ `RequiresParentApproval` + eligibility check |
| Spend limits exist | ✅ `MonthlySpendLimitCents` stored (enforcement deferred) |
| Minor accounts cannot buy randomized rewards | ✅ `minor_purchase_restricted` + `IsRandomized` gate |
| Minor accounts do not receive targeted ads | ⏸ No ad SDK — deferred |
| Parent can view purchase history | ⏸ No parent-specific endpoint — deferred Phase 2 |
| Parent can disable purchases | ✅ `ParentalPurchaseControl.PurchasesEnabled = false` |

### Ads

| Item | Status |
|---|---|
| Ads optional where possible | ⏸ No ad SDK selected — policy defined, enforcement deferred |
| Rewarded ads clearly labeled | ⏸ Frontend concern + no ad SDK |
| No targeted ads for children | ⏸ Policy defined; no enforcement until ad SDK integrated |
| No forced ads in core gameplay | ⏸ Product/frontend concern |

### Ranked Play

| Item | Status |
|---|---|
| Paid items do not affect ranked outcomes | ✅ No rank-advantage items in catalog |
| `IsRankAdvantage` field on StoreItem | ⏸ Deferred Phase 2 — add before any ranked-mode items enter catalog |
| Tournament mode — no paid gameplay advantage | ✅ Tournaments disabled in Alpha |
| Battle pass does not guarantee leaderboard placement | ✅ By current catalog design |

### Subscriptions

| Item | Status |
|---|---|
| Renewal date visible | ✅ Stripe/PayPal subscription status endpoint |
| Manage/cancel link visible | ✅ Stripe billing portal session endpoint |
| Subscription status endpoint | ✅ `GET /store/subscription/status/{playerId}` |
| Webhooks update entitlement state | ✅ Stripe + PayPal webhooks |
| Refunds revoke subscription benefits | ✅ Via transaction reversal |

---

## Prohibited — Do Not Build Without Legal Review

| Feature | Reason |
|---|---|
| Paid loot boxes / spin wheels / mystery packs | Gambling law risk (Belgium, Netherlands, US states) — requires odds disclosure, region gating, legal clearance |
| Paid randomized companion unlocks | Same as above |
| Cash-out rewards (real money, gift cards, crypto) | Money transmission / FinCEN concerns |
| User-to-user currency transfers | Money transmission risk — permanently prohibited by compliance strategy |
| Tournament entry fees (cash prize) | Prize promotion / gambling law — state-by-state analysis required |
| Crypto-linked rewards | FinCEN/VASP regulatory decision pending |
| Paid leaderboard / ranked advantages | Undermines competitive integrity claim |

---

## Phase 2 Implementation Queue (Post-Alpha)

See `/.codex/heartbeat/deferred-post-alpha.md` for the full deferred table. Priority order for Phase 2:

1. Monthly spend limit enforcement (query running spend in `PlayerTransaction`)
2. Parent account linking + `GET/PUT /parent/child/{id}/controls` endpoints
3. Per-purchase parent notification email
4. Split wallet columns (`coins_earned`, `coins_purchased`, `diamonds_earned`, `diamonds_purchased`)
5. `StoreItem.IsRankAdvantage` + platform/region eligibility fields
6. Chargeback auto-lock
7. Age/region feature flag service (per-country/state/age-band)
8. Ad policy enforcement (after ad SDK selection)
