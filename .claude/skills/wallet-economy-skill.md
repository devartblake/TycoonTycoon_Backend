---
project: TycoonTycoon_Backend / Synaptix
mode: Claude Code
priority: Alpha/Beta before June 1, without damaging long-term platform architecture
---

# Wallet Economy Skill

Use this skill when changing coins, XP, diamonds, rewards, store purchases, or match economy.

## Principles

- Backend is authoritative.
- Client never directly mints rewards.
- Wallet reads should be consistent.
- Reward writes should be idempotent.
- Duplicates must not double-pay.
- Anti-cheat may block rewards.
- Economy mutations should be observable.

## Alpha Required Flows

- Read authenticated wallet.
- Submit match result.
- Calculate reward server-side.
- Persist reward atomically.
- Return updated balance.
- Reject or no-op duplicate submission.
- Protect admin/economy endpoints.

## Checklist

- Is there an idempotency key?
- Is reward calculation server-side?
- Is the transaction safe?
- Are duplicate requests handled?
- Is audit/logging sufficient?
- Are tests in place?
