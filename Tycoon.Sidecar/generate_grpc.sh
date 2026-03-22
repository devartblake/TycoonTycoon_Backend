#!/usr/bin/env bash
# ─────────────────────────────────────────────────────────────────────────────
# Generate Python gRPC stubs from the shared proto file.
#
# Run once after cloning, or whenever sidecar.proto changes:
#   bash generate_grpc.sh
#
# Requires: pip install grpcio-tools (listed in requirements.txt)
# ─────────────────────────────────────────────────────────────────────────────
set -euo pipefail

PROTO_DIR="$(cd "$(dirname "$0")/../protos" && pwd)"
OUT_DIR="$(cd "$(dirname "$0")/app/grpc_generated" && pwd 2>/dev/null || mkdir -p "$(dirname "$0")/app/grpc_generated" && cd "$(dirname "$0")/app/grpc_generated" && pwd)"

mkdir -p "$OUT_DIR"
touch "$OUT_DIR/__init__.py"

python -m grpc_tools.protoc \
  -I "$PROTO_DIR" \
  --python_out="$OUT_DIR" \
  --grpc_python_out="$OUT_DIR" \
  "$PROTO_DIR/sidecar.proto"

echo "✅ gRPC stubs generated in $OUT_DIR"
