# Contract Test Prompt

Add backend contract tests.

## Prompt Template

Contract:
`<route/use case>`

Expected behavior:
`<success and failure cases>`

Constraints:
- Test status codes and response shape.
- Include auth behavior when applicable.
- Keep test data deterministic.
- Prefer integration tests for API contracts.
- Run the relevant test project.

Required output:
- Test file path.
- Cases covered.
- Verification result.
