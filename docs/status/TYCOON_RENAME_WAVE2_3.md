# Tycoon → Synaptix rename — Waves 2 + 3

**Date:** 2026-07-13  
**Depends on:** [Wave 1](TYCOON_RENAME_WAVE1.md)

---

## Wave 2 — Test host rename

| Before | After |
|--------|--------|
| `TycoonApiFactory` class | `SynaptixApiFactory` |
| `TestHost/TycoonApiFactory.cs` | `TestHost/SynaptixApiFactory.cs` |
| All `IClassFixture<TycoonApiFactory>` (~100+ test files) | `IClassFixture<SynaptixApiFactory>` |
| `FallbackPolicyApiFactory : TycoonApiFactory` | `: SynaptixApiFactory` |
| `HttpClientAdminExtensions` default key source | `SynaptixApiFactory.TestAdminKey` |

**Not changed:** solution filename `TycoonTycoon_Backend.slnx` (Wave 5).

---

## Wave 3 — Observability dual-write

### Code

| Item | Detail |
|------|--------|
| New type | `SynaptixObservability` in `Synaptix.Shared.Observability/SynaptixObservability.cs` |
| Legacy type | `TycoonObservability` **obsolete alias** pointing at dual instruments |
| Dual-write | Every counter/histogram records under **both** `synaptix_*` / `synaptix.*` and `tycoon_*` / `tycoon.*` |
| ServiceName | Canonical `synaptix-backend`; constant `LegacyServiceName = tycoon-backend` for docs |
| Call sites | `ObservabilityExtensions`, `ElasticRollupRebuilder` use `SynaptixObservability` |

### Instrument map (primary ← dual → legacy)

| Canonical (use in new dashboards) | Legacy (kept for continuity) |
|-----------------------------------|------------------------------|
| `synaptix_rebuild_runs_total` | `tycoon_rebuild_runs_total` |
| `synaptix_rebuild_docs_read_total` | `tycoon_rebuild_docs_read_total` |
| `synaptix_rebuild_docs_indexed_total` | `tycoon_rebuild_docs_indexed_total` |
| `synaptix_rebuild_docs_failed_total` | `tycoon_rebuild_docs_failed_total` |
| `synaptix_rebuild_duration_ms` | `tycoon_rebuild_duration_ms` |
| `synaptix_mongo_read_duration_ms` | `tycoon_mongo_read_duration_ms` |
| `synaptix_elastic_bulk_duration_ms` | `tycoon_elastic_bulk_duration_ms` |
| `synaptix.admin_ops.requests` | `tycoon.admin_ops.requests` |
| `synaptix.rollup_rebuild.docs_indexed` | `tycoon.rollup_rebuild.docs_indexed` |
| `synaptix.rollup_rebuild.batch_ms` | `tycoon.rollup_rebuild.batch_ms` |

### Grafana / ops

- No hardcoded `tycoon_*` series were found under `docker/monitoring/grafana` or `ops/dashboards` JSON.
- Runbooks updated to prefer `synaptix-backend` service name.
- **Action for ops:** When creating new panels, query **synaptix_*** first; keep tycoon_* as `or` until dual-write period ends.

### Sidecar Prometheus series

Sidecar rebalance metrics renamed to `synaptix_rebalance_*` only (no dual-write — see WAVE4 / sidecar note).

---

## Verify

```bash
dotnet build Synaptix.Backend.Api.Tests/Synaptix.Backend.Api.Tests.csproj -c Release
dotnet build Synaptix.Shared.Observability/Synaptix.Shared.Observability.csproj -c Release
```

---

## Next waves

| Wave | Scope |
|------|--------|
| **4** | Proto packages → `synaptix.mobile` / `synaptix.sidecar` — [WAVE4](TYCOON_RENAME_WAVE4.md) |
| **5** | Repo / `TycoonTycoon_Backend.slnx` rename |
| Sidecar | Done — single-write `synaptix_rebalance_*` (no dual-write) |
