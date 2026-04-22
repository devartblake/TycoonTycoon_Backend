# Operator Dashboard Quarterly Rollback Drill — Staging Q2 2026

## Objective

Complete the first quarterly live rollback drill and attach all artifacts to release notes.

## Status

- **Current state:** Completed
- **Kickoff date:** April 8, 2026
- **Execution date:** April 15, 2026
- **Owner:** Operator Platform Team

## Drill Plan

1. Route staging operator traffic to Django `operator-dashboard`.
2. Introduce a controlled dashboard degradation scenario.
3. Trigger failover to `operator-dashboard-blazor`.
4. Validate continuity for critical operator workflows.
5. Restore Django dashboard and confirm system stability.
6. Publish post-drill report and remediation items.

## Drill Artifact Checklist

- [x] Drill run timestamp (start/end, UTC)
- [x] Participants and role matrix
- [x] Trigger scenario and expected behavior
- [x] Observed failover time and user impact
- [x] Workflow continuity verification results
- [x] Findings/remediation items with owners and due dates
- [x] Link to release notes entry

## Participants and Role Matrix

| Participant | Role |
| --- | --- |
| Operator Platform Lead | Drill commander |
| SRE On-call | Failover executor |
| API On-call | Workflow verifier |
| Incident scribe | Timeline + artifact capture |

## Trigger Scenario

- Injected failure: Django dashboard service intentionally paused in staging for 90 seconds to force operator-facing outage path.
- Expected behavior: staging operator traffic fails over to `operator-dashboard-blazor` with no data-loss and bounded operator interruption.

## Evidence Log

| Timestamp (UTC) | Event | Result | Notes |
| --- | --- | --- | --- |
| 2026-04-15 14:00:00 | Drill start | Complete | Baseline health checks green |
| 2026-04-15 14:03:10 | Failure injected | Complete | Django dashboard paused |
| 2026-04-15 14:03:46 | Blazor failover active | Complete | 36s failover activation |
| 2026-04-15 14:21:12 | Django restored | Complete | Primary traffic shifted back |
| 2026-04-15 14:25:00 | Drill closeout | Complete | Artifacts captured and linked |

## Failover Metrics

- **Failover activation time:** 36 seconds (failure injected → Blazor active)
- **Primary restore time:** 54 seconds (restore start → Django serving traffic)
- **Operator impact window:** 43 seconds of degraded UI response during switch
- **Data loss / write failure count:** 0 observed

## Workflow Continuity Verification

| Workflow | During Blazor fallback | Result |
| --- | --- | --- |
| Admin login + token refresh | Verified | Pass |
| User triage + status update | Verified | Pass |
| Moderation log review | Verified | Pass |
| Security audit CSV export | Verified | Pass |
| Media diagnostics read flow | Verified | Pass |

## Findings and Remediation

1. **Finding:** One stale cache panel required manual refresh after fallback activation.  
   **Owner:** Operator Platform Team  
   **Due:** 2026-04-29
2. **Finding:** Alert threshold for operator dashboard failover is too coarse (2-minute rollup).  
   **Owner:** SRE Team  
   **Due:** 2026-04-22
3. **Finding:** Drill timeline capture was fully manual; automation needed for timestamp consistency.  
   **Owner:** API Platform Team  
   **Due:** 2026-05-01

## Release Notes Link

- Release artifact bundle: `docs/OPERATOR_RELEASE_ARTIFACTS_2026-04.md`

## Exit Criteria

- Failover and restoration complete without data loss.
- All critical operator workflows remain available during fallback.
- Post-drill report and release-note link published.
