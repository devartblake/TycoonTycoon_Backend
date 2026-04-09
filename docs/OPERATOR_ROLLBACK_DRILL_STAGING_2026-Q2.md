# Operator Dashboard Quarterly Rollback Drill — Staging Q2 2026

## Objective

Complete the first quarterly live rollback drill and attach all artifacts to release notes.

## Status

- **Current state:** In Progress (planning + scheduling started)
- **Kickoff date:** April 8, 2026
- **Target execution date:** April 15, 2026
- **Owner:** Operator Platform Team

## Drill Plan

1. Route staging operator traffic to Django `operator-dashboard`.
2. Introduce a controlled dashboard degradation scenario.
3. Trigger failover to `operator-dashboard-blazor`.
4. Validate continuity for critical operator workflows.
5. Restore Django dashboard and confirm system stability.
6. Publish post-drill report and remediation items.

## Drill Artifact Checklist

- [ ] Drill run timestamp (start/end, UTC)
- [ ] Participants and role matrix
- [ ] Trigger scenario and expected behavior
- [ ] Observed failover time and user impact
- [ ] Workflow continuity verification results
- [ ] Findings/remediation items with owners and due dates
- [ ] Link to release notes entry

## Evidence Log (to fill during drill)

| Timestamp (UTC) | Event | Result | Notes |
| --- | --- | --- | --- |
|  | Drill start | Pending | |
|  | Failure injected | Pending | |
|  | Blazor failover active | Pending | |
|  | Django restored | Pending | |
|  | Drill closeout | Pending | |

## Exit Criteria

- Failover and restoration complete without data loss.
- All critical operator workflows remain available during fallback.
- Post-drill report and release-note link published.
