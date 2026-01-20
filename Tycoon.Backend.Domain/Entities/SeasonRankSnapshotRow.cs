namespace Tycoon.Backend.Domain.Entities;

/// <summary>
/// Immutable snapshot of ranked season standings at the moment a season is closed.
/// Source of truth for post-close leaderboards, rewards, audits, and disputes.
/// </summary>
public sealed class SeasonRankSnapshotRow
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid SeasonId { get; private set; }
    public Guid PlayerId { get; private set; }

    public int RankPoints { get; private set; }
    public int Tier { get; private set; }
    public int TierRank { get; private set; }
    public int SeasonRank { get; private set; }      // global/season rank

    public int Wins { get; private set; }
    public int Losses { get; private set; }
    public int Draws { get; private set; }
    public int MatchesPlayed { get; private set; }

    public DateTimeOffset CapturedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

    private SeasonRankSnapshotRow() { }

    public SeasonRankSnapshotRow(PlayerSeasonProfile p, DateTimeOffset capturedAtUtc)
    {
        SeasonId = p.SeasonId;
        PlayerId = p.PlayerId;

        RankPoints = p.RankPoints;
        Tier = p.Tier;
        TierRank = p.TierRank;
        SeasonRank = p.SeasonRank;

        Wins = p.Wins;
        Losses = p.Losses;
        Draws = p.Draws;
        MatchesPlayed = p.MatchesPlayed;

        CapturedAtUtc = capturedAtUtc;
    }
}
