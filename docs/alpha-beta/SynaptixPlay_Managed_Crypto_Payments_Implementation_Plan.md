# SynaptixPlay Managed Cryptocurrency Payments Implementation Plan

**Repository:** `devartblake/TycoonTycoon_Backend`  
**Primary objective:** Allow SynaptixPlay customers to pay with cryptocurrency through a managed third-party provider while minimizing custody, treasury, security, compliance, and operational liability for SynaptixPlay.  
**Recommended launch model:** Hosted cryptocurrency checkout with automatic settlement to USD, followed by server-verified fulfillment of closed-loop digital goods.

---

## 1. Executive Decision

SynaptixPlay should **not** launch the repository's current direct on-chain cryptocurrency settlement architecture for Alpha or Beta.

The current implementation is designed to:

- Hold or access treasury private keys.
- Validate external blockchain addresses.
- Submit withdrawals directly to blockchain networks.
- Maintain pending and settled withdrawal records.
- Support wallet linking.
- Track internal crypto balances.
- Support prize pools.
- Support staking and unstaking.
- Operate settlement workers for Solana, XRP, Ethereum-compatible assets, and a proposed Synaptix token.

That design creates materially greater security, accounting, compliance, custody, fraud, and operational exposure.

The recommended production architecture is:

```text
Player
  |
  | Selects a product priced in USD
  v
Synaptix.Backend.Api
  |
  | Creates an internal payment order
  | Creates a hosted provider checkout session
  v
Managed Payment Provider
  |
  | Handles crypto wallet interaction
  | Handles crypto processing and conversion
  | Settles merchant proceeds in USD
  v
Verified Provider Webhook
  |
  | Signature validation
  | Provider-side payment re-verification
  v
Synaptix.Backend.Api
  |
  | Marks order paid exactly once
  | Grants closed-loop digital goods
  | Records immutable audit and ledger entries
  v
PostgreSQL / Existing Transaction Ledger
```

### Recommended provider order

1. **PayPal Pay with Crypto**
   - Best fit for broad consumer familiarity and hosted crypto payments.
   - Synaptix prices products in USD.
   - PayPal handles the crypto payment flow.
   - Synaptix receives fiat settlement.

2. **Stripe Stablecoin Payments**
   - Strong second provider.
   - Excellent developer tooling and .NET integration.
   - Suitable where stablecoin payment support is sufficient.

3. **Coinbase Business**
   - Consider only if a dedicated Coinbase merchant workflow is operationally useful.
   - Do not choose a configuration that leaves Synaptix holding customer crypto unless that is a deliberate, legally reviewed decision.

---

## 2. Non-Negotiable Alpha/Beta Guardrails

The following constraints should be treated as release-blocking requirements.

### 2.1 Synaptix must not custody customer cryptocurrency

Do not:

- Hold user private keys.
- Generate custodial wallets for players.
- Store seed phrases.
- Pool customer crypto.
- Maintain omnibus customer wallets.
- Sign customer transactions.
- Operate a user withdrawal treasury.

### 2.2 Synaptix must not operate direct crypto withdrawals

Disable:

- External wallet withdrawals.
- Treasury-signed payouts.
- Blockchain settlement workers.
- Manual withdrawal triggers.
- Network-specific transfer clients.
- Automated payout retries.

### 2.3 Synaptix must not launch a redeemable internal token

Internal SynaptixPlay currency must remain:

- Closed-loop.
- Non-transferable between users.
- Non-redeemable for cash.
- Non-redeemable for cryptocurrency.
- Non-tradable.
- Non-interest-bearing.
- Non-yield-generating.
- Unavailable outside SynaptixPlay.
- Clearly described as entertainment value, not an investment.

### 2.4 Synaptix must not launch staking

Disable and remove user-facing access to:

- Stake.
- Unstake.
- Yield.
- APY.
- Lockups that promise economic returns.
- Token appreciation language.

### 2.5 Crypto-funded player wagering must not be enabled

Do not allow:

- Player-funded crypto prize pools.
- Crypto entry fees tied to chance-based outcomes.
- Cash-equivalent crypto tournament payouts.
- Random crypto jackpots.
- Paid-entry crypto wagering.

For Alpha/Beta, rewards should be platform-funded and non-cash unless separately reviewed by qualified counsel.

---

## 3. Current Repository Assessment

The repository already contains two distinct cryptocurrency layers.

### 3.1 `.NET crypto economy layer`

Location:

```text
Synaptix.Backend.Api/Features/Crypto/
```

The API currently supports or contemplates:

- Wallet linking.
- Internal crypto balances.
- Crypto transaction history.
- Withdrawal requests.
- Prize-pool funding.
- Prize-pool distribution.
- Staking.
- Unstaking.
- Settlement approval.
- Settlement rejection.
- Pending-withdrawal administration.

The existing feature flags are useful and should remain part of the shutdown plan:

```text
crypto_enabled = false
Crypto:Enabled = false
```

### 3.2 Python on-chain settlement service

Location:

```text
Synaptix.CryptoService/
```

The service currently includes:

- Blockchain clients.
- Address validation.
- On-chain balance queries.
- MongoDB settlement logging.
- A settlement worker.
- Backend callbacks.
- Solana support.
- XRP support.
- EVM support.
- Treasury secret configuration.
- Manual settlement triggers.

### 3.3 Production disposition

For Alpha/Beta:

- Do not deploy `Synaptix.CryptoService`.
- Remove it from production Docker Compose profiles.
- Do not expose its port.
- Do not configure mainnet endpoints.
- Do not provision treasury keys.
- Do not provision a Synaptix token mint.
- Do not activate the settlement scheduler.
- Do not allow the .NET backend to generate pending crypto withdrawals.
- Keep the code available only as an experimental, disabled module if future research is desired.

---

## 4. Target Architecture

## 4.1 System responsibilities

### Synaptix.Backend.Api

Responsible for:

- Product catalog.
- USD price definitions.
- Player identity.
- Internal payment-order creation.
- Provider checkout creation.
- Webhook receipt.
- Webhook signature validation.
- Provider API re-verification.
- Fulfillment.
- Refund-state tracking.
- Reconciliation.
- Audit logging.
- Entitlements.
- Closed-loop currency grants.

Not responsible for:

- Blockchain private keys.
- Wallet custody.
- Crypto conversion.
- Blockchain transaction signing.
- Chain monitoring.
- Customer wallet security.
- Crypto asset custody.
- Network fee management.

### Payment provider

Responsible for:

- Hosted crypto payment user experience.
- Wallet interaction.
- Crypto payment processing.
- Provider-side customer eligibility.
- Conversion.
- Provider-side compliance controls.
- Merchant settlement.
- Provider transaction identifiers.
- Payment status events.
- Provider refund mechanisms.

### Flutter and Next.js clients

Responsible for:

- Displaying eligible payment methods.
- Requesting checkout from the backend.
- Opening the provider-hosted flow.
- Polling or refreshing order state after checkout.
- Displaying fulfillment results.

Clients must never be trusted to declare payment success.

---

## 5. Recommended Project Structure

Create a provider-neutral payment subsystem in the .NET backend.

```text
Synaptix.Backend.Domain/
  Payments/
    PaymentOrder.cs
    PaymentOrderStatus.cs
    PaymentFulfillmentStatus.cs
    PaymentProviderType.cs
    PaymentEvent.cs
    PaymentRefund.cs

Synaptix.Backend.Application/
  Payments/
    Abstractions/
      IPaymentProvider.cs
      IPaymentOrderRepository.cs
      IPaymentEventRepository.cs
      IPaymentReconciliationService.cs
    Models/
      CreateCheckoutCommand.cs
      CheckoutSessionResult.cs
      ProviderPaymentSnapshot.cs
      RefundRequest.cs
      RefundResult.cs
    Services/
      PaymentOrderService.cs
      PaymentFulfillmentService.cs
      PaymentReconciliationService.cs
      PaymentRefundService.cs

Synaptix.Backend.Infrastructure/
  Payments/
    PayPal/
      PayPalOptions.cs
      PayPalAuthClient.cs
      PayPalPaymentProvider.cs
      PayPalWebhookVerifier.cs
      PayPalModels.cs
    Stripe/
      StripeOptions.cs
      StripePaymentProvider.cs
      StripeWebhookVerifier.cs
    Persistence/
      PaymentOrderRepository.cs
      PaymentEventRepository.cs
      PaymentRefundRepository.cs

Synaptix.Backend.Api/
  Features/
    Payments/
      PaymentEndpoints.cs
      PaymentWebhookEndpoints.cs
      PaymentAdminEndpoints.cs
      PaymentContracts.cs

Synaptix.Backend.Tests/
  Payments/
    PaymentOrderServiceTests.cs
    PaymentFulfillmentServiceTests.cs
    PaymentWebhookTests.cs
    PaymentIdempotencyTests.cs
    PaymentReconciliationTests.cs
```

---

## 6. Core Provider Abstraction

Use an abstraction that supports PayPal first and Stripe later.

```csharp
public interface IPaymentProvider
{
    PaymentProviderType ProviderType { get; }

    Task<CheckoutSessionResult> CreateCheckoutAsync(
        CreateCheckoutCommand command,
        CancellationToken cancellationToken);

    Task<ProviderPaymentSnapshot> GetPaymentAsync(
        string providerOrderId,
        CancellationToken cancellationToken);

    Task<RefundResult> RefundAsync(
        RefundRequest request,
        CancellationToken cancellationToken);

    Task<bool> VerifyWebhookAsync(
        HttpRequest request,
        string rawBody,
        CancellationToken cancellationToken);
}
```

Recommended supporting interfaces:

```csharp
public interface IPaymentFulfillmentService
{
    Task<FulfillmentResult> FulfillAsync(
        Guid paymentOrderId,
        CancellationToken cancellationToken);
}

public interface IPaymentReconciliationService
{
    Task<ReconciliationResult> ReconcileAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        CancellationToken cancellationToken);
}
```

---

## 7. Payment Data Model

## 7.1 `PaymentOrder`

Recommended fields:

```text
Id
PlayerId
ProductId
Provider
ProviderOrderId
ProviderCaptureId
ExpectedAmount
ActualAmount
Currency
Status
FulfillmentStatus
IdempotencyKey
CreatedAtUtc
UpdatedAtUtc
CapturedAtUtc
FulfilledAtUtc
CancelledAtUtc
RefundedAtUtc
ProviderMetadataJson
Version
```

Recommended statuses:

```text
Created
Pending
Approved
Captured
Denied
Cancelled
Expired
RefundPending
PartiallyRefunded
Refunded
Failed
```

Recommended fulfillment statuses:

```text
NotStarted
Pending
Completed
Failed
Compensating
Reversed
```

## 7.2 `PaymentEvent`

Recommended fields:

```text
Id
Provider
ProviderEventId
ProviderOrderId
EventType
ReceivedAtUtc
VerifiedAtUtc
ProcessedAtUtc
ProcessingStatus
PayloadHash
ErrorCode
ErrorMessage
RetryCount
```

Create a unique index on:

```text
(Provider, ProviderEventId)
```

## 7.3 `PaymentFulfillment`

Recommended fields:

```text
Id
PaymentOrderId
PlayerId
ProductId
LedgerTransactionId
FulfillmentType
Quantity
Status
CreatedAtUtc
CompletedAtUtc
FailureReason
```

Create a unique index on:

```text
PaymentOrderId
```

This prevents duplicate grants.

## 7.4 Avoid storing unnecessary crypto data

Do not store:

- Seed phrases.
- Private keys.
- Wallet passwords.
- Customer wallet credentials.
- Full provider secrets.
- Raw unbounded webhook payloads indefinitely.
- Wallet addresses unless required for support or fraud review.
- Sensitive provider identity records that Synaptix does not need.

---

## 8. API Design

## 8.1 Client checkout endpoint

```http
POST /payments/checkout
Authorization: Bearer <player-token>
Content-Type: application/json
```

Example request:

```json
{
  "productId": "premium-pass-monthly",
  "provider": "paypal",
  "returnUrl": "https://app.synaptixplay.com/payments/return",
  "cancelUrl": "https://app.synaptixplay.com/payments/cancel"
}
```

Server responsibilities:

1. Resolve the authenticated player from the token.
2. Load the product from the authoritative catalog.
3. Ignore client-supplied price values.
4. Create an internal payment order.
5. Generate an idempotency key.
6. Create the provider checkout session.
7. Store the provider order ID.
8. Return the approval URL.

Example response:

```json
{
  "paymentOrderId": "00000000-0000-0000-0000-000000000000",
  "provider": "paypal",
  "status": "Pending",
  "approvalUrl": "https://provider.example/checkout/...",
  "expiresAtUtc": "2026-07-22T23:00:00Z"
}
```

## 8.2 Payment status endpoint

```http
GET /payments/{paymentOrderId}
Authorization: Bearer <player-token>
```

Return:

- Internal payment status.
- Fulfillment status.
- Product.
- Amount.
- Currency.
- Provider.
- Non-sensitive timestamps.

## 8.3 PayPal webhook endpoint

```http
POST /payments/webhooks/paypal
```

Processing sequence:

1. Read the exact raw body.
2. Validate required PayPal headers.
3. Verify the webhook signature.
4. Reject invalid events.
5. Insert the event with a unique provider event ID.
6. If duplicate, return success without reprocessing.
7. Resolve the associated internal payment order.
8. Retrieve the payment directly from PayPal.
9. Verify:
   - Merchant account.
   - Provider order ID.
   - Capture status.
   - Currency.
   - Amount.
   - Product reference or custom metadata.
10. Transition the internal order.
11. Fulfill exactly once.
12. Return a successful webhook response.

## 8.4 Administrative endpoints

Protect using existing administrative policies and strong audit logging.

Recommended routes:

```text
GET  /admin/payments
GET  /admin/payments/{id}
POST /admin/payments/{id}/reconcile
POST /admin/payments/{id}/retry-fulfillment
POST /admin/payments/{id}/refund
GET  /admin/payment-events
GET  /admin/payment-reconciliation/runs
```

Do not reuse a weak static administrative key as the only protection. Require:

- Authenticated administrator identity.
- Role or policy authorization.
- MFA for sensitive operations where possible.
- Audit logs.
- Rate limiting.
- Optional network restrictions for operator-only functions.

---

## 9. Fulfillment Design

Fulfillment must be server-side, transactional, and idempotent.

Example flow:

```text
Webhook verified
  |
  v
Payment order retrieved and locked
  |
  v
Already fulfilled?
  | yes -> return prior result
  | no
  v
Validate captured payment
  |
  v
Begin database transaction
  |
  +--> Mark order captured
  |
  +--> Create immutable player transaction
  |
  +--> Grant entitlement or closed-loop currency
  |
  +--> Create fulfillment record
  |
  +--> Mark fulfillment completed
  |
  v
Commit
```

### Required safeguards

- Use a database transaction.
- Use optimistic concurrency or row locking.
- Enforce unique fulfillment constraints.
- Never grant value based on the client redirect.
- Never trust a client-supplied amount.
- Never trust a client-supplied player ID.
- Never process an unverified webhook.
- Re-query the provider for high-value transactions.
- Record the provider capture ID.
- Record the existing Synaptix ledger transaction ID.

---

## 10. Closed-Loop Economy Rules

The existing `CRYPTO_UNITS` naming should be retired for user-facing Alpha/Beta functionality.

Recommended replacement names:

- Synaptix Credits.
- Trivia Coins.
- Gems.
- Event Tokens.
- Premium Credits.

### Required product rules

The terms and product definitions should state that credits:

- Have no cash value.
- Are licensed, not owned as property.
- Cannot be sold.
- Cannot be transferred.
- Cannot be exchanged for cryptocurrency.
- Cannot be withdrawn.
- Cannot earn interest.
- May be modified or discontinued subject to applicable law.
- Are intended solely for use in SynaptixPlay.

### Repository changes

Replace or isolate references to:

```text
crypto:units
crypto:staked:units
crypto:prize-pool:units
```

Suggested closed-loop replacements:

```text
wallet:premium-credits
wallet:event-tokens
wallet:gems
```

Do not use blockchain terminology in the ordinary internal game economy.

---

## 11. Security Requirements

## 11.1 Secret management

Store provider credentials in a managed secret system.

Recommended options:

- Docker secrets for development and controlled single-host deployments.
- 1Password Secrets Automation.
- HashiCorp Vault.
- Doppler.
- Cloud-provider secret manager.
- A properly implemented internal KMS-backed secret service.

Never:

- Commit secrets.
- Store secrets in Flutter.
- Store secrets in Next.js browser code.
- Bake secrets into Docker images.
- Log bearer tokens.
- Expose webhook secrets in diagnostics.

## 11.2 API controls

Require:

- TLS.
- Strict CORS.
- Authentication.
- Authorization.
- Rate limiting.
- Request-size limits.
- Structured validation.
- Secure headers.
- Correlation IDs.
- Central exception handling.
- No sensitive stack traces in production.

## 11.3 Webhook controls

Require:

- Signature verification.
- Timestamp tolerance.
- Replay detection.
- Unique provider event IDs.
- Raw-body hashing.
- Idempotent processing.
- Retry-safe handlers.
- Dead-letter handling.
- Alerting for repeated verification failures.

## 11.4 Administrative controls

Require:

- Least privilege.
- Separate operator and developer roles.
- MFA for provider dashboards.
- Restricted refund permissions.
- Restricted reconciliation permissions.
- Audit logs for all financial actions.
- No shared provider accounts.
- Quarterly access review.
- Immediate credential rotation after personnel changes.

---

## 12. Reconciliation

Implement reconciliation before enabling crypto checkout in production.

### 12.1 Scheduled reconciliation

Run at least daily.

Compare:

- Local payment orders.
- Provider orders.
- Captures.
- Denials.
- Refunds.
- Fulfillment records.
- Player ledger transactions.

### 12.2 Exception categories

Track:

```text
ProviderCapturedLocalPending
ProviderCapturedFulfillmentMissing
LocalCapturedProviderNotCaptured
AmountMismatch
CurrencyMismatch
DuplicateCapture
DuplicateFulfillment
RefundMissingLocally
LocalRefundProviderNotRefunded
WebhookMissing
WebhookVerificationFailed
```

### 12.3 Operator workflow

Each exception must include:

- Severity.
- Payment-order ID.
- Provider order ID.
- Provider capture ID.
- Player ID.
- Product ID.
- Expected amount.
- Actual amount.
- Recommended operator action.
- Full audit trail.

---

## 13. Refund Design

Refunds must be initiated from the backend or provider dashboard under controlled permissions.

### Refund sequence

1. Verify operator authorization.
2. Load the original payment.
3. Confirm refundable amount.
4. Submit refund to the provider.
5. Record provider refund ID.
6. Mark local refund pending.
7. Await provider confirmation.
8. Reverse or revoke the digital entitlement when permitted.
9. Handle partially consumed products using a documented policy.
10. Record all actions.

### Product-policy considerations

Define separate rules for:

- Unused virtual currency.
- Partially used virtual currency.
- Subscriptions.
- Event tickets.
- Digital cosmetics.
- Consumable power-ups.
- Fraudulent account access.
- Duplicate payments.
- Minor purchases.
- Provider-mandated refunds.

---

## 14. Legal and Liability Risk Register

This plan is a technical and operational risk-reduction framework, not legal advice. Qualified counsel should review the final product configuration and customer terms.

## 14.1 Money transmission and virtual currency activity

Risk increases when Synaptix:

- Accepts and transmits cryptocurrency.
- Holds customer cryptocurrency.
- Transfers value to external wallets.
- Exchanges crypto.
- Administers a redeemable token.
- Processes user-to-user transfers.

Mitigation:

- Use provider-hosted checkout.
- Receive USD settlement.
- Do not custody crypto.
- Do not enable external withdrawals.
- Keep game currency closed-loop.
- Do not issue a redeemable public token.

## 14.2 New York virtual currency regulation

Synaptix has a New York nexus. Activities involving custody, transmission, exchange, or issuance of virtual currency require specialist legal review.

Mitigation:

- Operate as a merchant accepting a provider-supported payment method.
- Avoid becoming the administrator, custodian, or transmitter.
- Obtain New York digital-assets counsel before any redeemable-token or payout feature.

## 14.3 Securities and investment-contract exposure

Risk increases when a token is marketed around:

- Appreciation.
- Profit.
- Yield.
- Staking returns.
- Scarcity.
- Revenue sharing.
- Ecosystem growth.
- Secondary-market trading.

Mitigation:

- Do not market internal game credits as investments.
- Do not promise returns.
- Do not launch a public SNX token without formal securities analysis.

## 14.4 Gambling, contest, and sweepstakes exposure

Risk increases when combining:

- Consideration.
- Chance.
- Prizes.
- Cash-equivalent cryptocurrency.
- Paid entry.
- Randomized outcomes.

Mitigation:

- Avoid player-funded crypto prize pools.
- Keep Alpha/Beta rewards non-cash and platform-funded.
- Review trivia tournament structures separately.
- Apply age and geographic restrictions where required.

## 14.5 Consumer protection

Risk includes:

- Misleading token descriptions.
- Hidden fees.
- Unclear refund rules.
- Unclear exchange-rate effects.
- Confusing virtual currency with legal tender.
- Deceptive scarcity or urgency.

Mitigation:

- Use plain-language disclosures.
- Price goods in USD.
- Show the provider before redirect.
- Explain refund treatment.
- Avoid investment terminology.

## 14.6 Tax and accounting

Risk includes:

- Incorrect revenue recognition.
- Incomplete provider-fee records.
- Improper refund treatment.
- Sales-tax errors.
- Crypto basis accounting if Synaptix receives crypto.

Mitigation:

- Receive USD settlement.
- Export provider transaction reports.
- Reconcile fees and refunds.
- Review tax treatment with a qualified accountant.

## 14.7 Provider concentration and account risk

Risk includes:

- Account holds.
- Reserve requirements.
- Service suspension.
- Changed eligibility.
- Changed fees.
- Product restrictions.
- Geographic limitations.

Mitigation:

- Use a provider abstraction.
- Maintain card and ordinary PayPal alternatives.
- Add Stripe as a second provider after initial stabilization.
- Keep a documented provider-offboarding plan.

---

## 15. Observability and Audit Requirements

Use the existing observability stack where practical.

### Metrics

```text
payments_checkout_created_total
payments_checkout_failed_total
payments_webhook_received_total
payments_webhook_verification_failed_total
payments_capture_completed_total
payments_capture_denied_total
payments_fulfillment_completed_total
payments_fulfillment_failed_total
payments_duplicate_event_total
payments_reconciliation_exception_total
payments_refund_requested_total
payments_refund_completed_total
```

### Logs

Include:

- Correlation ID.
- Internal payment-order ID.
- Provider.
- Provider order ID.
- Provider event ID.
- Event type.
- Processing result.
- Error code.

Do not log:

- Secrets.
- Access tokens.
- Full payment credentials.
- Full user PII.
- Unredacted provider responses containing sensitive data.

### Alerts

Alert on:

- Webhook signature failures.
- Fulfillment failures.
- Reconciliation mismatches.
- Duplicate capture patterns.
- High refund volume.
- Provider API outage.
- Repeated payment creation failures.
- Abnormal purchase velocity.
- Unauthorized administrative refund attempts.

---

## 16. Testing Plan

## 16.1 Unit tests

Test:

- Product price resolution.
- Checkout creation.
- Provider error mapping.
- Amount validation.
- Currency validation.
- Idempotency.
- Duplicate webhook handling.
- Duplicate fulfillment handling.
- Refund state transitions.
- Reconciliation categorization.
- Authorization policies.

## 16.2 Integration tests

Test:

- Provider sandbox checkout.
- Webhook signature verification.
- Captured-payment fulfillment.
- Denied payment.
- Expired payment.
- Duplicate webhook.
- Out-of-order webhook.
- Provider timeout.
- Database rollback.
- Refund.
- Partial refund.
- Reconciliation recovery.

## 16.3 Security tests

Test:

- Forged webhook.
- Replayed webhook.
- Modified amount.
- Modified currency.
- Modified player ID.
- Unauthorized refund.
- Unauthorized admin reconciliation.
- Rate-limit bypass attempts.
- Secret exposure in logs.
- Duplicate concurrent fulfillment.
- Account takeover purchase scenario.

## 16.4 Failure-injection tests

Simulate:

- Provider capture succeeds but local database fails.
- Local order is created but provider creation fails.
- Webhook arrives before checkout response.
- Webhook arrives multiple times.
- Fulfillment succeeds but response times out.
- Provider refund succeeds but local update fails.
- Reconciliation runs during fulfillment.
- Two workers process the same payment concurrently.

---

## 17. Deployment Plan

## Phase 0: Disable direct crypto

Tasks:

- Set all crypto economy flags to disabled.
- Remove `Synaptix.CryptoService` from production Compose.
- Remove public routing to the crypto service.
- Remove treasury-secret requirements from production.
- Hide wallet, stake, unstake, withdrawal, and crypto prize-pool UI.
- Add automated tests proving the routes remain unavailable.

Exit criteria:

- No direct on-chain transfer path is reachable.
- No treasury key is required for deployment.
- No player can request an external withdrawal.
- No staking endpoint is available to ordinary clients.

## Phase 1: Provider-neutral payment foundation

Tasks:

- Add payment domain entities.
- Add EF Core mappings and migrations.
- Add provider abstraction.
- Add checkout endpoint.
- Add payment status endpoint.
- Add fulfillment service.
- Add payment-event persistence.
- Add idempotency constraints.
- Add audit logging.

Exit criteria:

- A fake provider can complete end-to-end test purchases.
- Duplicate webhook and fulfillment tests pass.
- Products are priced server-side.
- No client can alter amount or player identity.

## Phase 2: Standard PayPal checkout

Tasks:

- Add PayPal credentials and options.
- Add PayPal API client.
- Create hosted checkout.
- Add webhook verification.
- Add capture re-verification.
- Add refund support.
- Add reconciliation.

Exit criteria:

- Sandbox payment completes.
- Verified webhook fulfills exactly once.
- Refund completes and is recorded.
- Reconciliation identifies intentionally injected mismatches.

## Phase 3: PayPal Pay with Crypto

Tasks:

- Complete provider eligibility and merchant review.
- Enable crypto funding source where supported.
- Keep product prices in USD.
- Keep settlement in USD.
- Add crypto-payment disclosures.
- Add support procedures for crypto-funded refunds.
- Validate the exact provider event sequence.

Exit criteria:

- No Synaptix wallet is involved.
- No blockchain secret exists in Synaptix infrastructure.
- A crypto-funded sandbox or approved test transaction fulfills correctly.
- Customer receipts and support records identify the provider transaction.

## Phase 4: Stripe stablecoin fallback

Tasks:

- Implement Stripe adapter.
- Add provider routing configuration.
- Add provider availability controls.
- Add provider-specific reconciliation.
- Add operational runbooks.

Exit criteria:

- Provider can be selected by configuration.
- Business logic remains provider-neutral.
- Existing PayPal behavior is unaffected.

## Phase 5: Legal-review-only features

Do not implement without documented approval:

- External crypto payouts.
- Player wallet custody.
- Public SNX token.
- Token sale.
- Staking or yield.
- User-to-user transfer.
- Player-funded crypto prize pools.
- Crypto cash-out.
- Exchange functionality.
- Marketplace crypto settlement.

---

## 18. Suggested Configuration

```json
{
  "Payments": {
    "Enabled": true,
    "DefaultProvider": "PayPal",
    "AllowCryptoFundingSource": false,
    "AllowExternalCryptoWithdrawals": false,
    "AllowPlayerTransfers": false,
    "AllowStaking": false,
    "AllowCryptoPrizePools": false,
    "SettlementCurrency": "USD",
    "RequireProviderReverification": true,
    "Reconciliation": {
      "Enabled": true,
      "IntervalMinutes": 1440
    }
  },
  "Crypto": {
    "Enabled": false
  }
}
```

Environment variables:

```text
PAYMENTS__ENABLED=true
PAYMENTS__DEFAULTPROVIDER=PayPal
PAYMENTS__ALLOWCRYPTOFUNDINGSOURCE=false
PAYMENTS__ALLOWEXTERNALCRYPTOWITHDRAWALS=false
PAYMENTS__ALLOWPLAYERTRANSFERS=false
PAYMENTS__ALLOWSTAKING=false
PAYMENTS__ALLOWCRYPTOPRIZEPOOLS=false
PAYMENTS__SETTLEMENTCURRENCY=USD
CRYPTO__ENABLED=false

PAYPAL__CLIENTID=<secret-reference>
PAYPAL__CLIENTSECRET=<secret-reference>
PAYPAL__WEBHOOKID=<secret-reference>
PAYPAL__BASEURL=https://api-m.sandbox.paypal.com
```

Do not place actual secrets in `.env.example`.

---

## 19. Implementation Backlog

> **Status audit — 2026-07-24 (code-verified).** Checkboxes below reflect the
> actual state of the codebase, not the original plan intent. `[x]` = present in
> code; `[ ]` = not implemented; a trailing `— note` records partial coverage or
> where the implementation diverged from the plan's naming. Net: the **managed
> checkout / fulfillment / reconciliation / refund** path (Epics D–G) is largely
> built; the **direct-crypto teardown** (Epic A), the **unified provider
> abstraction** (Epic C), and the **security/legal** epics (H, I) are mostly
> outstanding. Epic B was delivered with a leaner data model than specified.

## Epic A: Disable direct on-chain production functionality — ❌ NOT STARTED

> Managed-payments work proceeded in parallel without executing this Phase 0
> teardown. `Crypto:Enabled` is still `true` in `appsettings.json` and the
> `/crypto/*` routes (link-wallet, withdraw, stake, unstake) are live (config-
> and secure-channel-gated, admin approval on withdrawals). Client crypto
> surfaces are gated behind account upgrade, not removed.

- [ ] Disable `.NET` crypto feature flags. — `Crypto:Enabled: true` in appsettings.
- [ ] Remove Python crypto service from production deployment. — still present.
- [ ] Block port 8300 from public access. — infra, not addressed.
- [ ] Remove treasury-key deployment requirements. — not addressed.
- [ ] Hide wallet-linking UI. — client route gated behind account upgrade, endpoint active.
- [ ] Hide withdrawal UI. — same (gated, not removed).
- [ ] Hide staking UI. — same (gated, not removed).
- [ ] Hide crypto prize-pool UI. — same (gated, not removed).
- [ ] Add release documentation.
- [ ] Add deployment assertions.

## Epic B: Payment domain — ✅ DONE (leaner model than specified)

> Delivered as `PaymentCheckoutAttempt` + `PaymentReconciliationIssue` plus the
> existing immutable `PlayerTransaction` ledger, rather than the four entities
> named here. The four named entities were **not** created; the functional
> outcome (persist payment state, refunds, fulfillment, reconciliation) is met.

- [ ] Create `PaymentOrder`. — replaced by `PaymentCheckoutAttempt`.
- [ ] Create `PaymentEvent`. — covered by `PlayerTransaction` + deterministic event ids.
- [ ] Create `PaymentRefund`. — refunds recorded as a `{provider}-refund` `PlayerTransaction` + `MarkRefunded`.
- [ ] Create `PaymentFulfillment`. — covered by `PlayerTransaction` + entitlement grants.
- [x] Add EF Core configurations. — for both new entities.
- [x] Add migrations. — `20260724134710_AddPaymentReconciliation`.
- [x] Add unique indexes. — unique `(Provider, ProviderRef)` on `PaymentCheckoutAttempt`.
- [ ] Add repositories. — access is via `IAppDb` (EF context) directly, no repository layer.
- [x] Add concurrency control. — idempotency via deterministic `EventId` + the unique index.

## Epic C: Provider abstraction — ⚠️ PARTIAL (per-provider gateways, no unified abstraction)

> Implemented as two provider-specific interfaces (`IPayPalPaymentGateway`,
> `IStripePaymentGateway`); endpoints branch on a provider string. There is no
> unified `IPaymentProvider` / resolver / provider-neutral model layer.

- [ ] Create `IPaymentProvider`. — separate PayPal/Stripe gateways instead.
- [ ] Create provider resolver/factory. — endpoints switch on the provider string.
- [x] Create fake provider for tests. — fake gateways in Store*/AdminPayments endpoint tests.
- [ ] Add provider-neutral models. — provider-specific DTOs.
- [ ] Add error normalization. — only the shared `ApiResponses.Error` envelope.
- [x] Add provider availability checks. — `GET /store/system/status` reports enabled providers.

## Epic D: Checkout — ✅ DONE (minor gaps)

- [x] Add checkout endpoint. — Stripe `/store/payments/checkout/session`, PayPal `/store/payments/paypal/order`.
- [x] Resolve price server-side. — from `StoreItems`.
- [x] Add idempotency. — deterministic `CreateDeterministicGuid` event ids.
- [x] Add return and cancel URL validation. — absolute-URI validation.
- [ ] Add payment status endpoint. — no per-payment status GET (capture returns status; subscription status exists).
- [x] Add client contracts. — mobile (Flutter) + web-companion clients wired.
- [ ] Add API documentation. — Swagger tags only, no dedicated payment API doc.

## Epic E: PayPal — ✅ DONE except HTTP resilience

> `PayPalPaymentGateway` covers the API surface. Gap: it uses a default
> `IHttpClientFactory.CreateClient()` with no resilience handler, so there are
> no retry/circuit-breaker/timeout policies on the PayPal HTTP calls.

- [x] Add OAuth token client. — `/v1/oauth2/token`.
- [x] Add order creation. — `CreateOrderAsync`.
- [x] Add order retrieval. — `GetOrderAsync`.
- [x] Add capture status parsing. — `CaptureOrderAsync`.
- [x] Add webhook verification. — `VerifyWebhookAsync`, called on the webhook route.
- [x] Add refund support. — `RefundCaptureAsync`.
- [x] Add sandbox configuration. — `PayPalOptions.BaseUrl` (sandbox default).
- [ ] Add retry policies. — default HttpClient, no resilience handler.
- [ ] Add circuit breaker. — not configured.
- [ ] Add timeout handling. — no per-call timeout set.

## Epic F: Fulfillment — ✅ DONE

- [x] Add entitlement fulfillment. — `IEntitlementService.GrantAsync`.
- [x] Add closed-loop currency fulfillment. — grants via `StoreItem.ItemType`/`GrantQuantity`.
- [x] Add immutable ledger integration. — `PlayerTransaction`.
- [x] Add duplicate protection. — deterministic event ids + the PayPal double-fulfillment fix.
- [x] Add transactional processing. — `SaveChangesAsync` within the request.
- [x] Add retry workflow. — admin `retry-fulfillment` endpoint.
- [x] Add compensation workflow. — full refund revokes granted entitlements.

## Epic G: Reconciliation and admin — ✅ DONE except runbook

> Operator UI added in the React dashboard (`features/payments`).

- [x] Add scheduled reconciliation. — daily Hangfire `PaymentReconciliationJob`.
- [x] Add mismatch categories. — `ProviderCapturedFulfillmentMissing`, `AmountMismatch`, `CurrencyMismatch`.
- [x] Add admin payment search. — `GET /admin/payments`.
- [x] Add manual reconcile. — `POST /admin/payments/{id}/reconcile`.
- [x] Add retry fulfillment. — `POST /admin/payments/{id}/retry-fulfillment`.
- [x] Add controlled refund. — `POST /admin/payments/{id}/refund` (entitlement reversal on full refund).
- [x] Add financial audit log. — `AdminAuditLogger` on every payment action.
- [ ] Add operator runbook. — no payments-specific runbook doc yet.

## Epic H: Security — ⚠️ MOSTLY OUTSTANDING

> Note: the payment endpoints ride the existing KMS secure channel
> (`RequireSecureChannel`) + admin ops-key/role gating, which covers transport
> and admin auth, but the payment-specific security items below are largely not
> addressed.

- [ ] Store secrets in a managed secret system. — provider secrets are env-var placeholders, not a vault.
- [x] Add webhook replay protection. — signature verification + idempotent deterministic event ids make replays harmless.
- [ ] Add administrative MFA policy.
- [ ] Add least-privilege provider roles.
- [ ] Add payment-specific rate limits. — no `RequireRateLimiting` on payment routes.
- [ ] Add structured redaction.
- [x] Add security tests. — `StorePayPalEndpointsTests`, `StoreStripePaymentEndpointsTests`, `AdminPaymentsEndpointsTests`.
- [ ] Complete threat model.

## Epic I: Legal and policy — ❌ NOT STARTED (non-engineering, except in-app disclosure)

- [ ] Update Terms of Service.
- [ ] Update Privacy Policy.
- [ ] Add virtual-currency disclosures. — in-app PayPal crypto-funding disclosure string exists; no published policy.
- [ ] Add refund policy.
- [ ] Add minor-purchase controls.
- [ ] Review contest and prize mechanics.
- [ ] Obtain New York digital-assets counsel review.
- [ ] Obtain accounting review.

---

## 20. Definition of Done

The managed crypto payment capability is production-ready only when all of the
following are true. **Status as of the 2026-07-24 audit** (for the managed
PayPal/Stripe path):

- [x] Synaptix does not hold customer crypto. — managed path settles in USD.
- [x] Synaptix does not hold blockchain private keys. — managed path; (caveat: the still-enabled direct-crypto service is separate — see Epic A).
- [x] Synaptix does not sign blockchain transactions. — managed path.
- [x] Synaptix receives USD settlement.
- [x] All products are priced server-side in USD. — from `StoreItems`.
- [x] Client callbacks cannot grant entitlements. — fulfillment is server-side (capture/webhook).
- [x] All webhook signatures are verified. — PayPal `VerifyWebhookAsync`; Stripe `EventUtility.ConstructEvent` (caveat: Stripe falls back to unverified parse if no `WebhookSecret` configured).
- [x] Duplicate events are harmless. — deterministic event ids.
- [x] Duplicate fulfillment is prevented by database constraints. — unique `(Provider, ProviderRef)` + unique `PlayerTransaction.EventId`.
- [x] Refunds are controlled and auditable. — admin refund endpoint + `AdminAuditLogger`.
- [x] Daily reconciliation is operational. — Hangfire `PaymentReconciliationJob`.
- [ ] Monitoring and alerting are configured. — payment-specific metrics/alerts not verified.
- [ ] Terms and disclosures are published. — only the in-app crypto-funding disclosure exists.
- [ ] Direct withdrawal and staking routes are disabled. — **not disabled**; `Crypto:Enabled: true`, routes live (Epic A).
- [x] Closed-loop credits are non-transferable and non-redeemable. — coins/diamonds are non-redeemable by design.
- [ ] Security testing is complete. — endpoint tests exist; threat model / payment security suite not complete (Epic H).
- [ ] Provider sandbox testing is complete. — sandbox config present; end-to-end sandbox sign-off not recorded.
- [ ] Legal review is complete for the final product configuration.

---

## 21. Claude/Codex Implementation Prompt

Use the following prompt with Claude Code or Codex after providing repository access.

```text
You are implementing a managed payment architecture in the GitHub repository:

devartblake/TycoonTycoon_Backend

Objective:
Replace the production use of the existing direct on-chain cryptocurrency settlement architecture with a provider-managed payment system. SynaptixPlay must not custody cryptocurrency, store blockchain private keys, sign blockchain transactions, operate user crypto withdrawals, enable staking, or issue a redeemable public token during Alpha/Beta.

Primary provider:
PayPal. Design the implementation so Stripe can be added later through a provider-neutral abstraction.

Required architecture:
1. Keep the existing Synaptix.CryptoService code disabled and excluded from production deployment.
2. Ensure Crypto:Enabled and crypto_enabled remain false in Alpha/Beta production configuration.
3. Add a provider-neutral payment domain to the .NET backend.
4. Add PaymentOrder, PaymentEvent, PaymentFulfillment, and PaymentRefund persistence with EF Core.
5. Add unique constraints that prevent duplicate provider events and duplicate fulfillment.
6. Add IPaymentProvider and a PayPal implementation.
7. Add a server-authoritative checkout endpoint.
8. Price products exclusively from the backend catalog.
9. Add a PayPal webhook endpoint that:
   - reads the raw body,
   - validates the provider signature,
   - detects replay and duplicate events,
   - retrieves the order or capture directly from PayPal,
   - verifies amount, currency, merchant, status, and internal order reference,
   - fulfills exactly once.
10. Integrate fulfillment with the existing PlayerTransaction ledger.
11. Treat purchased Synaptix credits as closed-loop, non-transferable, and non-redeemable.
12. Do not use CRYPTO_UNITS for ordinary purchased game currency. Introduce neutral closed-loop item types such as wallet:premium-credits or wallet:gems.
13. Add refund support.
14. Add daily reconciliation.
15. Add protected administrative endpoints for payment inspection, reconciliation, fulfillment retry, and refund.
16. Add structured logs, metrics, audit records, and redaction.
17. Add unit, integration, concurrency, replay, duplicate-event, and failure-injection tests.
18. Add Docker and configuration changes.
19. Add documentation and an operator runbook.
20. Do not commit secrets.

Implementation method:
- First inspect the current repository structure, DbContext abstractions, PlayerTransaction model, feature-flag system, authorization policies, Docker Compose files, existing observability stack, and test conventions.
- Produce an implementation plan mapped to exact repository paths.
- Implement in small, reviewable commits or patches.
- Preserve existing architecture conventions.
- Do not silently delete experimental crypto code; isolate and disable it.
- Flag any legal-policy assumption as a configuration or documentation requirement, not as legal advice.

Required acceptance tests:
- Forged webhook is rejected.
- Replayed webhook does not duplicate fulfillment.
- Concurrent duplicate webhooks do not duplicate fulfillment.
- Client-supplied price changes are ignored.
- Client-supplied player identity changes are ignored.
- A provider capture with the wrong amount or currency is not fulfilled.
- A valid capture produces exactly one PlayerTransaction grant.
- A provider success followed by a local failure is recoverable through reconciliation.
- Refunds are recorded and entitlement reversal follows the configured product policy.
- Direct withdrawal, staking, and on-chain settlement remain disabled in Alpha/Beta.

Before making changes:
Return:
1. Repository findings.
2. Exact files to add.
3. Exact files to modify.
4. Database migration plan.
5. Security threat model.
6. Ordered implementation phases.
7. Risks or assumptions that require owner review.
```

---

## 22. Recommended Immediate Next Commit Sequence

### Commit 1

```text
chore(crypto): disable direct on-chain features for alpha
```

Scope:

- Feature flags.
- Production configuration.
- Docker Compose.
- UI/API exposure documentation.
- Tests proving direct crypto is unavailable.

### Commit 2

```text
feat(payments): add provider-neutral payment domain
```

Scope:

- Entities.
- EF Core mappings.
- Repositories.
- Migration.
- Interfaces.
- Fake provider tests.

### Commit 3

```text
feat(payments): add server-authoritative checkout and fulfillment
```

Scope:

- Checkout endpoint.
- Status endpoint.
- Fulfillment.
- Idempotency.
- Ledger integration.

### Commit 4

```text
feat(paypal): add hosted checkout and verified webhooks
```

Scope:

- PayPal client.
- Authentication.
- Checkout creation.
- Webhook validation.
- Provider re-verification.
- Tests.

### Commit 5

```text
feat(payments): add refunds and reconciliation
```

Scope:

- Refund workflow.
- Scheduled reconciliation.
- Administrative endpoints.
- Alerts.
- Runbook.

### Commit 6

```text
docs(payments): add managed crypto payment operations and compliance guardrails
```

Scope:

- Architecture.
- Deployment.
- Incident response.
- Refund operations.
- Provider outage handling.
- Legal-review gates.

---

## 23. Final Recommendation

The correct Alpha/Beta strategy is not to make SynaptixPlay a cryptocurrency platform. It is to make SynaptixPlay a digital-goods merchant that offers a provider-managed crypto-funded payment option.

The safest practical boundary is:

```text
Provider handles cryptocurrency.
Synaptix receives USD.
Synaptix grants closed-loop digital goods.
No custody.
No withdrawal.
No staking.
No public token.
No player-funded crypto wagering.
```

This approach materially reduces the surface area for:

- Private-key theft.
- Treasury loss.
- Blockchain settlement errors.
- Custody exposure.
- Money-transmission risk.
- Token-administration risk.
- Crypto accounting complexity.
- Operational reconciliation failures.
- Consumer confusion.

It does not eliminate all legal or commercial liability. Synaptix remains responsible for its own application security, product design, disclosures, fulfillment, refunds, taxes, customer support, privacy obligations, contest design, and provider-contract compliance.
