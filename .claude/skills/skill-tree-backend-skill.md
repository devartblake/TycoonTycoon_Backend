---
project: TycoonTycoon_Backend / Synaptix
mode: Claude Code
priority: Alpha/Beta before June 1, without damaging long-term platform architecture
---

# Skill Tree Backend Skill

Use this skill when designing or implementing backend support for the Flutter skill tree.

## Goal

Support the client skill tree without overbuilding progression systems before Alpha.

## Alpha Scope

- Fetch skill tree/catalog.
- Fetch player unlock/progression state.
- Unlock a node if requirements and currency/XP rules pass.
- Return updated player skill state.
- Feature-flag advanced effects if not ready.

## Post-Alpha Scope

- seasonal trees.
- complex synergies.
- cooldown effects.
- skill respec.
- admin skill editor.
- A/B testing of tree layouts.

## Rules

- Server validates unlock requirements.
- Client cannot self-award unlocks.
- Skill effects must be explicit and auditable.
- Keep catalog seed data versionable.
- Use feature flags for incomplete categories.
