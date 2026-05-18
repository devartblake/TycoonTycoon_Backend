---
project: TycoonTycoon_Backend / Synaptix
mode: Claude Code
priority: Alpha/Beta before June 1, without damaging long-term platform architecture
---

# Command: Review Feature for Alpha

Use this command to decide whether a feature should ship in Alpha/Beta.

## Input Expected

Feature name, affected files, or user request.

## Instructions

1. Identify the bounded context.
2. Determine whether it is needed before June 1.
3. Check whether it can be feature-flagged.
4. Identify backend, DB, Docker, security, test, and frontend contract impact.
5. Recommend build, defer, cut scope, or feature-flag.

## Output

```md
# Feature Alpha Review

## Feature
Name:

## Decision
Build / Feature-flag / Defer / Remove from Alpha path

## Priority
P0/P1/P2/P3

## Reason
-

## Minimum Ship Scope
-

## Deferred Scope
-

## Required Tests
-

## Risks
-
```
