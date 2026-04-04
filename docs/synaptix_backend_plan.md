# Synaptix Backend Migration Plan (Tycoon Backend)

**Scope:** All backend work within the TycoonTycoon_Backend repo for the Trivia Tycoon -> Synaptix rebrand
**Includes:** .NET API, Blazor operator dashboard, Vue/web dashboard, Swagger/OpenAPI docs, backend analytics, operator-facing copy
**Companion doc:** `docs/synaptix_frontend_plan.md` (Flutter app work — will live in the Flutter project's docs directory)

> **Governing rule:** Change what operators, developers, and docs readers see. Do NOT do a solution-wide namespace rename until the product layer is coherent and tested.

---

## Global Backend Principles

1. Keep endpoint paths, DTO fields, domain model names, and service contracts stable.
2. Keep database table names, migration identifiers, and persistence keys unchanged.
3. Treat this as a product-language alignment, not a namespace migration.
4. Change visible titles, headings, descriptions, and operator-facing copy first.
5. Defer project/namespace renaming to Packet E (optional).
6. Keep a migration log for every visible rename and every deferred technical rename.

---

## BE Packet A — Audit + Brand Surface Reframe (Phases 0–1) ✅ COMPLETE

### BE-A1: Backend Audit (Phase 0)

**Objective:** Inventory all product-visible backend surfaces before changing anything.

**Scope:**
- Swagger/OpenAPI titles and descriptions
- Operator dashboard titles and headings (Blazor)
- Vue/web dashboard titles and headings
- Server-rendered product references
- Event names exposed in analytics/admin tools
- Docs and READMEs that reference product areas
- Any backend-generated copy visible to operators or downstream systems

**Deliverables:**
- `backend_surface_inventory.md` — surface-by-surface audit of product-visible copy
- `risk_register.md` — items that must NOT be renamed (endpoints, DTOs, migrations, etc.)
- Backend project impact matrix (which projects are touched, which are deferred)
- Deferred technical rename list (namespaces, project names, service names)

**Exit criteria:**
- All product-visible backend surfaces inventoried
- Risk register complete
- Deferred renames documented

---

### BE-A2: Brand Surface Reframe (Phase 1)

**Objective:** Make API docs and operator dashboards visibly read as Synaptix.

**Target areas:**

1. **Swagger/OpenAPI configuration**
   ```csharp
   options.SwaggerDoc("v1", new OpenApiInfo
   {
       Title = "Synaptix API",
       Version = "v1",
       Description = "Platform API for Synaptix gameplay, progression, and live competition."
   });
   ```

2. **Operator dashboard (Blazor)**
   - Top header -> "Synaptix Command"
   - Section shell branding
   - Dashboard welcome copy

3. **Vue/web dashboard**
   - App shell title
   - Visible navigation labels
   - Landing heading

4. **Product-facing docs**
   - Headings that reference "Trivia Tycoon" or "Tycoon" in a product context

**Do NOT change:**
- `Tycoon.Backend.*` namespaces
- `Tycoon.OperatorDashboard.*` project names
- Endpoint paths
- DTO/model property names
- Database tables or migration identifiers
- Route constants
- CI/CD pipeline names
- Service registration names

**Exit criteria:**
- Swagger doc title says "Synaptix API"
- Operator dashboard header says "Synaptix Command"
- Vue dashboard shell title is updated
- Product-facing docs headings aligned
- No namespace or contract churn introduced

---

## BE Packet B — Profile Support (Phases 2–3 support) ✅ COMPLETE

**Objective:** Provide backend support for frontend mode/theme system if needed.

**Scope:** Minimal — only act if the frontend requires backend persistence for:
- [x] Preferred Synaptix mode (kids / teen / adult)
- [x] Preferred home surface
- [x] Reduced motion preference
- [x] Tone preference (playful / balanced / competitive)

**Work items:**
- [x] Add additive fields to player profile storage/endpoints — `PlayerPreferences` entity with dedicated `GET/PUT /users/me/preferences` endpoints
- [x] Do NOT rename existing profile fields or keys — confirmed, no existing fields changed
- [x] Do NOT change existing profile endpoint paths — confirmed, `/users/me` untouched

**Exit criteria:**
- [x] Frontend mode/theme system can persist preferences through backend if needed
- [x] No existing profile contracts broken

---

## BE Packet C — Product-Language Alignment (Phase 5) ✅ COMPLETE

**Objective:** Align all backend-visible product language with Synaptix vocabulary so frontend and backend read as one platform.

> **Dependency:** Best done after frontend Packet C (Phase 4) so the frontend vocabulary is settled first.

### BE-C1: Swagger/OpenAPI Alignment

**Goal:** API documentation visibly matches the Synaptix product language.

**Work items:**
- Title: "Synaptix API" (if not already done in BE-A2)
- Feature summaries and tag descriptions use platform vocabulary:
  - Leaderboard endpoints -> described as Arena functionality
  - Arcade endpoints -> described as Labs functionality
  - Skill tree endpoints -> described as Pathways functionality
  - Social endpoints -> described as Circles functionality
  - Admin endpoints -> described as Command functionality
- Update endpoint descriptions where product vocabulary matters
- Update example payloads where product names appear in visible descriptions

**Do NOT change:**
- Endpoint paths (e.g., `/api/leaderboard` stays as-is)
- DTO field names
- Code-level namespaces
- Controller class names

---

### BE-C2: Operator Dashboard — Blazor

**Goal:** Operator-facing Blazor dashboard reads as Synaptix Command.

**Work items:**
- Top shell heading: "Synaptix Command"
- Nav grouping labels aligned to platform vocabulary
- Dashboard landing copy updated
- Section framing uses Arena / Labs / Pathways / Circles / Command language
- Feature area headings updated

**Do NOT change:**
- Dashboard routes
- Service wiring
- Auth wiring
- Project names or namespaces

---

### BE-C3: Vue/Web Dashboard

**Goal:** Vue/web dashboard shell aligns with Synaptix vocabulary.

**Work items:**
- Shell title updated
- Sidebar labels (where product-facing) updated
- Dashboard welcome copy updated
- Visible group headings updated

**Do NOT change:**
- Dashboard framework or layout systems
- Project root names
- API call paths

---

### BE-C4: Backend Docs and Descriptions

**Goal:** Product-facing and operator-facing docs align with Synaptix.

**Work items:**
- Project-level headings that reference the product
- Implementation docs that describe feature areas (only product-facing text)
- Operator-facing setup docs
- README sections describing the platform at a business level

**Rule:** If the text is implementation-critical and tied to code naming, leave it alone. If it's product-facing or operator-facing, align it to Synaptix.

---

### BE-C5: Analytics/Admin Terminology

**Goal:** Backend analytics wording matches what the frontend now calls the surfaces.

**Work items (display-level alignment only):**
- "Leaderboard events" -> "Arena events" in dashboards/docs
- "Arcade usage" -> "Labs usage" in visible reports
- "Skill tree interactions" -> "Pathways interactions" in admin-facing language

**Rule:** This is a display-language alignment pass, not an event-schema rewrite. Schema work belongs in BE-D1.

---

### BE-C Exit Criteria
- [ ] Frontend and backend product language match (awaiting frontend status)
- [x] Swagger docs say "Synaptix API" with aligned feature descriptions
- [x] Operator dashboards read as "Synaptix Command"
- [x] Backend docs do not contradict the frontend rebrand
- [x] No endpoint, namespace, or contract churn introduced

---

## BE Packet D — Analytics + Stabilization (Phases 6–7) ✅ COMPLETE

### BE-D1: Analytics and Telemetry (Phase 6)

**Objective:** Make the Synaptix rebrand measurable from the backend side.

**Work items:**
- Align event taxonomy docs with new dimensions
- Add/support analytics dimensions:
  - `synaptix_mode` (kids / teen / adult)
  - `surface` (hub / arena / labs / pathways / journey / circles / command)
  - `audience_segment`
  - `entry_point`
  - `brand_version`
- Update admin analytics wording in dashboards
- Update analytics label descriptions in operator-facing tools

**Rules:**
- Additive only — do NOT break existing analytics pipelines
- Ensure event naming consistency with frontend (cross-reference: `synaptix_frontend_plan.md` FE-D1)

---

### BE-D2: Stabilization and QA (Phase 7)

**Backend QA checklist:**
- [x] Swagger docs render correctly with Synaptix branding
- [x] Blazor dashboard loads correctly with Command branding
- [x] Vue/web dashboard loads correctly
- [x] Admin copy is consistent across all operator surfaces
- [x] No accidental contract breaks (endpoints, DTOs, auth)
- [x] Analytics dimensions appear as expected
- [ ] No namespace-related build regressions (requires build environment verification)

**Cross-layer QA:**
- [ ] Frontend labels match backend dashboard/docs language (awaiting frontend status)
- [x] No mixed "Trivia Tycoon" / "Synaptix" copy in operator-visible paths
- [ ] Operator surfaces use the same vocabulary as the app (awaiting frontend status)

**Deliverables:**
- Regression checklist completion
- Terminology consistency pass
- Bug log and fix pass
- Deferred rename list re-evaluated

**Exit criteria:**
- No major functional regressions
- No major brand inconsistency in operator-facing flows
- System stable enough to decide on Packet E

---

## BE Packet E — Optional Deep Technical Rename (Phase 8) ⏸️ DEFERRED

**Default recommendation: DEFER unless Packets A–D are stable.**
**Status:** Deferred — Packets A–D are complete. Decision gate pending stable production release.

### Workstream A: Backend Namespace and Project Family Rename

**Candidates:**
- `Tycoon.Backend.Api` -> `Synaptix.Backend.Api`
- `Tycoon.Backend.Application` -> `Synaptix.Backend.Application`
- `Tycoon.Backend.Domain` -> `Synaptix.Backend.Domain`
- `Tycoon.Backend.Infrastructure` -> `Synaptix.Backend.Infrastructure`
- `Tycoon.OperatorDashboard` -> `Synaptix.OperatorDashboard`
- All related project references

**Risks:**
- Solution-wide compile churn
- DI registration breakage
- Project reference breakage
- Docker and build script breakage
- Deployment and observability confusion

**Recommendation:** Treat as a dedicated backend modernization track, not a casual rename.

### Workstream B: Ops and Telemetry Naming Cleanup

**Candidates:**
- Service names in dashboards
- Deployment labels
- CI/CD job names
- Alert labels
- Log and trace service names

**Risks:**
- Observability blind spots
- Broken dashboards/alerts

**Recommendation:** Only do this after namespace rename decisions are finalized.

### Workstream C: Additional Technical Cleanup

**Candidates:**
- JWT issuer/audience names
- Docker image names
- Deployment/service names in orchestration
- Telemetry service identifiers

### Preconditions for Packet E
- Stable release from Packet D
- No major outstanding rebrand bugs
- Rollback strategy documented
- Build/test coverage acceptable
- Feature work frozen or reduced

### Decision Gate
Approve Packet E only if:
1. Visible product migration is already stable
2. Clear long-term engineering/operational benefit exists
3. Rollback is feasible
4. Team accepts this as a dedicated modernization effort

### If Deferred
- Document the defer decision explicitly
- State that product and technical roots may differ intentionally
- Record trigger conditions for revisiting later
- Keep known technical mismatches on the backlog

---

## Terminology Quick Reference

| Existing Concept | Synaptix Concept |
|---|---|
| Trivia Tycoon | Synaptix |
| Leaderboard endpoints | Arena |
| Arcade endpoints | Labs |
| Skill tree endpoints | Pathways |
| Social/messaging | Circles |
| Admin dashboard | Synaptix Command |
| XP | Neural XP or XP |
| Coins | Credits |
| Gems | Synapse Shards |

---

## Cross-Reference: Frontend Alignment Points

These are moments where the backend plan should align with frontend work (see `synaptix_frontend_plan.md`):

| Backend Phase | Frontend Alignment |
|---|---|
| BE-A (Audit) | Can run in parallel with FE-A |
| BE-B (Profile Support) | Needed only if FE-B1 requires backend persistence |
| BE-C (Language Alignment) | Should follow FE-C so frontend vocabulary is settled first |
| BE-D1 (Analytics) | Dimensions must match FE-D1 event instrumentation |
| BE-D2 (Stabilization) | Cross-layer QA with frontend labels |
| BE-E (Deep Rename) | Independent of FE-E |

---

## Recommended Execution Order

1. **BE Packet A** (parallel with FE Packet A)
2. **BE Packet B** (only if FE Packet B needs backend support)
3. **BE Packet C** (after FE Packet C settles frontend vocabulary)
4. **BE Packet D** (parallel with FE Packet D)
5. **BE Packet E** — defer unless justified

---

## Internal Soft Launch Validation (from backend perspective)

When the frontend team runs an internal soft launch, the backend should validate:
- [x] Swagger loads with Synaptix branding
- [x] Dashboards load with Synaptix Command branding
- [x] Analytics dimensions visible for new surfaces
- [x] No "Trivia Tycoon" copy in operator-facing paths
- [x] No API contract breaks
- [ ] Dashboard terminology matches app UI terminology (awaiting frontend status)
