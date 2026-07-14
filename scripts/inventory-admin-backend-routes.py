#!/usr/bin/env python3
"""Extract /admin MapGroup + Map* relative paths from Synaptix.Backend.Api."""
from __future__ import annotations

import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1] / "Synaptix.Backend.Api"
GROUP_RE = re.compile(r"""MapGroup\(\s*"(?P<path>[^"]+)"\s*\)""")
MAP_RE = re.compile(
    r"""Map(?P<method>Get|Post|Put|Patch|Delete)\(\s*"(?P<path>[^"]*)"\s*[,)]"""
)


def main() -> None:
    full: set[str] = set()
    for path in ROOT.rglob("*.cs"):
        text = path.read_text(encoding="utf-8", errors="ignore")
        # Collect group paths declared in this file
        groups = [m.group("path") for m in GROUP_RE.finditer(text)]
        # Prefer groups that look admin-relative (start with / and not /api)
        admin_groups = [g for g in groups if g.startswith("/") and not g.startswith("/api")]
        maps = list(MAP_RE.finditer(text))
        if not maps:
            continue
        # Heuristic: if file is under Features/Admin* or mentions /admin, expand
        is_admin = "Admin" in path.name or "/admin" in text or "admin" in path.as_posix().lower()
        if not is_admin:
            continue
        bases = admin_groups or [""]
        for base in bases:
            # Bases are relative to MapGroup("/admin")
            for m in maps:
                rel = m.group("path")
                if base:
                    joined = base.rstrip("/") + ("/" + rel.lstrip("/") if rel else "")
                else:
                    joined = rel
                joined = joined.replace("//", "/")
                if not joined.startswith("/"):
                    joined = "/" + joined
                # Normalize constraints
                joined = re.sub(r"\{([^}:]+)[^}]*\}", r"{\1}", joined)
                full.add("/admin" + joined if not joined.startswith("/admin") else joined)

    for r in sorted(full):
        print(r)
    print(f"# total={len(full)}", flush=True)


if __name__ == "__main__":
    main()
