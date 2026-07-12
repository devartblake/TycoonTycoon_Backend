namespace Synaptix.Backend.Application.Seasons;

public sealed record SeasonTiebreakerOptions
{
    public bool Enabled { get; init; } = true;

    /// <summary>How long after season close the tiebreaker match is scheduled.</summary>
    public TimeSpan ScheduleDelay { get; init; } = TimeSpan.FromHours(24);

    /// <summary>
    /// Grace period after the scheduled time before the expiry sweep resolves
    /// a no-show tiebreaker deterministically.
    /// </summary>
    public TimeSpan ExpiryGrace { get; init; } = TimeSpan.FromHours(24);

    /// <summary>
    /// Tie groups larger than this resolve deterministically instead of via a
    /// match — a 20-way tie is noise, not a championship.
    /// </summary>
    public int MaxGroupSize { get; init; } = 8;
}
