# Operator Dashboard Parallel-Run Validation — Staging (Kickoff April 8, 2026)

## Objective

Execute the first full Django-vs-Blazor parallel-run in staging with real operator accounts and produce sign-off artifacts.

## Status

- **Current state:** ✅ Complete — sign-offs collected, parity gaps closed
- **Kickoff date:** April 8, 2026
- **Validation window:** April 9–11, 2026
- **Closed date:** April 11, 2026
- **Owner:** Operator Platform Team

## Scope

- Django `operator-dashboard` and legacy `operator-dashboard-blazor` run side-by-side.
- Real operator accounts execute critical workflows on both surfaces.
- Results are compared for parity and operational safety.

## Workflow Matrix

| Workflow | Django Result | Blazor Result | Parity | Notes |
| --- | --- | --- | --- | --- |
| Login/logout | ✅ Pass | ✅ Pass | ✅ | Both surfaces authenticate and redirect correctly |
| Aggregated health page | ✅ Pass | ✅ Pass | ✅ | Health aggregation matches; Django includes MinIO status |
| Users triage + bulk actions | ✅ Pass | ✅ Pass | ✅ | Sort/filter/pagination and dry-run bulk guardrail confirmed |
| Moderation logs + profile + set-status | ✅ Pass | ✅ Pass | ✅ | Logs render and set-status actions reach backend correctly |
| Security audit + CSV export | ✅ Pass | ✅ Pass | ✅ | CSV export byte-for-byte equivalent on both surfaces |
| Media intent + MinIO diagnostics | ✅ Pass | ✅ Pass | ✅ | MinIO bucket status surface; no gaps observed |

No P0 parity gaps identified. One P2 cosmetic difference noted: Django surfaces MinIO reachability inline on the health page; Blazor shows a separate diagnostics tab. Deferred to post-cutover UX pass.

## Operator Sign-offs

| Operator | Date | Approved (Y/N) | Notes |
| --- | --- | --- | --- |
| ops-lead-1 (staging account) | 2026-04-10 | Y | All critical workflows validated; approved for cutover planning |
| ops-lead-2 (staging account) | 2026-04-11 | Y | Bulk-action guardrail confirmed; MinIO diagnostics match expectations |

## Evidence Pack

1. **Staging environment:** compose revision `staging-2026-04-09`, API image tag `api:2026-04-09-rc1`, Django dashboard image `dashboard-django:2026-04-09-rc1`.
2. **Test accounts used:** `ops-lead-1@staging.synaptix.local`, `ops-lead-2@staging.synaptix.local` (staging-only accounts, no production access).
3. **Workflow matrix:** see table above — all six workflows passed; one P2 cosmetic gap logged.
4. **Operator sign-offs:** two sign-offs recorded in the table above.
5. **Follow-up defects:** P2 cosmetic gap (MinIO diagnostics layout difference) tracked in backlog; no blocker defects.

## Exit Criteria

- ✅ No P0 parity gaps remain open.
- ✅ At least two operator sign-offs are recorded.
- ✅ Rollback procedure remains validated and documented (see `docs/OPERATOR_ROLLBACK_DRILL_STAGING_2026-Q2.md`).
