from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path
from textwrap import wrap

from reportlab.lib import colors
from reportlab.lib.pagesizes import letter
from reportlab.pdfgen import canvas


ROOT = Path(__file__).resolve().parents[1]
OUT_DIR = ROOT / "artifacts" / "release-evidence"
PAGE_W, PAGE_H = letter
MARGIN = 42
LINE = 13


@dataclass(frozen=True)
class EvidenceItem:
    key: str
    title: str
    pass_detail: str
    fail_detail: str
    source: str
    priority: str = "Must Pass"


@dataclass(frozen=True)
class WorkflowItem:
    key: str
    surface: str
    pass_detail: str
    fail_detail: str
    source: str


class FillablePacket:
    def __init__(self, path: Path, title: str, subtitle: str):
        self.path = path
        self.c = canvas.Canvas(str(path), pagesize=letter)
        self.title = title
        self.subtitle = subtitle
        self.y = PAGE_H - MARGIN
        self.page = 0
        self.field_seq = 0
        self.new_page()

    def save(self) -> None:
        self.c.save()

    def new_page(self) -> None:
        if self.page:
            self.c.showPage()
        self.page += 1
        self.y = PAGE_H - MARGIN
        self.c.setFont("Helvetica-Bold", 9)
        self.c.setFillColor(colors.HexColor("#5f6b7a"))
        self.c.drawString(MARGIN, PAGE_H - 24, self.title)
        self.c.drawRightString(PAGE_W - MARGIN, PAGE_H - 24, f"Page {self.page}")
        self.c.setStrokeColor(colors.HexColor("#d8dde6"))
        self.c.line(MARGIN, PAGE_H - 31, PAGE_W - MARGIN, PAGE_H - 31)
        self.c.setFillColor(colors.black)

    def ensure(self, needed: float) -> None:
        if self.y - needed < MARGIN:
            self.new_page()

    def heading(self, text: str) -> None:
        self.ensure(34)
        self.c.setFont("Helvetica-Bold", 16)
        self.c.setFillColor(colors.HexColor("#111827"))
        self.c.drawString(MARGIN, self.y, text)
        self.y -= 21
        self.c.setFillColor(colors.black)

    def subheading(self, text: str) -> None:
        self.ensure(26)
        self.c.setFont("Helvetica-Bold", 11)
        self.c.setFillColor(colors.HexColor("#1f2937"))
        self.c.drawString(MARGIN, self.y, text)
        self.y -= 16
        self.c.setFillColor(colors.black)

    def para(self, text: str, width: int = 96, font: str = "Helvetica", size: int = 9) -> None:
        lines = []
        for part in text.split("\n"):
            lines.extend(wrap(part, width=width) or [""])
        self.ensure(len(lines) * (size + 3) + 4)
        self.c.setFont(font, size)
        self.c.setFillColor(colors.HexColor("#374151"))
        for line in lines:
            self.c.drawString(MARGIN, self.y, line)
            self.y -= size + 3
        self.y -= 3
        self.c.setFillColor(colors.black)

    def label(self, text: str, x: float, y: float) -> None:
        self.c.setFont("Helvetica", 7)
        self.c.setFillColor(colors.HexColor("#4b5563"))
        self.c.drawString(x, y, text)
        self.c.setFillColor(colors.black)

    def text_field(self, name: str, x: float, y: float, w: float, h: float, value: str = "", multiline: bool = False) -> None:
        flags = "multiline" if multiline else ""
        self.c.acroForm.textfield(
            name=name,
            x=x,
            y=y,
            width=w,
            height=h,
            value=value,
            fieldFlags=flags,
            borderColor=colors.HexColor("#9ca3af"),
            fillColor=colors.white,
            textColor=colors.black,
            forceBorder=True,
            fontName="Helvetica",
            fontSize=7,
        )

    def checkbox(self, name: str, x: float, y: float) -> None:
        self.c.acroForm.checkbox(
            name=name,
            x=x,
            y=y,
            size=9,
            borderColor=colors.HexColor("#6b7280"),
            fillColor=colors.white,
            textColor=colors.HexColor("#0f766e"),
            buttonStyle="check",
            forceBorder=True,
        )

    def choice_status(self, item_key: str, x: float, y: float) -> None:
        self.c.acroForm.choice(
            name=f"{item_key}_status",
            x=x,
            y=y,
            width=86,
            height=16,
            options=["Pending", "Pass", "Fail", "Needs investigation", "N/A"],
            value="Pending",
            borderColor=colors.HexColor("#9ca3af"),
            fillColor=colors.white,
            textColor=colors.black,
            forceBorder=True,
            fontName="Helvetica",
            fontSize=7,
        )

    def cover(self) -> None:
        self.c.setFont("Helvetica-Bold", 20)
        self.c.setFillColor(colors.HexColor("#111827"))
        self.c.drawString(MARGIN, self.y, self.title)
        self.y -= 26
        self.c.setFont("Helvetica", 11)
        self.c.setFillColor(colors.HexColor("#4b5563"))
        self.c.drawString(MARGIN, self.y, self.subtitle)
        self.y -= 26
        self.c.setFillColor(colors.black)
        self.para(
            "Use this packet to capture launch evidence. A row is complete only when status is Pass, "
            "the required proof is attached or linked, and the reviewer/date fields are filled. "
            "Do not mark staging or production gates complete from local compose evidence alone."
        )
        self.subheading("Run metadata")
        rows = [
            ("Environment", "env"),
            ("Release SHA / image tag", "sha"),
            ("Evidence packet owner", "owner"),
            ("Execution date", "date"),
            ("Evidence folder / ticket link", "evidence_root"),
        ]
        for label, name in rows:
            self.ensure(28)
            self.label(label, MARGIN, self.y)
            self.text_field(name, MARGIN + 130, self.y - 4, 360, 17)
            self.y -= 24

    def source_list(self, sources: list[str]) -> None:
        self.heading("Source markdown covered")
        for source in sources:
            self.ensure(16)
            self.c.setFont("Helvetica", 8)
            self.c.drawString(MARGIN, self.y, source)
            self.y -= 12

    def quick_guide(self, steps: list[str]) -> None:
        self.heading("Completion guide")
        for idx, step in enumerate(steps, 1):
            self.para(f"{idx}. {step}", width=94)

    def signoff(self, roles: list[str], prefix: str) -> None:
        self.heading("Sign-off")
        self.para("Every required approver must sign after all Must Pass rows are Pass or have an approved mitigation.")
        for role in roles:
            self.ensure(58)
            key = self.safe(f"{prefix}_{role}")
            self.c.setFont("Helvetica-Bold", 9)
            self.c.drawString(MARGIN, self.y, role)
            self.y -= 14
            x = MARGIN
            for label, width in [("Name", 120), ("Date", 80), ("Approved Y/N", 80), ("Signature / approval link", 190)]:
                self.label(label, x, self.y)
                self.text_field(f"{key}_{self.safe(label)}", x, self.y - 18, width, 16)
                x += width + 10
            self.y -= 40

    def safe(self, text: str) -> str:
        return "".join(ch.lower() if ch.isalnum() else "_" for ch in text).strip("_")[:80]

    def item_block(self, item: EvidenceItem) -> None:
        self.ensure(116)
        key = self.safe(item.key)
        y0 = self.y
        self.c.setStrokeColor(colors.HexColor("#e5e7eb"))
        self.c.setFillColor(colors.HexColor("#f9fafb"))
        self.c.roundRect(MARGIN, y0 - 103, PAGE_W - 2 * MARGIN, 101, 5, fill=True, stroke=True)
        self.c.setFillColor(colors.black)
        self.c.setFont("Helvetica-Bold", 9)
        self.c.drawString(MARGIN + 8, y0 - 15, item.title[:92])
        self.c.setFont("Helvetica", 7)
        self.c.setFillColor(colors.HexColor("#6b7280"))
        self.c.drawString(MARGIN + 8, y0 - 27, f"Priority: {item.priority} | Source: {item.source}")
        self.c.setFillColor(colors.black)
        self.label("Status", MARGIN + 8, y0 - 46)
        self.choice_status(key, MARGIN + 48, y0 - 51)
        self.label("Owner", MARGIN + 145, y0 - 46)
        self.text_field(f"{key}_owner", MARGIN + 180, y0 - 51, 92, 16)
        self.label("Date", MARGIN + 281, y0 - 46)
        self.text_field(f"{key}_date", MARGIN + 305, y0 - 51, 75, 16)
        self.label("Evidence link / attachment ID", MARGIN + 389, y0 - 46)
        self.text_field(f"{key}_evidence", MARGIN + 389, y0 - 70, 145, 16)
        self.label("Pass requires", MARGIN + 8, y0 - 69)
        self.text_field(f"{key}_pass_detail", MARGIN + 65, y0 - 86, 220, 24, item.pass_detail, multiline=True)
        self.label("Fail / hold when", MARGIN + 298, y0 - 69)
        self.text_field(f"{key}_fail_detail", MARGIN + 366, y0 - 86, 168, 24, item.fail_detail, multiline=True)
        self.y -= 112

    def workflow_block(self, item: WorkflowItem) -> None:
        self.ensure(124)
        key = self.safe(item.key)
        y0 = self.y
        self.c.setStrokeColor(colors.HexColor("#e5e7eb"))
        self.c.setFillColor(colors.HexColor("#f9fafb"))
        self.c.roundRect(MARGIN, y0 - 111, PAGE_W - 2 * MARGIN, 109, 5, fill=True, stroke=True)
        self.c.setFillColor(colors.black)
        self.c.setFont("Helvetica-Bold", 9)
        self.c.drawString(MARGIN + 8, y0 - 15, item.surface[:90])
        self.c.setFont("Helvetica", 7)
        self.c.setFillColor(colors.HexColor("#6b7280"))
        self.c.drawString(MARGIN + 8, y0 - 27, f"Source: {item.source}")
        self.c.setFillColor(colors.black)
        labels = [("Django", 8), ("Blazor", 78), ("Match", 148), ("Overall", 218)]
        for label, dx in labels[:3]:
            self.label(label, MARGIN + dx, y0 - 47)
            self.choice_status(f"{key}_{label.lower()}", MARGIN + dx, y0 - 66)
        self.label("Overall", MARGIN + labels[3][1], y0 - 47)
        self.choice_status(key, MARGIN + labels[3][1], y0 - 66)
        self.label("Evidence link / screenshot / log", MARGIN + 318, y0 - 47)
        self.text_field(f"{key}_evidence", MARGIN + 318, y0 - 66, 216, 16)
        self.label("Pass requires", MARGIN + 8, y0 - 84)
        self.text_field(f"{key}_pass_detail", MARGIN + 65, y0 - 102, 220, 25, item.pass_detail, multiline=True)
        self.label("Fail / hold when", MARGIN + 298, y0 - 84)
        self.text_field(f"{key}_fail_detail", MARGIN + 366, y0 - 102, 168, 25, item.fail_detail, multiline=True)
        self.y -= 122


def release_items() -> list[EvidenceItem]:
    return [
        EvidenceItem("build_release", "Release build", "Release build completes with 0 errors; warnings are triaged or linked.", "Any build error, missing SDK, or untriaged blocker warning.", "ALPHA_RELEASE_CRITERIA.md"),
        EvidenceItem("api_tests", "Backend and application tests", "Target suites pass or have documented skip rationale.", "Any failed existing test suite without approved mitigation.", "ALPHA_RELEASE_CRITERIA.md"),
        EvidenceItem("ef_schema", "EF schema validation", "Schema drift check reports no pending model changes.", "Pending migration/model drift appears.", "ALPHA_RELEASE_CRITERIA.md"),
        EvidenceItem("migration_artifact_ci", "CI migration artifact", "CI uploads idempotent SQL migration artifact for release SHA.", "Artifact missing, wrong startup project, or failed generation.", "ALPHA_RELEASE_CRITERIA.md"),
        EvidenceItem("staging_migrations", "Staging EF migrations applied", "Migration log/transcript attached and final EF history row verified.", "Missing migration proof or wrong final migration.", "ALPHA_RELEASE_CRITERIA.md"),
        EvidenceItem("staging_ready", "Staging readiness", "GET /health/ready returns 200 with PostgreSQL, Redis, RabbitMQ, and MinIO healthy.", "Non-200 readiness or degraded dependency.", "ALPHA_RELEASE_CRITERIA.md"),
        EvidenceItem("app_config", "Public app config", "GET /api/v1/app/config returns minimumClientVersion and correct flag map.", "Missing config, wrong flags, or non-200 in staging.", "ALPHA_RELEASE_CRITERIA.md"),
        EvidenceItem("signup_login", "Auth signup/login", "Valid staging signup/login returns JWT and usable identity.", "Auth fails, missing token, or identity mismatch.", "ALPHA_RELEASE_CRITERIA.md"),
        EvidenceItem("wallet", "Wallet authoritative read", "Authenticated GET /users/me/wallet returns expected balances.", "Wallet missing, stale, or wrong player.", "ALPHA_RELEASE_CRITERIA.md"),
        EvidenceItem("quiz_complete", "Quiz completion reward idempotency", "First POST /quiz/complete grants XP/coins; duplicate EventId returns Duplicate.", "Double grant, missing reward, or non-idempotent result.", "ALPHA_RELEASE_CRITERIA.md"),
        EvidenceItem("leaderboard", "Leaderboard update", "POST /leaderboard records score and tier leaderboard reflects update after recalc.", "Score missing, wrong tier, or recalc failure.", "ALPHA_RELEASE_CRITERIA.md"),
        EvidenceItem("disabled_flags", "Disabled endpoint enforcement", "Disabled Alpha endpoints return HTTP 403 code FeatureDisabled.", "Any disabled endpoint returns 200, 503, or wrong error shape.", "ALPHA_RELEASE_CRITERIA.md"),
        EvidenceItem("flag_toggle", "Admin feature flag toggle", "Admin toggles a flag on/off without API restart; state reflected by config.", "Toggle fails, requires restart, or stale config remains.", "ALPHA_RELEASE_CRITERIA.md"),
        EvidenceItem("rollback_notes", "Rollback notes reviewed", "Rollback plan/notes reviewed by engineer and on-call.", "No review or unclear restore path.", "ALPHA_ROLLBACK_PLAN.md"),
        EvidenceItem("rollback_drill", "Rollback drill", "Non-production restore/rollback drill evidence attached.", "Drill missing, restore fails, or timing unknown.", "ALPHA_RELEASE_CRITERIA.md"),
        EvidenceItem("flutter_smoke", "Flutter live backend smoke", "Flutter live backend smoke passes against migrated staging API.", "Flutter smoke fails or not run against correct staging.", "ALPHA_RELEASE_CRITERIA.md"),
        EvidenceItem("known_issues", "Known issues review", "All P0/P1 issues resolved or approved with mitigation.", "Unmitigated P0/P1 remains.", "ALPHA_KNOWN_ISSUES.md"),
        EvidenceItem("release_gate", "Release gate workflow", "release-gate.yml passes on release SHA and artifact link attached.", "Workflow fails or artifact missing.", "ALPHA_RELEASE_CRITERIA.md"),
        EvidenceItem("oncall", "72-hour on-call confirmed", "On-call owner and escalation path recorded for launch window.", "No on-call coverage or unclear escalation.", "ALPHA_RELEASE_CRITERIA.md"),
        EvidenceItem("quality_perf", "Quality and observability checks", "Golden path latency/logs/Hangfire/OTLP checks pass or have mitigation.", "Performance/log/job/tracing issue with no mitigation.", "ALPHA_RELEASE_CRITERIA.md", "Should Pass"),
    ]


def cutover_gates() -> list[EvidenceItem]:
    return [
        EvidenceItem("ef_migrations_applied", "efMigrationsApplied", "Staging and production migration logs/transcripts attached with final EF history verification.", "Any live migration evidence missing or failed.", "OPERATOR_DASHBOARD_MAY_CUTOVER_COMPLETION_GUIDE.md"),
        EvidenceItem("strict_readiness", "strictReadiness", "Strict Synaptix.MigrationService readiness logs attached for live environments.", "Strict readiness disabled, missing, or failed.", "OPERATOR_DASHBOARD_MAY_CUTOVER_COMPLETION_GUIDE.md"),
        EvidenceItem("parallel_run", "parallelRun", "Staging runbook matrix completed with real operators and no blocking discrepancies.", "Blank result/evidence rows, login failure, 500, or functional discrepancy.", "STAGING_PARALLEL_RUN_RUNBOOK_2026-05-15.md"),
        EvidenceItem("sign_off", "signOff", "QA Lead, Backend Lead, and On-call Operator approval rows populated.", "Any required approver missing or withholding approval.", "OPERATOR_PARALLEL_RUN_EVIDENCE_2026-04-08.md"),
        EvidenceItem("cutover", "cutover", "Django route/upstream active, timestamp/owner/image tags recorded, post-cutover smoke passed.", "Route not flipped, smoke failure, or missing owner/timestamp.", "OPERATOR_RELEASE_ARTIFACTS_2026-04.md"),
        EvidenceItem("blazor_rollback_window", "blazorRollbackWindow", "Blazor fallback remains warm through 2026-06-12 or approved policy exception attached.", "Fallback unavailable before window closes or policy exception missing.", "OPERATOR_RELEASE_ARTIFACTS_2026-04.md"),
    ]


def cutover_workflows() -> list[WorkflowItem]:
    return [
        WorkflowItem("auth_permissions", "Auth and permissions", "Login/logout, sidebar email, nav permissions, and expected 403 behavior verified.", "Login fails, wrong operator shown, unexpected 403/200, or session not cleared.", "STAGING_PARALLEL_RUN_RUNBOOK_2026-05-15.md"),
        WorkflowItem("command_center", "Command center / health", "All service tiles render and statuses match authoritative health.", "Missing tile, wrong status, or uninvestigated degraded dependency.", "STAGING_PARALLEL_RUN_RUNBOOK_2026-05-15.md"),
        WorkflowItem("users", "Users triage and detail", "List/filter/detail/activity/ban/unban work with before-after evidence.", "Long IDs overlap, activity missing, ban/unban unverified, or Blazor divergence.", "STAGING_PARALLEL_RUN_RUNBOOK_2026-05-15.md"),
        WorkflowItem("moderation", "Moderation logs and player profile", "Logs/profile/detail/status update show matching player/status/reason/timestamps.", "Filter failure, missing detail, wrong player, or status mutation not persisted.", "STAGING_PARALLEL_RUN_RUNBOOK_2026-05-15.md"),
        WorkflowItem("security_audit", "Security audit", "Audit list/filter/detail metadata load and event IDs match.", "Missing metadata, stale events, filter failure, or unreadable detail.", "STAGING_PARALLEL_RUN_RUNBOOK_2026-05-15.md"),
        WorkflowItem("questions", "Questions queue and detail", "Pending list/detail/options/status transitions verified with evidence.", "Approve/reject/edit unverified, wrong status, or missing question controls.", "STAGING_PARALLEL_RUN_RUNBOOK_2026-05-15.md"),
        WorkflowItem("economy", "Economy player", "Lookup/history/grant/deduction reflect correct player, amount, reason, and timestamp.", "Wrong sign/currency/player, missing history, or mutation cannot be verified.", "STAGING_PARALLEL_RUN_RUNBOOK_2026-05-15.md"),
        WorkflowItem("store", "Store flash sales, stock policies, analytics", "Lists/filter/cancel/date-range checks pass with staging-safe objects.", "Wrong SKU/status/date range, cancel divergence, or no evidence.", "STAGING_PARALLEL_RUN_RUNBOOK_2026-05-15.md"),
        WorkflowItem("game_events", "Game events", "Scheduled/Open/Live status transitions occur in order and refresh correctly.", "Wrong event, missing action, failed transition, or timeout.", "STAGING_PARALLEL_RUN_RUNBOOK_2026-05-15.md"),
        WorkflowItem("seasons", "Seasons", "Season activation, leaderboard, and recompute checks complete without error.", "Wrong season activated, leaderboard fails, or recompute unconfirmed.", "STAGING_PARALLEL_RUN_RUNBOOK_2026-05-15.md"),
        WorkflowItem("anticheat", "Anti-cheat flags", "List/filter/review actions persist reviewed state and notes/operator evidence.", "Review not persisted, filter wrong, or missing flag context.", "STAGING_PARALLEL_RUN_RUNBOOK_2026-05-15.md"),
        WorkflowItem("notifications", "Notifications", "Send/schedule/cancel/template/channel/history/dead-letter workflows show IDs and history.", "Job/schedule/template/channel mutation missing or unverified.", "STAGING_PARALLEL_RUN_RUNBOOK_2026-05-15.md"),
        WorkflowItem("event_queue", "Event queue", "Reprocess with safe limit returns job/confirmation and timestamp.", "No confirmation, invalid scope accepted, or excessive limit used.", "STAGING_PARALLEL_RUN_RUNBOOK_2026-05-15.md"),
        WorkflowItem("storage_media", "Storage and media", "Media intent upload and MinIO diagnostics show real status without secret exposure.", "Upload fails, diagnostic misleading, or secrets exposed in evidence.", "STAGING_PARALLEL_RUN_RUNBOOK_2026-05-15.md"),
        WorkflowItem("avatar_api", "Avatar purchase API path", "Catalog/purchase/owned/re-purchase/download owner/non-owner responses match expected statuses.", "Wrong ownership/status code or asset URL permission failure.", "STAGING_PARALLEL_RUN_RUNBOOK_2026-05-15.md"),
        WorkflowItem("django_only", "Supplemental Django-only checks", "Detail/investigation/personalization/player stock/advanced notification checks pass.", "Any 500, missing golden-path control, broken return navigation, or unverified mutation.", "STAGING_PARALLEL_RUN_RUNBOOK_2026-05-15.md"),
    ]


def build_release_pdf() -> None:
    pdf = FillablePacket(
        OUT_DIR / "alpha_beta_release_evidence_packet.pdf",
        "Alpha/Beta Release Evidence Packet",
        "Fillable evidence checklist for alpha-beta-2026 launch readiness",
    )
    pdf.cover()
    pdf.quick_guide(
        [
            "Start with repo evidence: release build, backend/application tests, EF schema validation, migration artifact generation, and local compose smoke.",
            "Apply migrations to staging using Synaptix.MigrationService or the DBA fallback. Attach logs and the final EF history query before marking migration rows Pass.",
            "Run staging readiness and golden-path API smoke. Record request IDs, HTTP status, timestamps, and sanitized response snippets.",
            "Run Flutter live backend smoke against the migrated staging API. Attach command output or CI artifact link.",
            "Review rollback and known issues. Any P0/P1 issue must be resolved or explicitly mitigated before sign-off.",
            "Run release-gate.yml against the release SHA. Attach workflow run and artifact links.",
            "Collect Backend Lead, QA Lead, On-Call Engineer, and Product Owner sign-off only after Must Pass rows are Pass or have approved mitigation.",
        ]
    )
    pdf.source_list(
        [
            "docs/releases/ALPHA_RELEASE_CRITERIA.md",
            "docs/releases/ALPHA_ENABLED_FEATURES.md",
            "docs/releases/ALPHA_DISABLED_FEATURES.md",
            "docs/releases/ALPHA_KNOWN_ISSUES.md",
            "docs/releases/ALPHA_ROLLBACK_PLAN.md",
            "docs/alpha-beta/Synaptix_Alpha_Beta_Release_Plan.md",
            "docs/alpha-beta/Synaptix_Alpha_Beta_Database_Migration_Implementation_Plan.md",
            ".codex/heartbeat/alpha-status.md",
            ".codex/heartbeat/current-blockers.md",
            ".codex/heartbeat/verification-log.md",
        ]
    )
    pdf.heading("Alpha/Beta release criteria")
    for item in release_items():
        pdf.item_block(item)
    pdf.signoff(["Backend Lead", "QA Lead", "On-Call Engineer", "Product Owner"], "alpha")
    pdf.heading("Evidence attachment index")
    for idx in range(1, 11):
        pdf.ensure(31)
        pdf.label(f"Attachment {idx} description", MARGIN, pdf.y)
        pdf.text_field(f"alpha_attachment_{idx}_description", MARGIN + 115, pdf.y - 4, 180, 17)
        pdf.label("Link / artifact ID", MARGIN + 310, pdf.y)
        pdf.text_field(f"alpha_attachment_{idx}_link", MARGIN + 375, pdf.y - 4, 160, 17)
        pdf.y -= 25
    pdf.save()


def build_cutover_pdf() -> None:
    pdf = FillablePacket(
        OUT_DIR / "operator_cutover_evidence_packet.pdf",
        "Operator Dashboard Cutover Evidence Packet",
        "Fillable staging parallel-run, cutover, and rollback-window evidence checklist",
    )
    pdf.cover()
    pdf.quick_guide(
        [
            "Freeze inputs: record staging/prod environment identifiers, image tags, database target references, and operator account aliases without secrets.",
            "Apply migrations and strict dashboard readiness. Do not pass efMigrationsApplied or strictReadiness without live logs.",
            "Execute every applicable staging runbook workflow. The Django/Blazor x marks show applicability only; each row still needs Pass/Fail/Needs investigation and evidence.",
            "For mutating workflows, capture before evidence, submitted action/reason, after evidence, and refreshed history/status proof.",
            "Hold cutover for any Django 500, login failure, missing golden-path control, Blazor functional discrepancy, or unverified mutation.",
            "Collect QA Lead, Backend Lead, and On-call Operator approval after the runbook is complete.",
            "After production route cutover, record timestamp, route owner, image tags, smoke results, and Blazor fallback health through 2026-06-12.",
        ]
    )
    pdf.source_list(
        [
            "docs/infrastructure/STAGING_PARALLEL_RUN_RUNBOOK_2026-05-15.md",
            "docs/operator-dashboard/OPERATOR_DASHBOARD_MAY_CUTOVER_COMPLETION_GUIDE.md",
            "docs/operator-dashboard/OPERATOR_PARALLEL_RUN_EVIDENCE_2026-04-08.md",
            "docs/operator-dashboard/OPERATOR_RELEASE_ARTIFACTS_2026-04.md",
            "docs/operator-dashboard/OPERATOR_DASHBOARD_PARITY_CHECKLIST.md",
            "artifacts/operator-cutover/operator-cutover-readiness.md",
            "artifacts/operator-cutover/operator-cutover-readiness.json",
        ]
    )
    pdf.heading("Environment and operator accounts")
    for label, name in [
        ("Staging environment", "staging_env"),
        ("Django image tag", "django_image"),
        ("Backend API image tag", "backend_image"),
        ("Migration service image tag", "migration_image"),
        ("Blazor fallback endpoint/tag", "blazor_fallback"),
        ("Database target reference", "db_target"),
    ]:
        pdf.ensure(26)
        pdf.label(label, MARGIN, pdf.y)
        pdf.text_field(name, MARGIN + 150, pdf.y - 4, 360, 17)
        pdf.y -= 24
    for idx in range(1, 3):
        pdf.ensure(33)
        pdf.label(f"Operator {idx} alias / role", MARGIN, pdf.y)
        pdf.text_field(f"operator_{idx}_alias", MARGIN + 110, pdf.y - 4, 135, 17)
        pdf.label("Permissions", MARGIN + 260, pdf.y)
        pdf.text_field(f"operator_{idx}_permissions", MARGIN + 315, pdf.y - 4, 220, 17)
        pdf.y -= 27

    pdf.heading("Cutover release gates")
    for item in cutover_gates():
        pdf.item_block(item)

    pdf.heading("Staging parallel-run workflow matrix")
    for item in cutover_workflows():
        pdf.workflow_block(item)

    pdf.heading("Production cutover smoke")
    for route in ["/login", "/", "/users", "/operations/notifications", "/personalization", "/store/player-stock"]:
        key = pdf.safe(f"smoke_{route}")
        pdf.ensure(42)
        pdf.c.setFont("Helvetica-Bold", 9)
        pdf.c.drawString(MARGIN, pdf.y, f"Smoke check {route}")
        pdf.choice_status(key, MARGIN + 120, pdf.y - 4)
        pdf.label("HTTP / result", MARGIN + 218, pdf.y)
        pdf.text_field(f"{key}_http", MARGIN + 275, pdf.y - 4, 70, 16)
        pdf.label("Evidence", MARGIN + 356, pdf.y)
        pdf.text_field(f"{key}_evidence", MARGIN + 400, pdf.y - 4, 135, 16)
        pdf.y -= 36

    pdf.signoff(["QA Lead", "Backend Lead", "On-call Operator"], "cutover")
    pdf.heading("Defect and discrepancy log")
    for idx in range(1, 9):
        pdf.ensure(43)
        pdf.label(f"Defect {idx} workflow", MARGIN, pdf.y)
        pdf.text_field(f"defect_{idx}_workflow", MARGIN + 88, pdf.y - 4, 120, 16)
        pdf.label("Ticket", MARGIN + 220, pdf.y)
        pdf.text_field(f"defect_{idx}_ticket", MARGIN + 250, pdf.y - 4, 100, 16)
        pdf.label("Resolution / mitigation", MARGIN + 362, pdf.y)
        pdf.text_field(f"defect_{idx}_resolution", MARGIN + 442, pdf.y - 4, 93, 16)
        pdf.y -= 34
    pdf.save()


def main() -> None:
    OUT_DIR.mkdir(parents=True, exist_ok=True)
    build_release_pdf()
    build_cutover_pdf()
    print(f"Wrote {OUT_DIR / 'alpha_beta_release_evidence_packet.pdf'}")
    print(f"Wrote {OUT_DIR / 'operator_cutover_evidence_packet.pdf'}")


if __name__ == "__main__":
    main()
