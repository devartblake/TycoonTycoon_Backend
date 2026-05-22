# Codex Heartbeat System for Synaptix Backend

This package defines a repo-local Codex Heartbeat workflow for `TycoonTycoon_Backend`.

Purpose:
- Keep long-running Codex work observable.
- Track Alpha/Beta readiness before June 1.
- Record verification results.
- Prevent agent drift.
- Surface blockers instead of guessing.
- Preserve long-term architecture while shipping Alpha/Beta.

Use this even if your Codex environment does not expose an official feature named `Heartbeat`; this package acts as the project-level protocol.
