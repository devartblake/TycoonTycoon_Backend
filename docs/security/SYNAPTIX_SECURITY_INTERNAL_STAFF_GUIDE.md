# Synaptix Security System - Internal Staff Guide

This guide explains how the Synaptix security system protects sensitive player and operator actions. It is written for internal staff and avoids code-level implementation details.

---

## Purpose

The Synaptix security system adds an extra protection layer around sensitive actions, even though normal transport security is already required.

It protects actions such as:

- Account session refresh
- Admin sign-in and admin session refresh
- Wallet and crypto-related actions
- Store purchases and payment preparation
- Receipt validation
- Other high-impact account or economy actions

The goal is to make sure sensitive requests are current, belong to the authenticated user, cannot be replayed, and cannot be moved from one protected action to another.

---

## How It Works At A Staff Level

1. A player or operator signs in normally.
2. Before a protected action is sent, the client establishes a short-lived secure session.
3. The protected request is wrapped so only the intended backend security service can verify and open it.
4. Each protected request carries freshness information.
5. The backend checks that the request:
   - belongs to the active secure session,
   - belongs to the authenticated user,
   - has not been used before,
   - was created recently,
   - matches the exact action being attempted.
6. If the checks pass, the protected action continues.
7. If any check fails, the action is rejected before the protected business operation runs.

This means a captured or copied protected request should not be useful later, for another user, or for a different action.

---

## What Changed In The Latest Hardening Pass

The latest backend hardening completed the replay and request-binding work for protected actions.

The system now rejects:

- Missing freshness metadata.
- Invalid request ordering metadata.
- Reused request metadata.
- Protected requests created outside the allowed time window.
- Requests tied to the wrong authenticated user.
- Requests whose protected body does not match the action context.

This improves protection for payment, store, wallet, admin, and session-refresh workflows.

---

## What Internal Teams Should Know

### Support

If a player reports that a sensitive action failed, collect:

- Player account identifier.
- Approximate time of the action.
- Device/platform.
- The screen or action the player was using.
- Whether retrying after signing in again resolved the issue.

Do not ask players for private session data, payment credentials, or raw diagnostic request data.

### QA

When testing protected flows, verify:

- A normal fresh request succeeds.
- Refreshing or retrying through the app works cleanly.
- Reusing an old protected action fails.
- Signing out and signing back in restores normal behavior.
- Payment and store actions do not apply twice after a retry.

QA evidence should include screenshots, timestamps, test account IDs, and result summaries. Do not attach private session data or raw protected request bodies.

### Operations

When monitoring protected flows, watch for:

- Sudden increases in protected-request rejection rates.
- Repeated replay-style failures from the same account or device.
- Admin login failures after deployment.
- Payment or store checkout failures after frontend releases.
- Time synchronization issues across client, backend, and hosting environments.

Escalate repeated replay or subject-mismatch failures as possible abuse, stale-client behavior, or clock drift.

### Frontend Coordination

The frontend team must keep protected action handling aligned with the backend security contract. The client should send fresh protected-action metadata for each sensitive request and should not reuse protected request envelopes.

If a user signs out, changes account, or a secure session expires, the client should establish a new secure session before retrying protected actions.

---

## Expected User Experience

Most players should never notice the security layer.

When something goes wrong, the app should show a calm retry or sign-in-refresh experience rather than exposing internal security language.

Recommended user-facing language:

- "We could not verify this action. Please try again."
- "Your session needs to be refreshed before continuing."
- "This action could not be completed safely. Please sign in again."

Avoid showing internal security or infrastructure terms to players.

---

## Current Status

Completed:

- Backend protected-action enforcement.
- Short-lived secure session support.
- Request freshness enforcement.
- Replay rejection.
- User/session binding.
- Request-context binding.
- Local backend verification tests.

Still coordinated separately:

- Frontend secure-channel integration.
- Live staging evidence.
- Production secret-management hardening.
- Post-Alpha hybrid security roadmap.

---

## Escalation

Escalate to Backend/Security when:

- Protected store or payment actions fail repeatedly.
- Admin users cannot sign in after deployment.
- Logs show repeated replay or ownership mismatch failures.
- A client release changes protected request behavior.
- Clock drift or environment time mismatch is suspected.

Escalate to Product/Support when:

- The issue affects player-facing purchases or account access.
- The app shows confusing or overly technical error messages.
- Multiple players report the same protected-action failure.
