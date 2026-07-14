# Tycoon → Synaptix rename — Wave 4 (gRPC packages)

**Date:** 2026-07-13  
**Depends on:** [Waves 2+3](TYCOON_RENAME_WAVE2_3.md)

---

## Scope

Rename protobuf **package** identifiers that appear on the gRPC wire
(`/package.Service/Method`). C# type namespaces were already
`Synaptix.Backend.Api.Grpc` and did not need changing.

| Before | After |
|--------|--------|
| `package tycoon.mobile` | `package synaptix.mobile` |
| `package tycoon.sidecar` | `package synaptix.sidecar` |
| `/tycoon.mobile.MobileMatchService/*` | `/synaptix.mobile.MobileMatchService/*` |
| `/tycoon.sidecar.SidecarService/*` | `/synaptix.sidecar.SidecarService/*` |

---

## Changed

| Area | Change |
|------|--------|
| `protos/mobile.proto` | package → `synaptix.mobile` |
| `protos/sidecar.proto` | package → `synaptix.sidecar` |
| C# server codegen | Rebuild regenerates `__ServiceName` under `obj/` via Grpc.Tools |
| Python sidecar stubs | Regenerated `app/grpc_generated/sidecar_pb2*.py` |
| Docs / example Dart | Package + client naming updated |

**Unchanged**

- `option csharp_namespace = "Synaptix.Backend.Api.Grpc"`
- Service implementation types (`MobileMatchGrpcService`, `SidecarGrpcService`)
- REST/OpenAPI paths (not gRPC)

---

## Breaking change — client regen required

This is a **wire-level break**. Any client stub compiled against
`tycoon.mobile` / `tycoon.sidecar` will call the old full method path and get
`UNIMPLEMENTED` from a Wave-4+ server.

| Client | Action |
|--------|--------|
| **Python sidecar** (this repo) | Done — run `bash Synaptix.Sidecar/generate_grpc.sh` after future proto edits |
| **Flutter mobile** (external) | Regenerate from `protos/mobile.proto` and ship with the matching backend |
| **Any hand-rolled path strings** | Replace `tycoon.` → `synaptix.` in full method names |

Deploy backend + sidecar + mobile gRPC stubs in lockstep for environments that use gRPC.

---

## Verify

```bash
# C# server (regenerates stubs on build)
dotnet build Synaptix.Backend.Api/Synaptix.Backend.Api.csproj -c Release

# Python stubs
bash Synaptix.Sidecar/generate_grpc.sh
# or on Windows:
# python -m grpc_tools.protoc -I protos --python_out=Synaptix.Sidecar/app/grpc_generated \
#   --grpc_python_out=Synaptix.Sidecar/app/grpc_generated protos/sidecar.proto
# then patch relative import in sidecar_pb2_grpc.py (see generate_grpc.sh)

# Expect in generated code:
#   __ServiceName = "synaptix.mobile.MobileMatchService"
#   __ServiceName = "synaptix.sidecar.SidecarService"
#   '/synaptix.sidecar.SidecarService/ReportAnalyticsEvent'
```

**Verified 2026-07-13:** API Release build succeeded; Python stubs contain `synaptix.sidecar.*`; C# `obj/Release` `__ServiceName` uses `synaptix.*`.

---

## Next waves

| Wave | Scope |
|------|--------|
| **5** | Repo / `TycoonTycoon_Backend.slnx` rename |
| Sidecar metrics | Done — rename only to `synaptix_rebalance_*` (no dual-write) |
| Flutter (external) | Regenerate mobile protos against `synaptix.mobile` |
