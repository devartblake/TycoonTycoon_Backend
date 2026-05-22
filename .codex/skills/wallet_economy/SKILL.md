# Skill: Wallet and Economy Authority

Use this skill for coins, XP, diamonds, rewards, store purchases, missions, skills, and match payout logic.

## Rules

- `/users/me/wallet` is the authoritative wallet read endpoint.
- All wallet mutations must be server authoritative.
- Reward claims must be idempotent.
- Anti-cheat/moderation decisions must be able to block rewards.
- Client-provided balances are never trusted.
- Use transaction boundaries for balance changes.

## Procedure

1. Identify the economy event.
2. Confirm idempotency key or natural uniqueness.
3. Validate player/account status.
4. Apply anti-cheat/moderation gate.
5. Mutate balance in one authoritative service.
6. Persist audit/reason metadata.
7. Return new balance snapshot.
