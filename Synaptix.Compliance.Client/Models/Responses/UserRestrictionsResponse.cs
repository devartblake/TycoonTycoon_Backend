namespace Synaptix.Compliance.Client.Models.Responses;

public sealed record UserRestrictionsResponse(
    Guid UserId,
    IReadOnlyList<string> Restrictions);
