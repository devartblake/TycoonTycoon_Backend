# Operator Dashboard Drill Checklist — April 7, 2026

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

