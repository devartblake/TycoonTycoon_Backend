#!/usr/bin/env python3
"""Extract React operator dashboard API path literals for contract inventory."""
from __future__ import annotations

import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "Synaptix.OperatorDashboard.React" / "src"

PATH_RE = re.compile(
    r"""['"`](/(?:admin|api|health|alive|ready|metrics)[^'"`\s]*)['"`]"""
)
INTERP_RE = re.compile(r"\$\{[^}]+\}")


def main() -> int:
    found: dict[str, set[str]] = {}
    for path in SRC.rglob("*.ts*"):
        if "node_modules" in path.parts:
            continue
        text = path.read_text(encoding="utf-8", errors="ignore")
        rel = path.relative_to(SRC).as_posix()
        for m in PATH_RE.finditer(text):
            raw = m.group(1)
            cleaned = INTERP_RE.sub("{param}", raw)
            # Drop query strings for catalog
            cleaned = cleaned.split("?", 1)[0]
            found.setdefault(cleaned, set()).add(rel)

    for route in sorted(found):
        sources = ", ".join(sorted(found[route])[:4])
        extra = f" (+{len(found[route]) - 4} more)" if len(found[route]) > 4 else ""
        print(f"{route}\t{sources}{extra}")

    print(f"# total_unique_paths={len(found)}", file=sys.stderr)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
