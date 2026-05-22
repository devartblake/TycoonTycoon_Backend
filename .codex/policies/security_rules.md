# Security Rules

## Security baseline

- Never commit secrets, tokens, presigned URLs, passwords, or real keys.
- Use environment variables or typed options for secrets.
- Keep KMS boundaries explicit.
- Do not invent custom cryptography.
- Prefer proven platform primitives: TLS, JWT validation, envelope encryption, key rotation, audit logs, rate limits.
- Admin endpoints must be protected by explicit policy or ops key.
- Sidecar recommendations must not directly mutate authoritative game/economy state.

## Alpha security priorities

- Auth works.
- Admin operations are gated.
- Wallet/economy mutation is server authoritative.
- Reward claims are idempotent.
- Logs do not leak secrets.
- KMS project compiles and has test coverage for critical contracts.
