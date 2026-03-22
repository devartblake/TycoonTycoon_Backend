# Flutter Frontend Implementation Plan (Game Balance / Economy)

This document is an execution-ready plan for building the Flutter client side of the new game-balance system.

Use this with **ChatGPT**, **Claude Code**, or **Codex** as a source-of-truth task spec.

---

## 1) Objective

Implement Flutter UX + client integration for:

- Energy-aware mode entry (`casual`, `ranked`, `guardian`, `jackpot`)
- Safeguards (first-session discount, daily free jackpot ticket, pity state visibility)
- Operator-driven config compatibility (values can change server-side)
- Monetization hooks (revive quote, ticket status, offer-ready telemetry)

---

## 2) Backend Endpoints to Integrate

### Mobile economy endpoints

- `GET /mobile/economy/state`
- `POST /mobile/economy/session/start?playerId={uuid}`
- `POST /mobile/economy/daily-jackpot-ticket/claim?playerId={uuid}`
- `POST /mobile/economy/revive/quote?playerId={uuid}&almostWin={bool}`
- `POST /mobile/economy/pity/report-loss?playerId={uuid}`
- `POST /mobile/economy/pity/report-win?playerId={uuid}`

### Match start endpoint (policy-enforced)

- `POST /mobile/matches/start`
  - Handle `409 CONFLICT` as a user-facing “cannot enter mode” state.

---

## 3) Flutter Architecture Recommendation

Use this structure:

```text
lib/
  features/economy/
    data/
      economy_api.dart
      economy_dtos.dart
      economy_repository_impl.dart
    domain/
      economy_repository.dart
      models/
    presentation/
      controllers/
      pages/
      widgets/
```

### State management

Pick one and standardize:
- Riverpod (recommended)
- Bloc/Cubit
- Provider (only if project already committed)

### Core contracts

Create client models for:
- Economy state (energy + modes + safeguards)
- Session-start adjusted costs
- Daily ticket claim response
- Revive quote response
- Pity response

---

## 4) UX Surfaces to Build

## 4.1 Economy HUD (global)

Display:
- Current energy / max
- Regen rate text (`+1 every X min`)
- Daily ticket availability

Refresh triggers:
- app resume
- mode entry attempts
- post-match
- manual pull-to-refresh

## 4.2 Mode Entry Cards

For each mode card:
- Show cost (energy or ticket)
- Show “discounted” badge when session-start discount is active
- Disable CTA if unavailable (and show why)

CTA sequence:
1. Call `POST /mobile/economy/session/start`
2. Render adjusted costs
3. If eligible, call `/mobile/matches/start`
4. On `409`, map to user-friendly reason

## 4.3 Jackpot Ticket Panel

- “Claim Daily Ticket” button
- Remaining ticket count today
- Disabled state + tooltip after limit reached

## 4.4 Revive Sheet

- Call `POST /mobile/economy/revive/quote` before rendering purchase
- Show base cost vs discounted cost when `almostWin=true`

## 4.5 Pity Feedback Integration

- After loss: call `pity/report-loss`
- After win: call `pity/report-win`
- Show subtle hint when pity active (no overexposure)

---

## 5) Error Handling Contract

Handle these explicitly:

- `403 FORBIDDEN`: moderation/enforcement block
- `409 CONFLICT`: policy denial (insufficient energy / ticket)
- network timeout / 5xx: retry + fallback toast

Map to deterministic UX copy:
- “Not enough energy”
- “No ticket available”
- “Mode unavailable right now”

Never expose raw backend exception text directly.

---

## 6) Telemetry Events (Flutter)

Emit client analytics events:
- `economy_state_loaded`
- `mode_entry_attempted`
- `mode_entry_blocked`
- `daily_ticket_claimed`
- `daily_ticket_denied`
- `revive_quote_loaded`
- `pity_state_changed`

Include fields:
- `playerId`
- `mode`
- `reasonCode`
- `energyCostApplied`
- `ticketConsumed`

---

## 7) Phased Delivery Plan (Frontend)

## Phase F1 (MVP wiring)

- Economy API client + DTOs
- Economy HUD
- Mode entry flow with policy conflict handling

## Phase F2 (Safeguards UX)

- Daily ticket claim UX
- Revive quote sheet
- Pity signal integration

## Phase F3 (Polish + resilience)

- Offline/retry UX
- Cached state hydration
- Enhanced analytics + A/B flags

---

## 8) Definition of Done

- User can see real-time economy state from backend
- User cannot enter blocked mode without clear reason
- Ticket claim + revive quote + pity calls are integrated
- All failure states are user-safe and localized
- Integration tests cover core success/failure flows

---

## 9) Prompt Templates for AI Coding Tools

## Template A — ChatGPT/Codex

> Implement Flutter economy client integration using this plan. Start with Phase F1 only. Create DTOs, repository, state controller, and a basic Economy HUD + mode entry screen. Use clean architecture and include tests for repository parsing + conflict mapping.

## Template B — Claude Code

> Follow `docs/FLUTTER_GAME_BALANCE_IMPLEMENTATION_PLAN.md`. Implement Phase F2 on top of existing Phase F1: daily ticket claim UI, revive quote UI, and pity reporting hooks. Keep API contract compatible with backend endpoints and add widget tests.

## Template C — Any Agent (incremental)

> Read `docs/FLUTTER_GAME_BALANCE_IMPLEMENTATION_PLAN.md` and complete only one phase in this PR. Include: code, tests, migration notes (if any), and a checklist of completed bullets from the plan.

---

## 10) Implementation Notes for Humans

- Keep all economy numbers server-driven.
- Avoid embedding energy constants in Flutter.
- Treat policy as authoritative; frontend only reflects/requests.
- Expect server values to evolve during live tuning.
