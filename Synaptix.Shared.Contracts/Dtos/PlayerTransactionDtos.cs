namespace Synaptix.Shared.Contracts.Dtos
{
    // ─── Request DTOs ────────────────────────────────────────────────

    public sealed record PlayerTransactionActorDto(
        Guid PlayerId,
        string Role,              // "system", "buyer", "seller", "recipient", "sender"
        int AllocationPercent = 100
    );

    public sealed record PlayerTransactionItemDto(
        string ItemType,          // "powerup:skip", "cosmetic:hat"
        int Quantity,
        string Operation          // "grant", "revoke", "swap"
    );

    public sealed record PlayerTransactionCurrencyDto(
        Guid PlayerId,
        IReadOnlyList<EconomyLineDto> Lines
    );

    public sealed record CreatePlayerTransactionRequest(
        Guid EventId,
        string Kind,                                           // "match-completion", "purchase", "game-event-entry"
        Guid? CorrelatedEventId = null,                        // links to match, game event, etc.
        string? Receipt = null,                                // IAP receipt
        IReadOnlyList<PlayerTransactionActorDto>? Actors = null,
        IReadOnlyList<PlayerTransactionCurrencyDto>? CurrencyChanges = null,
        IReadOnlyList<PlayerTransactionItemDto>? ItemChanges = null,
        string? Note = null
    );

    public sealed record DisputePlayerTransactionRequest(
        Guid PlayerTransactionId,
        string Reason
    );

    public sealed record ReversePlayerTransactionRequest(
        Guid PlayerTransactionId,
        string Reason
    );

    // ─── Result DTOs ─────────────────────────────────────────────────

    public sealed record PlayerTransactionResultDto(
        Guid PlayerTransactionId,
        Guid EventId,
        string Kind,
        string Status,            // "Applied", "Duplicate", "Failed", "InsufficientFunds"
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset? CompletedAtUtc,
        IReadOnlyList<PlayerTransactionActorDto> Actors,
        IReadOnlyList<PlayerTransactionItemDto> ItemChanges,
        IReadOnlyList<EconomyTxnResultDto> EconomyResults
    );

    // ─── History / Query DTOs ────────────────────────────────────────

    public sealed record PlayerTransactionListItemDto(
        Guid Id,
        Guid EventId,
        Guid? CorrelatedEventId,
        string Kind,
        string Status,
        string? DisputeReason,
        int ActorCount,
        int EconomyTxnCount,
        int ItemChangeCount,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset? CompletedAtUtc
    );

    public sealed record PlayerTransactionHistoryDto(
        int Page,
        int PageSize,
        int Total,
        IReadOnlyList<PlayerTransactionListItemDto> Items
    );

    public sealed record PlayerTransactionDetailDto(
        Guid Id,
        Guid EventId,
        Guid? CorrelatedEventId,
        string Kind,
        string Status,
        string? Receipt,
        string? DisputeReason,
        Guid? DisputeLinkedToTransactionId,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset? CompletedAtUtc,
        IReadOnlyList<PlayerTransactionActorDto> Actors,
        IReadOnlyList<PlayerTransactionItemDto> ItemChanges,
        IReadOnlyList<EconomyTxnListItemDto> EconomyTransactions
    );
}
