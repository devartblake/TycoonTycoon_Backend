# Security Review Prompt

Review a security-sensitive change.

## Prompt Template

Change:
`<describe change or files>`

Review for:
- auth bypass
- admin endpoint exposure
- secrets/log leakage
- KMS boundary violation
- insecure crypto
- wallet/economy tampering
- missing audit trail

Required output:
- Findings by severity.
- Required fixes.
- Recommended tests.
- Alpha release impact.
