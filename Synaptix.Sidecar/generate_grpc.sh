#!/usr/bin/env bash
# ─────────────────────────────────────────────────────────────────────────────
# Generate Python gRPC stubs from the shared proto file.
#
# Run once after cloning, or whenever sidecar.proto changes:
#   bash Synaptix.Sidecar/generate_grpc.sh
#
# Requires: pip install grpcio-tools (listed in requirements.txt)
# ─────────────────────────────────────────────────────────────────────────────
set -eu

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROTO_DIR="$(cd "${SCRIPT_DIR}/../protos" && pwd)"
OUT_DIR="${SCRIPT_DIR}/app/grpc_generated"

mkdir -p "${OUT_DIR}"
touch "${OUT_DIR}/__init__.py"

python3 -m grpc_tools.protoc \
  -I "${PROTO_DIR}" \
  --python_out="${OUT_DIR}" \
  --grpc_python_out="${OUT_DIR}" \
  "${PROTO_DIR}/sidecar.proto"

# grpc_tools emits `import sidecar_pb2` which breaks package imports under
# `app.grpc_generated`. Rewrite to relative import for runtime stability.
OUT_DIR="${OUT_DIR}" python3 - <<'PY_PATCH'
import os
from pathlib import Path

p = Path(os.environ["OUT_DIR"]) / "sidecar_pb2_grpc.py"
text = p.read_text()
text = text.replace("import sidecar_pb2 as sidecar__pb2", "from . import sidecar_pb2 as sidecar__pb2")
p.write_text(text)
PY_PATCH

echo "✅ gRPC stubs generated in ${OUT_DIR}"
