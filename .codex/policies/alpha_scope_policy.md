# Alpha Scope Policy

## Mission

Optimize all Codex work for a stable Alpha/Beta release before June 1 while preserving the future Synaptix platform architecture.

## Priority labels

Every task must classify work as:

- P0 Alpha blocker: required to start, login, play, persist, or verify core backend flows.
- P1 Alpha important: needed for confidence, testing, monitoring, or release safety.
- P2 Post-Alpha: useful, but not required for Alpha/Beta.
- P3 Long-term platform: architecture investment that should not block Alpha.

## Default decision rules

- Feature flag unfinished modules instead of deleting them.
- Do not expand scope unless the missing work blocks P0/P1.
- Prefer one stable happy path over several unfinished modes.
- Preserve existing contracts unless a break is explicitly required.
- Add tests around changed behavior before broad refactors.
- Do not introduce new infrastructure unless existing infrastructure cannot satisfy the task.
- Treat local Docker as the integration truth.
