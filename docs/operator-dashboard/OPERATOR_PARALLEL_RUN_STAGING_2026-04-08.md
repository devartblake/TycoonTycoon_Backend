# Operator Dashboard Parallel-Run Validation — Staging (Kickoff April 8, 2026)

## Objective

Execute the first full Django-vs-Blazor parallel-run in staging with real operator accounts and produce sign-off artifacts.

## Status

- **Current state:** In Progress (kickoff complete)
- **Kickoff date:** April 8, 2026
- **Target validation window:** April 9–11, 2026
- **Owner:** Operator Platform Team

## Scope

- Django `operator-dashboard` and legacy `operator-dashboard-blazor` run side-by-side.
- Real operator accounts execute critical workflows on both surfaces.
- Results are compared for parity and operational safety.

## Workflow Matrix

| Workflow | Django Result | Blazor Result | Parity | Notes |
| --- | --- | --- | --- | --- |
| Login/logout | Pending | Pending | Pending | |
| Aggregated health page | Pending | Pending | Pending | |
| Users triage + bulk actions | Pending | Pending | Pending | |
| Moderation logs + profile + set-status | Pending | Pending | Pending | |
| Security audit + CSV export | Pending | Pending | Pending | |
| Media intent + MinIO diagnostics | Pending | Pending | Pending | |

## Operator Sign-off Template

| Operator | Date | Approved (Y/N) | Notes |
| --- | --- | --- | --- |
|  |  |  |  |
|  |  |  |  |

## Required Evidence Pack

1. Staging environment identifiers (compose revision + API image tags).
2. Test account list used for validation (non-secret references only).
3. Workflow matrix with pass/fail and discrepancy notes.
4. Sign-off table from operators.
5. Follow-up defects (if any) linked to issue tracker.

## Exit Criteria

- No P0 parity gaps remain open.
- At least two operator sign-offs are recorded.
- Rollback procedure remains validated and documented.
