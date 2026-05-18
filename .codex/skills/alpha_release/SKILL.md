# Skill: Alpha/Beta Release Execution

Use this skill when preparing, triaging, or implementing work for the June 1 Alpha/Beta target.

## Procedure

1. Classify the task as P0, P1, P2, or P3.
2. Identify the affected bounded context.
3. Determine whether the feature should be enabled, disabled, or feature-flagged for Alpha.
4. Identify API, DB, Docker, auth, and test impact.
5. Implement the smallest production-safe change.
6. Run relevant verification.
7. Report what changed, what passed, what failed, and what remains.

## Release bias

Prefer stability over breadth. Do not build optional gameplay systems if wallet, auth, match submit, catalog, migrations, or Docker startup are unstable.
