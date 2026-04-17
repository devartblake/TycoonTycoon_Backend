# Operator Dashboard Drill Checklist — April 8, 2026

## Cadence

- **Monthly**: one tabletop drill
- **Quarterly**: one live rollback drill in staging

## Monthly Tabletop Drill

1. Simulate backend-api degradation and walk mitigation choices.
2. Simulate auth refresh failure and validate operator fallback path.
3. Simulate MinIO outage and verify media action suppression behavior.
4. Confirm incident artifact collection process is followed.

## Quarterly Live Rollback Drill (Staging)

1. Route traffic to Django `operator-dashboard`.
2. Introduce a controlled dashboard failure.
3. Fail over to `operator-dashboard-blazor`.
4. Validate operator continuity for critical workflows.
5. Restore Django dashboard and close drill report.

## Required Outputs

- Drill date/time
- Participants
- Findings and remediation actions
- Next scheduled drill date

## Status Update — April 8, 2026

- Monthly tabletop checklist is ready and documented.
- ✅ Quarterly live rollback drill executed in staging on April 15, 2026.
- ✅ Drill execution plan + artifact log updated with completed evidence (`docs/OPERATOR_ROLLBACK_DRILL_STAGING_2026-Q2.md`).
- ✅ Drill report artifact completed with failover metrics and workflow continuity results (`docs/OPERATOR_ROLLBACK_DRILL_REPORT_2026-04-08.md`).
- ✅ Release artifact linkage published (`docs/OPERATOR_RELEASE_ARTIFACTS_2026-04.md`).
- Next action: schedule the Q3 live rollback drill window and pre-assign drill roles.
