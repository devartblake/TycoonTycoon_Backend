namespace Synaptix.Backend.Application.Seasons;

/// <summary>
/// Tunables for solo-quiz season point accrual. Multiplayer accrual is
/// governed separately by <see cref="RankedSeasonOptions"/> — solo play
/// should never out-earn ranked matches, hence the daily cap.
/// </summary>
public sealed record SeasonSoloPointsOptions
{
    public bool Enabled { get; init; } = true;

    /// <summary>Season rank points per correctly answered question.</summary>
    public int PointsPerCorrect { get; init; } = 1;

    /// <summary>
    /// Maximum solo season points a player can earn per UTC day. Keeps the
    /// leaderboard from rewarding pure grind over match play (one ranked win
    /// is worth roughly half a capped solo day).
    /// </summary>
    public int DailyCap { get; init; } = 50;
}
