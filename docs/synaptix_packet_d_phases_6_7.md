# Synaptix Packet D Implementation Guide
## Phases 6-7: Analytics & Telemetry Upgrade + Stabilization & Regression Hardening

This packet makes Synaptix measurable and production-ready.

---

## Phase 6 - Analytics & Telemetry

### Objective
Track usage of:
- Hub
- Arena
- Labs
- Pathways
- Journey
- Circles
- Command

### Core event structure
```dart
trackEvent("synaptix_surface_opened", {
  "surface": "arena",
  "mode": mode.name,
});
```

### Add dimensions
- synaptix_mode
- surface
- entry_point

### Rules
- additive only
- do NOT break existing analytics

---

## Phase 7 - Stabilization

### Frontend QA
- Hub loads
- Arena works
- Labs works
- Pathways works
- Journey loads
- Circles works
- Command works

### Backend QA
- Swagger loads
- dashboards load
- analytics appears

### Consistency check
- no "Trivia Tycoon" left
- all surfaces match naming

---

## Deliverables
- analytics working
- no regressions
- release-ready app