# Admin Security Metrics

This document defines the operational metrics introduced for admin hardening.

## Meter
- `Tycoon.Backend.Api.AdminSecurity`

## Counters
- `admin_auth_events_total`
  - tags: `action` (`login|refresh|me`), `outcome`
- `admin_notification_events_total`
  - tags: `action` (`send|schedule`), `outcome`
- `admin_audit_events_total`
  - tags: `action`
- `admin_rate_limit_rejected_total`
  - tags: `path`

## Histogram
- `admin_request_latency_ms`
  - tags: `area` (`auth|notifications`), `action`, `outcome`

## Suggested alerts
- High `admin_auth_events_total{outcome="unauthorized"}` over rolling 5m window.
- High `admin_rate_limit_rejected_total` sustained for admin auth routes.
- Increased `admin_notification_events_total{outcome="not_found"}` indicating config drift.

## Alert thresholds (starting points)
- `admin_auth_events_total{outcome="unauthorized"}`: alert at `>= 20/min` over 5m for 10m.
- `admin_rate_limit_rejected_total{path="/admin/auth/login|/admin/auth/refresh|/admin/notifications/send"}`: alert at `>= 10/min` over 5m.
- `admin_notification_events_total{outcome="not_found"}`: alert at `>= 5/min` over 15m for 15m.

## Runbook
- Incident response: `ops/runbooks/admin-security-incidents.md`.
