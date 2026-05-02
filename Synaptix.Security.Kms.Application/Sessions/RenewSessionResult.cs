namespace Synaptix.Security.Kms.Application.Sessions;

public sealed record RenewSessionResult(
    Guid SessionId,
    DateTimeOffset ExpiresAtUtc);
