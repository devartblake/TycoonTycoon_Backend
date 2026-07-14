#!/usr/bin/env python3
"""Compare React operator dashboard path literals to Backend.Api /admin maps."""
from __future__ import annotations

import re
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def run_inventory(script: str) -> list[str]:
    proc = subprocess.run(
        [sys.executable, str(ROOT / "scripts" / script)],
        cwd=ROOT,
        capture_output=True,
        text=True,
        check=False,
    )
    lines = []
    for line in proc.stdout.splitlines():
        line = line.strip()
        if not line or line.startswith("#"):
            continue
        # react inventory is path\\tsources
        path = line.split("\t", 1)[0].strip()
        if path:
            lines.append(path)
    return lines


def normalize(path: str) -> str:
    p = path.split("?", 1)[0].rstrip("/") or "/"
    p = re.sub(r"\{[^}/]+\}", "{id}", p)
    # collapse double segments
    p = re.sub(r"/+", "/", p)
    return p.lower()


def main() -> int:
    react = run_inventory("inventory-react-admin-routes.py")
    backend = run_inventory("inventory-admin-backend-routes.py")

    react_n = {normalize(p): p for p in react if p.startswith("/admin")}
    backend_n = {normalize(p): p for p in backend if p.startswith("/admin")}

    only_react = sorted(set(react_n) - set(backend_n))
    only_backend = sorted(set(backend_n) - set(react_n))
    both = sorted(set(react_n) & set(backend_n))

    print(f"react_admin_paths={len(react_n)}")
    print(f"backend_admin_paths={len(backend_n)}")
    print(f"matched_normalized={len(both)}")
    print(f"react_only={len(only_react)}")
    print(f"backend_only={len(only_backend)}")
    print("\n## React-only (potential gaps / wrong client paths)\n")
    for k in only_react:
        print(f"- `{react_n[k]}`")
    print("\n## Backend-only (not called by React literals)\n")
    for k in only_backend[:80]:
        print(f"- `{backend_n[k]}`")
    if len(only_backend) > 80:
        print(f"- … +{len(only_backend) - 80} more")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
