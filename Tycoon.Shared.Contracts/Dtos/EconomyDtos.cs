namespace Tycoon.Shared.Contracts.Dtos
{
    public enum CurrencyType
    {
        Xp = 1,
        Coins = 2,
        Diamonds = 3
    }

    public enum EconomyTxnStatus
    {
        Applied = 1,
        Duplicate = 2,
        InsufficientFunds = 3,
        Invalid = 4
    }

    public sealed record EconomyLineDto(CurrencyType Currency, int Delta);

    public sealed record CreateEconomyTxnRequest(
        Guid EventId,
        Guid PlayerId,
        string Kind,                    // e.g. "mission-complete", "referral-redeem", "skill-unlock"
        IReadOnlyList<EconomyLineDto> Lines,
        string? Note = null
    );

    public sealed record EconomyTxnResultDto(
        Guid EventId,
        Guid PlayerId,
        EconomyTxnStatus Status,
        IReadOnlyList<EconomyLineDto> AppliedLines,
        int BalanceXp,
        int BalanceCoins,
        int BalanceDiamonds,
        DateTimeOffset ProcessedAtUtc
    );

    public sealed record EconomyTxnListItemDto(
        Guid EventId,
        string Kind,
        IReadOnlyList<EconomyLineDto> Lines,
        DateTimeOffset CreatedAtUtc
    );

    public sealed record EconomyHistoryDto(
        Guid PlayerId,
        int Page,
        int PageSize,
        int Total,
        IReadOnlyList<EconomyTxnListItemDto> Items
    );

    public sealed record AdminRollbackEconomyRequest(
        Guid EventId,
        string Reason
    );
}
