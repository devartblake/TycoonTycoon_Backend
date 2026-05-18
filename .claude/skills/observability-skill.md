---
project: TycoonTycoon_Backend / Synaptix
mode: Claude Code
priority: Alpha/Beta before June 1, without damaging long-term platform architecture
---

# Observability Skill

Use this skill when adding logs, metrics, traces, or health checks.

## Signals

### Logs

Use structured logs. Include correlation IDs. Do not log secrets.

### Metrics

Use low-cardinality labels. Track counts, latency, failures, and dependency state.

### Traces

Trace external calls, database operations, sidecar calls, reward mutations, and background jobs.

### Health Checks

- Liveness: process is alive.
- Readiness: required dependencies are available.
- Optional dependencies should not block readiness unless required for the current profile.

## Alpha Focus

Instrument:

- startup
- migrations
- DB connection
- auth failure
- wallet/reward mutation
- sidecar fallback
- MinIO seed failures
- background jobs
