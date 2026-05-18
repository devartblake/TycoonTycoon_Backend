# Alpha/Beta Backend Checklist

## P0

- [ ] Backend starts locally through Docker.
- [ ] PostgreSQL migrations apply cleanly.
- [ ] Auth/session identity works.
- [ ] `/users/me/wallet` is authoritative.
- [ ] Match submit is idempotent.
- [ ] Rewards are server-authoritative.
- [ ] Store catalog has stable fallback.
- [ ] Admin operations are gated.
- [ ] Critical tests pass.

## P1

- [ ] Feature flags disable unfinished modules.
- [ ] MinIO seed loading is deterministic.
- [ ] Health/readiness checks are meaningful.
- [ ] Structured logs include correlation context.
- [ ] Sidecar failures degrade safely.
- [ ] CI verifies build and tests.

## P2 / Post-Alpha

- [ ] Full analytics dashboards.
- [ ] Advanced personalization tuning.
- [ ] Extended admin workflows.
- [ ] Multi-region deployment planning.
- [ ] Deep economy balancing automation.
