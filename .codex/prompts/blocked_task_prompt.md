# Blocked Task Prompt

Use this when Codex is blocked.

Required behavior:
- Do not guess.
- Do not keep changing unrelated files.
- Update `.codex/heartbeat/current-task.md` to `blocked`.
- Add a blocker entry to `.codex/heartbeat/current-blockers.md`.
- Record skipped/failed verification in `.codex/heartbeat/verification-log.md`.
- Provide the concrete decision or missing input needed to continue.
