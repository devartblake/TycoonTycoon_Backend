namespace Tycoon.Shared.Contracts.Dtos;

public record SpinResultRequest(
    string RewardType,
    int Amount,
    string SpinId);

public record SpinResultResponse(
    bool Success,
    int CoinsGranted,
    int GemsGranted,
    int NewBalance,
    string Message);

public record SpinStatsDto(
    int DailyCount,
    int WeeklyCount,
    int TotalCount,
    int DailyLimit,
    int RemainingToday,
    DateTimeOffset DailyResetAt);

public record SpinHistoryEntry(
    string SpinId,
    string RewardType,
    int Amount,
    DateTimeOffset ClaimedAt);
