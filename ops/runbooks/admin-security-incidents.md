# Admin Security Incident Runbook

This runbook describes first-response actions for alerts emitted from the
`Tycoon.Backend.Api.AdminSecurity` meter.

## Alert: admin auth unauthorized spike

**Signal**
- `admin_auth_events_total{outcome="unauthorized"}`

**Default threshold**
- Trigger when 5-minute rate is `>= 20/min` for 10 consecutive minutes.

**Triage**
1. Check dashboard breakdown by `action` (`login`, `refresh`, `me`) to identify source route.
2. Correlate with `admin_rate_limit_rejected_total` for auth endpoints.
3. Confirm whether attempts are from expected admin IP ranges (if ingress/WAF logs available).

**Mitigation**
1. Rotate `Admin:OpsKey` if suspected leak.
2. Tighten upstream WAF/IP allowlist and temporary rate limits.
3. Confirm no privileged JWT audience/issuer misconfiguration in recent deploy.

## Alert: admin rate-limit reject spike

**Signal**
- `admin_rate_limit_rejected_total`

**Default threshold**
- Trigger when 5-minute rate is `>= 10/min` on any of:
  - `/admin/auth/login`
  - `/admin/auth/refresh`
  - `/admin/notifications/send`

**Triage**
1. Identify top offending `path` tag.
2. Verify if traffic aligns with known maintenance/admin scripts.
3. Check for correlated auth failures and dead-letter growth in notifications.

**Mitigation**
1. For legitimate load, increase limit cautiously and time-box the override.
2. For abuse, block source ranges and keep stricter limits.
3. Record incident context in security audit records for postmortem.

## Alert: admin notification not-found drift

**Signal**
- `admin_notification_events_total{outcome="not_found"}`

**Default threshold**
- Trigger when 15-minute rate is `>= 5/min` for 15 consecutive minutes.

**Triage**
1. Inspect failing `action` (`send` or `schedule`).
2. Validate channel key presence and config consistency across environments.
3. Confirm recent deploy/migration did not change channel seeds/config.

**Mitigation**
1. Re-seed/fix missing channel config.
2. Replay dead-letter entries after config correction.
3. Capture root cause and update rollout checklist.

## Post-incident checklist

1. Add summary in team incident log.
2. Link affected dashboard screenshot and metric query.
3. If threshold was noisy, tune threshold in docs/alerts and note rationale.
