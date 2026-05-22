# Skill: Feature Flags

Use this skill to disable non-essential features for Alpha without deleting long-term platform work.

## Procedure

1. Identify feature owner and current entry points.
2. Decide default Alpha state: enabled or disabled.
3. Add typed flag config.
4. Gate endpoint, background job, UI contract, or service registration.
5. Ensure disabled behavior is explicit and testable.
6. Document rollout and removal criteria.

## Rules

- Do not silently return fake success for disabled critical flows.
- Use clear `FeatureDisabled` errors where appropriate.
- Keep flags grouped by bounded context.
