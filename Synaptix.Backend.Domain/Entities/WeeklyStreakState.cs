using System.Text.Json;

namespace Synaptix.Backend.Domain.Entities;

public sealed class WeeklyStreakState
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid PlayerId { get; private set; }
    public DateOnly CycleStartDate { get; private set; }
    public int CurrentDay { get; private set; }
    // Stored as JSON array string e.g. "[1,2,3]"
    public string ClaimedDaysJson { get; private set; } = "[]";
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private WeeklyStreakState() { } // EF

    public WeeklyStreakState(Guid playerId, DateOnly cycleStartDate)
    {
        PlayerId = playerId;
        CycleStartDate = cycleStartDate;
        CurrentDay = 1;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public IReadOnlyList<int> GetClaimedDays()
        => JsonSerializer.Deserialize<List<int>>(ClaimedDaysJson) ?? [];

    public void ClaimDay(int day)
    {
        var claimed = JsonSerializer.Deserialize<List<int>>(ClaimedDaysJson) ?? [];
        if (!claimed.Contains(day))
            claimed.Add(day);
        ClaimedDaysJson = JsonSerializer.Serialize(claimed);
        CurrentDay = day < 7 ? day + 1 : 1;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public void StartNewCycle(DateOnly cycleStartDate)
    {
        CycleStartDate = cycleStartDate;
        CurrentDay = 1;
        ClaimedDaysJson = "[]";
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }
}
