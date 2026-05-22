# Skill: Observability

Use this skill for logs, metrics, tracing, health checks, and dashboards.

## Procedure

1. Identify the operational question the signal must answer.
2. Add structured logs with correlation/context.
3. Add metrics only if they will be used.
4. Add traces around network/DB/queue boundaries.
5. Keep health checks separated into liveness and readiness when possible.
6. Avoid logging secrets or PII.

## Alpha signals

- API startup
- DB migration success/failure
- auth failures
- wallet mutations
- match submit failures
- MinIO seed loading
- sidecar timeout/fallback
