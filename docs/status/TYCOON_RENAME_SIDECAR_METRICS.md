# Tycoon → Synaptix — Sidecar rebalance metrics rename

**Date:** 2026-07-13  
**Approach:** Single-write rename only (no dual-write).

## Changed

| Before | After |
|--------|--------|
| Prometheus `tycoon_rebalance_*` | `synaptix_rebalance_*` |
| ES index default `tycoon_rebalance_metrics` | `synaptix_rebalance_metrics` |

**Files:** `Synaptix.Sidecar/app/routers/utilities.py`, `app/config.py`, tests.

## Ops

- Update any Prometheus scrapes / Grafana panels that queried `tycoon_rebalance_*`.
- Override with `REBALANCE_METRICS_INDEX` only if you need a non-default ES index name.
