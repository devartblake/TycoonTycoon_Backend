# Skill: KMS and Security Boundary

Use this skill for Synaptix.Security.Kms, encryption, secrets, auth, admin ops, or threat-model work.

## Rules

- Do not design custom cryptography.
- Use envelope encryption concepts.
- Separate key management from gameplay services.
- Add audit logs for sensitive operations.
- Avoid logging plaintext secrets, tokens, or keys.
- Keep client/server trust boundaries explicit.

## Procedure

1. Identify protected data and threat model.
2. Identify caller and authorization requirement.
3. Confirm KMS/client contract.
4. Implement minimal secure path.
5. Add tests for authorization, failure, and audit behavior.
