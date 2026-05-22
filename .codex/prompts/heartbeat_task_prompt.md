# Heartbeat Task Prompt

Use this prompt when asking Codex to perform any multi-step Synaptix backend task.

## Prompt

You are working in `TycoonTycoon_Backend`.

Use the Codex Heartbeat Protocol before and during this task.

Task:
`<describe task>`

Priority:
`P0 Alpha blocker | P1 Alpha important | P2 Post-Alpha | P3 Long-term platform`

Requirements:
- Update `.codex/heartbeat/current-task.md`.
- Record verification in `.codex/heartbeat/verification-log.md`.
- Update `.codex/heartbeat/current-blockers.md` if blocked.
- Update `.codex/heartbeat/alpha-status.md` if this affects Alpha/Beta readiness.
- Make the smallest production-safe change.
- Preserve Clean Architecture.
- Run relevant verification or explain why it could not run.

Final response must include:
1. Task status.
2. Files changed.
3. Verification results.
4. Blockers.
5. Alpha/Beta impact.
6. Next recommended task.
