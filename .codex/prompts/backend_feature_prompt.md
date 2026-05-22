# Backend Feature Prompt

Use this prompt when asking Codex to implement a backend feature.

## Prompt Template

Implement the following backend feature in `TycoonTycoon_Backend`.

Feature:
`<describe feature>`

Priority:
`P0/P1/P2/P3`

Constraints:
- Optimize for Alpha/Beta before June 1.
- Preserve long-term Clean Architecture.
- Do not add speculative abstractions.
- Keep local Docker as official integration target.
- Add or update tests.
- Do not commit secrets.
- Use feature flags if not Alpha-critical.

Required output:
- Summary of changes.
- Files changed.
- Verification commands run.
- Any failed/skipped checks.
- Follow-up issues.
