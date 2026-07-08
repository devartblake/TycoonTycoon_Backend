namespace Synaptix.Shared.Contracts.Dtos
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

    // Operator economy dashboard: per-player summary (Coins is the headline balance).
    public sealed record AdminPlayerEconomyDto(
        Guid PlayerId,
        string Email,
        string Handle,
        int CurrentBalance,
        int TotalEarned,
        int TotalSpent,
        int TotalRefunded,
        DateTimeOffset? LastTransactionAt,
        DateTimeOffset AccountCreatedAt
    );

    // Operator economy dashboard: aggregate Coins circulation across all wallets.
    public sealed record AdminEconomyStatsDto(
        int TotalPlayers,
        long TotalCurrency,
        double AverageBalance,
        int LargestBalance,
        int SmallestBalance
    );

    public sealed record ModeBalanceRuleDto(
        string Mode,
        int EnergyCost,
        int? Lives,
        bool RequiresTicket,
        int TierPointsWeight
    );

    public sealed record GameBalanceConfigDto(
        int MaxEnergy,
        int StartEnergy,
        int RegenMinutesPerEnergy,
        int DailyFreeEnergy,
        int AdEnergyMin,
        int AdEnergyMax,
        bool LevelUpFullRefill,
        int? PremiumEnergyCapBonus,
        decimal? PremiumRegenMultiplier,
        IReadOnlyList<ModeBalanceRuleDto> Modes,
        SafeguardConfigDto Safeguards,
        DateTimeOffset UpdatedAtUtc
    );

    public sealed record UpdateGameBalanceConfigRequest(
        int? MaxEnergy,
        int? StartEnergy,
        int? RegenMinutesPerEnergy,
        int? DailyFreeEnergy,
        int? AdEnergyMin,
        int? AdEnergyMax,
        bool? LevelUpFullRefill,
        int? PremiumEnergyCapBonus,
        decimal? PremiumRegenMultiplier,
        IReadOnlyList<ModeBalanceRuleDto>? Modes,
        SafeguardConfigDto? Safeguards
    );

    public sealed record EconomySimulationRequest(
        int SessionMinutes,
        int? SessionNumber,
        int? CasualMatches,
        int? RankedMatches,
        int? GuardianMatches
    );

    public sealed record EconomySimulationResponse(
        int StartingEnergy,
        int EnergySpent,
        int EnergyRegenerated,
        int EndingEnergy,
        int EstimatedMatchesByMode,
        int EstimatedSessionMinutes
    );

    public sealed record SafeguardConfigDto(
        int FirstSessionsReducedCostCount,
        int FirstSessionsEnergyDiscount,
        int DailyFreeJackpotTickets,
        int ReviveBaseGemCost,
        int AlmostWinReviveDiscountPercent,
        int PityLossThreshold,
        decimal PityDifficultyReductionPercent
    );

    public sealed record ModeEntryDecisionDto(
        bool Allowed,
        string ReasonCode,
        string Message,
        int EnergyCostApplied,
        bool TicketConsumed,
        int CurrentEnergy
    );
}
