namespace Synaptix.Shared.Contracts.Dtos
{
    public sealed record AdminBulkBanRequest(
        IReadOnlyList<Guid> PlayerIds,
        string Reason,
        DateTimeOffset? Until
    );

    public sealed record AdminBulkRewardRequest(
        Guid BatchId,                       // caller-supplied; makes retries idempotent per player
        IReadOnlyList<Guid> PlayerIds,
        IReadOnlyList<EconomyLineDto> Rewards,
        string? Note
    );

    public sealed record AdminBulkResetProgressRequest(
        Guid BatchId,
        IReadOnlyList<Guid> PlayerIds,
        string Scope,                       // currently only "skills"
        int? RefundPercent
    );

    public sealed record BatchOperationItemResultDto(
        Guid PlayerId,
        bool Success,
        string? Error
    );

    public sealed record BatchOperationResultDto(
        int Requested,
        int Succeeded,
        int Failed,
        IReadOnlyList<BatchOperationItemResultDto> Items
    );
}
