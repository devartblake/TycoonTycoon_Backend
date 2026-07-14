# Alpha Launch Alerts (Minimum Set)

**Purpose:** Make existing OpenTelemetry / Prometheus / Sentry signal **actionable** for Alpha soft-launch.  
**Related:** [`docs/observability/admin-security-metrics.md`](../../docs/observability/admin-security-metrics.md), [`admin-security-incidents.md`](admin-security-incidents.md)  
**Program:** Track B / H6 in [`docs/status/BCE_EXECUTION_PLAN.md`](../../docs/status/BCE_EXECUTION_PLAN.md)

---

## Who gets paged (fill for your environment)

| Role | Primary | Backup | Channel |
|------|---------|--------|---------|
| On-call engineer | _Slack_ | _TBD_ | _Slack/PagerDuty_ |
| Backend lead | _LMX-BLADE_ | _TBD_ | _email_ |
| Ops / DevOps | _LMX-BLADE_ | _TBD_ | _email_ |
| Product (inform only) | _LMX-BLADE_| | _email_ |

Owners partially filled — confirm on-call rotation and PagerDuty/Slack routing before soft-launch.  
H1 staging evidence: [`docs/operator-dashboard/H1_STAGING_EVIDENCE_TEMPLATE.md`](../../docs/operator-dashboard/H1_STAGING_EVIDENCE_TEMPLATE.md).

---

## Minimum alert set

| # | Signal | Why | Starting threshold | First response |
|---|--------|-----|--------------------|----------------|
| 1 | API **5xx** rate | Bad deploy / dependency outage | ≥ 2% of requests or ≥ 10/min for 5m | Check `backend-api` logs, recent deploy, DB/Redis |
| 2 | Auth **401/403** spike | Contract drift, bad keys, attack | ≥ 50/min for 5m on `/api/v1/auth/*` | Compare client build vs routes; JWT config |
| 3 | **Redis** health fail | Presence, cache, rate limits | Any sustained fail 2m | Restart Redis; confirm password/network |
| 4 | **Postgres** health fail | Total outage | Any sustained fail 1m | Failover / storage / connections |
| 5 | **Migration job** failed | Schema skew risk | Any non-zero exit | Do not roll API forward; inspect MigrationService logs |
| 6 | **SignalR / WS** connection churn | Realtime broken | Connection open/close rate anomaly vs baseline | Check sticky sessions, Redis backplane, `/ws` |
| 7 | Admin auth unauthorized | Admin abuse / key leak | See admin-security-metrics.md | [`admin-security-incidents.md`](admin-security-incidents.md) |
| 8 | Sentry **new issue** spike | Regression | ≥ 20 new events / 10m same fingerprint | Bisect last deploy |

---

## Where metrics already exist

- Admin security meters: AdminSecurity instruments (auth, notifications, rate-limit rejects); service `synaptix-backend` (legacy `tycoon-backend` may still appear)  
- Dashboard JSON: `ops/dashboards/admin-security-observability.json`  
- Compose monitoring stack: `docker/monitoring/`, `docker/compose` monitoring profiles  
- **Prometheus rules (Wave 1 / H6b):** `docker/monitoring/prometheus/rules/alert-rules.yml` group `alpha-launch`  
  - `AlphaApi5xxRateHigh`, `AlphaAuthFailureSpike`, `AlphaBackendScrapeDown`, `AlphaRedisExporterDown`, `AlphaPostgresExporterDown`, `AlphaAdminRateLimitSustained`

---

## Staging vs production

| Environment | Expectation |
|-------------|-------------|
| Local | No paging; optional console |
| Staging | Full alert set; same thresholds or slightly tighter |
| Production | Same set; add on-call rotation |

---

## Validation checklist

- [ ] Who-gets-paged table filled  
- [ ] At least one synthetic 5xx / auth failure produces a visible alert in staging  
- [ ] Migration failure path tested in non-prod  
- [ ] Link this runbook from on-call wiki / Slack canvas  

---

## Out of scope (Security sprint later)

Secret scanning pages, presence spoof detection, economy fraud rules — deferred to Security hardening sprint (A).
