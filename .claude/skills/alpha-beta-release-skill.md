---
project: TycoonTycoon_Backend / Synaptix
mode: Claude Code
priority: Alpha/Beta before June 1, without damaging long-term platform architecture
---

# Alpha/Beta Release Skill

Use this skill when planning, reducing scope, or validating work for the June 1 Alpha/Beta release.

## Goal

Deliver a stable Alpha/Beta release with minimum viable backend capability and no destructive architecture shortcuts.

## Release Gate Categories

### P0 Required

- Local Docker stack boots.
- API starts.
- Database migrations apply.
- Auth/session flow works.
- `/users/me/wallet` works.
- Match submission is idempotent.
- Reward minting is server-authoritative.
- Store/mission/skill seed data is available or safely feature-flagged.
- Admin/security-sensitive endpoints are protected.
- Smoke tests or contract tests exist for critical endpoints.

### P1 Important

- Observability for startup, migrations, auth, rewards.
- Contract tests for Flutter-facing endpoints.
- Sidecar fallback behavior.
- MinIO seed validation.
- Feature flags documented.

### P2 Post-Alpha

- Full personalization.
- advanced analytics.
- dashboard polish.
- complex economy balancing.
- extensive event streaming.

### P3 Long-Term

- multi-tenant scaling.
- advanced KMS lifecycle.
- automated deployment pipelines.
- full observability dashboards.
- cross-platform monetization systems.

## Procedure

1. Classify the request.
2. Identify if the work is Alpha-critical.
3. Recommend build, defer, or feature-flag.
4. Define acceptance criteria.
5. Add verification commands.

## Output

```md
## Release Classification
Priority:
Decision:

## Why
-

## Minimum Alpha Scope
-

## Deferred Scope
-

## Acceptance Criteria
-

## Verification
-
```
