#!/usr/bin/env python3
"""Collect Operator Dashboard cutover readiness evidence as JSON and Markdown.

This script is intentionally read-only. It probes health/login endpoints and
records release-gate evidence supplied by the caller, but it never flips routes
or changes backend state.
"""

from __future__ import annotations

import argparse
import json
import os
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
from datetime import datetime, timezone
from http.cookiejar import CookieJar
from pathlib import Path
from typing import Any


def utc_now() -> str:
    return datetime.now(timezone.utc).replace(microsecond=0).isoformat().replace("+00:00", "Z")


def check_status(status: str) -> int:
    return {"pass": 0, "pending": 1, "blocked": 2, "fail": 3}.get(status, 3)


class Probe:
    def __init__(self, timeout: int) -> None:
        self.timeout = timeout

    def request(
        self,
        method: str,
        url: str,
        *,
        body: bytes | None = None,
        headers: dict[str, str] | None = None,
        opener: urllib.request.OpenerDirector | None = None,
    ) -> tuple[int, str]:
        req = urllib.request.Request(url, data=body, method=method, headers=headers or {})
        active_opener = opener or urllib.request.build_opener()
        try:
            with active_opener.open(req, timeout=self.timeout) as response:
                return response.status, response.read().decode("utf-8", errors="replace")
        except urllib.error.HTTPError as exc:
            return exc.code, exc.read().decode("utf-8", errors="replace")


def add_check(
    checks: list[dict[str, Any]],
    check_id: str,
    label: str,
    func,
    *,
    required: bool = True,
) -> None:
    start = time.perf_counter()
    try:
        status, evidence, http_status = func()
    except Exception as exc:  # noqa: BLE001 - evidence capture should not hide unexpected failures.
        status = "fail" if required else "blocked"
        evidence = f"{type(exc).__name__}: {exc}"
        http_status = None

    duration_ms = int((time.perf_counter() - start) * 1000)
    checks.append(
        {
            "id": check_id,
            "label": label,
            "status": status,
            "required": required,
            "httpStatus": http_status,
            "durationMs": duration_ms,
            "evidence": evidence,
        }
    )


def json_body(payload: dict[str, Any]) -> bytes:
    return json.dumps(payload).encode("utf-8")


def parse_json(payload: str) -> dict[str, Any]:
    try:
        parsed = json.loads(payload) if payload else {}
        return parsed if isinstance(parsed, dict) else {}
    except json.JSONDecodeError:
        return {}


def cookie_value(jar: CookieJar, name: str) -> str:
    for cookie in jar:
        if cookie.name == name:
            return cookie.value
    return ""


def status_from_gate(value: str) -> str:
    value = (value or "pending").strip().lower()
    return value if value in {"pass", "fail", "blocked", "pending"} else "pending"


def build_markdown(result: dict[str, Any]) -> str:
    lines = [
        "# Operator Dashboard Cutover Readiness",
        "",
        f"- Environment: `{result['environment']}`",
        f"- Generated: `{result['generatedAtUtc']}`",
        f"- Commit: `{result['commit']}`",
        f"- Overall status: `{result['overallStatus']}`",
        f"- Dashboard UI: `{result.get('dashboardUi', 'unknown')}`",
        f"- Dashboard URL: `{result.get('dashboardUrl', '')}`",
        f"- Fallback dashboard: `{result.get('fallbackDashboardUrl') or 'none'}`",
        f"- API URL: `{result.get('apiUrl', '')}`",
        f"- Parallel-run doc: `{result.get('parallelRunDoc', '')}`",
        "",
        "## Checks",
        "",
        "| Check | Status | HTTP | Evidence |",
        "|---|---|---:|---|",
    ]
    for check in result["checks"]:
        http_status = "" if check["httpStatus"] is None else str(check["httpStatus"])
        evidence = str(check["evidence"]).replace("|", "\\|")
        lines.append(f"| {check['label']} | `{check['status']}` | {http_status} | {evidence} |")

    lines.extend(["", "## Release Gates", "", "| Gate | Status |", "|---|---|"])
    for gate, status in result["releaseGates"].items():
        lines.append(f"| {gate} | `{status}` |")

    lines.extend(["", "## Next Actions", ""])
    for action in result["nextActions"]:
        lines.append(f"- {action}")
    if not result["nextActions"]:
        lines.append("- None.")
    lines.append("")
    return "\n".join(lines)


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--environment", default=os.getenv("CUTOVER_ENVIRONMENT", "staging"))
    parser.add_argument("--api-url", default=os.getenv("API_URL", "http://localhost:5000"))
    parser.add_argument(
        "--dashboard-url",
        default=os.getenv("DASHBOARD_URL", os.getenv("REACT_DASHBOARD_URL", "http://localhost:8300")),
        help="Primary operator UI base URL (React by default for R4)",
    )
    parser.add_argument(
        "--dashboard-ui",
        default=os.getenv("DASHBOARD_UI", "react"),
        choices=["react", "django"],
        help="Primary dashboard UI for probes (default: react)",
    )
    parser.add_argument(
        "--fallback-dashboard-url",
        default=os.getenv("FALLBACK_DASHBOARD_URL", ""),
        help="Optional Django fallback URL when primary UI is React",
    )
    parser.add_argument("--operator-email", default=os.getenv("SMOKE_ADMIN_EMAIL", ""))
    parser.add_argument("--operator-password-env", default=os.getenv("OPERATOR_PASSWORD_ENV", "SMOKE_ADMIN_PASSWORD"))
    parser.add_argument("--output-json", default=os.getenv("CUTOVER_READINESS_JSON", "artifacts/operator-cutover/operator-cutover-readiness.json"))
    parser.add_argument("--output-md", default=os.getenv("CUTOVER_READINESS_MD", "artifacts/operator-cutover/operator-cutover-readiness.md"))
    parser.add_argument("--commit", default=os.getenv("GITHUB_SHA", "local"))
    parser.add_argument("--timeout", type=int, default=int(os.getenv("CUTOVER_PROBE_TIMEOUT", "15")))
    args = parser.parse_args()

    api_url = args.api_url.rstrip("/")
    dashboard_url = args.dashboard_url.rstrip("/")
    fallback_url = (args.fallback_dashboard_url or "").rstrip("/")
    dashboard_ui = (args.dashboard_ui or "react").strip().lower()
    operator_password = os.getenv(args.operator_password_env, "")
    admin_ops_header = os.getenv("ADMIN_OPS_HEADER", "X-Admin-Ops-Key")
    admin_ops_key = os.getenv("ADMIN_OPS_KEY", "")
    probe = Probe(args.timeout)
    checks: list[dict[str, Any]] = []
    access_token = ""

    def admin_headers(extra: dict[str, str] | None = None) -> dict[str, str]:
        headers = dict(extra or {})
        if admin_ops_key:
            headers[admin_ops_header] = admin_ops_key
        return headers

    add_check(
        checks,
        "backend_health",
        "Backend health",
        lambda: (
            "pass" if (status := probe.request("GET", f"{api_url}/healthz")[0]) < 400 else "fail",
            f"GET {api_url}/healthz returned HTTP {status}",
            status,
        ),
    )

    # ── Primary dashboard probe (React SPA or Django healthz) ───────────────
    if dashboard_ui == "react":
        def react_spa() -> tuple[str, str, int | None]:
            for path in ("/index.html", "/"):
                status, body = probe.request("GET", f"{dashboard_url}{path}")
                if status < 400 and ("html" in body.lower() or "<!doctype" in body.lower() or status == 200):
                    return "pass", f"GET {dashboard_url}{path} returned HTTP {status} (SPA shell)", status
            return "fail", f"React SPA not reachable at {dashboard_url} (tried /index.html and /)", status

        add_check(checks, "react_spa", "React operator SPA", react_spa)
    else:
        add_check(
            checks,
            "django_health",
            "Django dashboard health",
            lambda: (
                "pass" if (status := probe.request("GET", f"{dashboard_url}/healthz")[0]) < 400 else "fail",
                f"GET {dashboard_url}/healthz returned HTTP {status}",
                status,
            ),
        )

    # Optional Django fallback when React is primary
    if dashboard_ui == "react" and fallback_url:
        add_check(
            checks,
            "django_fallback_health",
            "Django fallback health",
            lambda: (
                "pass" if (status := probe.request("GET", f"{fallback_url}/healthz")[0]) < 400 else "fail",
                f"GET {fallback_url}/healthz returned HTTP {status}",
                status,
            ),
            required=False,
        )

    def backend_login() -> tuple[str, str, int | None]:
        nonlocal access_token
        if not args.operator_email or not operator_password:
            return "blocked", f"Missing operator email or password env `{args.operator_password_env}`", None
        status, payload = probe.request(
            "POST",
            f"{api_url}/admin/auth/login",
            body=json_body({"email": args.operator_email, "password": operator_password}),
            headers=admin_headers({"Content-Type": "application/json"}),
        )
        parsed = parse_json(payload)
        access_token = str(parsed.get("accessToken") or "")
        if 200 <= status < 300 and access_token:
            return "pass", "Backend admin login returned an access token", status
        return "fail", f"Backend admin login returned HTTP {status}", status

    add_check(checks, "backend_admin_login", "Backend admin login", backend_login)

    def backend_me() -> tuple[str, str, int | None]:
        if not access_token:
            return "blocked", "Skipped because backend admin login did not return a token", None
        status, payload = probe.request(
            "GET",
            f"{api_url}/admin/auth/me",
            headers=admin_headers({"Authorization": f"Bearer {access_token}"}),
        )
        parsed = parse_json(payload)
        email = parsed.get("email") or parsed.get("Email") or "<present>"
        if 200 <= status < 300:
            return "pass", f"Backend admin profile returned email {email}", status
        return "fail", f"Backend admin profile returned HTTP {status}", status

    add_check(checks, "backend_admin_profile", "Backend admin profile", backend_me)

    def backend_dashboard() -> tuple[str, str, int | None]:
        if not access_token:
            return "blocked", "Skipped because backend admin login did not return a token", None
        status, _ = probe.request(
            "GET",
            f"{api_url}/admin/dashboard",
            headers=admin_headers({"Authorization": f"Bearer {access_token}"}),
        )
        if 200 <= status < 300:
            return "pass", f"Backend admin dashboard returned HTTP {status}", status
        if status == 404:
            return "pass", "Backend admin dashboard endpoint is not exposed; skipped optional aggregate probe", status
        return "fail", f"Backend admin dashboard returned HTTP {status}", status

    add_check(checks, "backend_admin_dashboard", "Backend admin dashboard", backend_dashboard)

    # R1/R2 admin surfaces React depends on (path existence + auth, not full UI)
    def admin_get(path: str, label: str) -> tuple[str, str, int | None]:
        if not access_token:
            return "blocked", "Skipped because backend admin login did not return a token", None
        status, _ = probe.request(
            "GET",
            f"{api_url}{path}",
            headers=admin_headers({"Authorization": f"Bearer {access_token}"}),
        )
        if 200 <= status < 300:
            return "pass", f"{label}: GET {path} → HTTP {status}", status
        if status in (401, 403):
            return "fail", f"{label}: GET {path} → HTTP {status} (auth/ops-key)", status
        if status == 404:
            return "fail", f"{label}: GET {path} → 404 (route missing)", status
        return "fail", f"{label}: GET {path} → HTTP {status}", status

    for check_id, label, path in (
        ("admin_users_list", "Admin users list", "/admin/users?page=1&pageSize=5"),
        ("admin_moderation_logs", "Admin moderation logs", "/admin/moderation/logs?page=1&pageSize=5"),
        ("admin_audit_security", "Admin audit security", "/admin/audit/security?page=1&pageSize=5"),
        ("admin_notifications_channels", "Admin notifications channels", "/admin/notifications/channels"),
        ("admin_store_catalog", "Admin store catalog", "/admin/store/catalog?page=1&pageSize=5"),
        ("admin_questions", "Admin questions", "/admin/questions?page=1&pageSize=5"),
        ("admin_storage_prefixes", "Admin storage prefixes", "/admin/storage/prefixes"),
        ("admin_personalization_summary", "Admin personalization summary", "/admin/personalization/summary"),
    ):
        add_check(
            checks,
            check_id,
            label,
            lambda p=path, l=label: admin_get(p, l),
            required=True,
        )

    def django_login(base: str, check_label: str) -> tuple[str, str, int | None]:
        if not args.operator_email or not operator_password:
            return "blocked", f"Missing operator email or password env `{args.operator_password_env}`", None
        jar = CookieJar()
        opener = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(jar))
        status, login_page = probe.request("GET", f"{base}/login", opener=opener)
        csrf = cookie_value(jar, "csrftoken")
        if not csrf:
            return "blocked", f"Login page returned HTTP {status}, but no csrftoken cookie was found", status
        body = urllib.parse.urlencode(
            {
                "email": args.operator_email,
                "password": operator_password,
                "csrfmiddlewaretoken": csrf,
            }
        ).encode("utf-8")
        status, _ = probe.request(
            "POST",
            f"{base}/login",
            body=body,
            headers={
                "Content-Type": "application/x-www-form-urlencoded",
                "Referer": f"{base}/login",
                "X-CSRFToken": csrf,
            },
            opener=opener,
        )
        if 200 <= status < 400:
            return "pass", f"{check_label} completed with HTTP {status}", status
        page_hint = " login page loaded" if login_page else ""
        return "fail", f"{check_label} returned HTTP {status};{page_hint}", status

    if dashboard_ui == "django":
        add_check(
            checks,
            "django_login",
            "Django session login (primary)",
            lambda: django_login(dashboard_url, "Django primary login"),
        )
    elif fallback_url:
        add_check(
            checks,
            "django_fallback_login",
            "Django fallback session login",
            lambda: django_login(fallback_url, "Django fallback login"),
            required=False,
        )

    gates = {
        "efMigrationsApplied": status_from_gate(os.getenv("GATE_EF_MIGRATIONS_APPLIED", "pending")),
        "strictReadiness": status_from_gate(os.getenv("GATE_STRICT_READINESS", "pending")),
        "reactPrimaryUrl": status_from_gate(os.getenv("GATE_REACT_PRIMARY_URL", "pending")),
        "parallelRun": status_from_gate(os.getenv("GATE_PARALLEL_RUN", "pending")),
        "signOff": status_from_gate(os.getenv("GATE_SIGN_OFF", "pending")),
        "cutover": status_from_gate(os.getenv("GATE_CUTOVER", "pending")),
        "djangoRollbackWindow": status_from_gate(
            os.getenv("GATE_DJANGO_ROLLBACK_WINDOW", os.getenv("GATE_BLAZOR_ROLLBACK_WINDOW", "pending"))
        ),
    }

    required_checks = [c for c in checks if c.get("required", True)]
    if any(check["status"] == "fail" for check in required_checks):
        overall = "fail"
    elif any(check["status"] == "blocked" for check in required_checks) or any(
        value == "blocked" for value in gates.values()
    ):
        overall = "blocked"
    elif all(check["status"] == "pass" for check in required_checks) and all(
        value == "pass" for value in gates.values()
    ):
        overall = "pass"
    else:
        overall = "pending"

    next_actions = [
        f"Resolve failing check `{check['id']}`"
        for check in checks
        if check["status"] in {"fail", "blocked"} and check.get("required", True)
    ]
    next_actions.extend(
        f"Attach evidence for release gate `{gate}`"
        for gate, status in gates.items()
        if status != "pass"
    )
    if dashboard_ui == "react":
        next_actions.append(
            "Complete React parallel-run matrix: docs/operator-dashboard/REACT_STAGING_PARALLEL_RUN.md"
        )

    result = {
        "generatedAtUtc": utc_now(),
        "environment": args.environment,
        "commit": args.commit,
        "dashboardUi": dashboard_ui,
        "dashboardUrl": dashboard_url,
        "fallbackDashboardUrl": fallback_url or None,
        "apiUrl": api_url,
        "overallStatus": overall,
        "checks": checks,
        "releaseGates": gates,
        "nextActions": next_actions,
        "parallelRunDoc": "docs/operator-dashboard/REACT_STAGING_PARALLEL_RUN.md",
    }

    json_path = Path(args.output_json)
    md_path = Path(args.output_md)
    json_path.parent.mkdir(parents=True, exist_ok=True)
    md_path.parent.mkdir(parents=True, exist_ok=True)
    json_path.write_text(json.dumps(result, indent=2) + "\n", encoding="utf-8")
    md_path.write_text(build_markdown(result), encoding="utf-8")

    print(f"Wrote {json_path}")
    print(f"Wrote {md_path}")
    return 0 if overall in {"pass", "pending"} else 1


if __name__ == "__main__":
    sys.exit(main())
