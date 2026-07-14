# Operator Dashboard Direction — 2026-07-13

## Decision

**`Synaptix.OperatorDashboard.React` is the canonical Operator Dashboard** for operator/admin roles going forward.

`Synaptix.OperatorDashboard.Django` is **legacy / replacement target** — no new feature work; keep available only for rollback or residual workflows until React cutover evidence is complete.

Blazor / Vue / Web operator experiments remain **deprecated** (unchanged).

## Why this matters

Earlier May 2026 docs treated Django as canonical. That is **superseded**.

React currently has substantial **admin API path drift** vs `Synaptix.Backend.Api` (`/admin/*`). That drift was previously P2 (“secondary UI”); it is now **P0/P1 launch risk**.

See: [`REACT_ADMIN_ROUTE_GAP_INVENTORY.md`](REACT_ADMIN_ROUTE_GAP_INVENTORY.md)

## Implications

| Area | Implication |
|------|-------------|
| Alpha/Beta operator UX | Ship React; do not plan new Django screens |
| Backend contracts | Align React `src/**/api*.ts` to real `Admin*Endpoints` **or** add backend routes React already calls |
| Auth / RBAC | Prefer .NET admin auth (`/admin/auth/*`) + role claims; do not grow Django-only ACL |
| Cutover evidence | H1-style gates apply to **React staging**, not Django-first parallel-run |
| Django | Maintenance mode; decommission after React staging sign-off + rollback window |
| CI | React unit tests already blocking; E2E remains non-blocking until #439; add route-contract checks over time |

## Cutover outline (React replaces Django)

1. **Contract:** Close P0/P1 React path gaps (auth, users, moderation, notifications, store, questions, storage). **R1–R3 done in repo.**  
2. **Staging:** Deploy React as primary operator URL; Django optional fallback.  
3. **Parallel-run:** Operators complete critical workflows on React only — **[`REACT_STAGING_PARALLEL_RUN.md`](REACT_STAGING_PARALLEL_RUN.md) (R4).**  
4. **Sign-off:** QA / Backend / On-call — [`H1_STAGING_EVIDENCE_TEMPLATE.md`](H1_STAGING_EVIDENCE_TEMPLATE.md).  
5. **Decommission:** Remove Django from prod compose after rollback window (**R5**).

Automation: `scripts/operator-cutover-readiness.py --dashboard-ui react`.

## Doc precedence

If an older doc says “Django is canonical,” **this file wins** (dated 2026-07-13).

Historical May cutover guides remain useful for **ops process**, but the **target UI is React**.
