# Tycoon.OperatorDashboard.Vue

Vue front-end target for Operator Dashboard migration.

## Phase-1 bootstrap (in progress)
- [x] App shell + router scaffold
- [x] Initial Wave-A routes: Dashboard, Audit Log, Users
- [x] RBAC route guard helper skeleton
- [x] Session bootstrap integration (`/api/me`) for route guard permission checks
- [ ] Shared API client conventions
- [ ] Error envelope normalization

## NPM scripts
- `npm run dev`
- `npm run build`
- `npm run preview`
- `npm run test`
- `npm run lint`

## Notes
This directory is intentionally lightweight in the initial migration commit.
Do not commit `node_modules`.
