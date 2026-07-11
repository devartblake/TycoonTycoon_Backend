namespace Synaptix.Backend.Domain.Entities;

/// <summary>
/// A closed-out record of a sponsor's jackpot boost for one game event. Written
/// once at close when the event carried a sponsor multiplier, so ops can
/// reconcile how much each sponsor actually funded (the boost above the base
/// jackpot) and which player received it. One row per event.
/// </summary>
public sealed class GameEventSponsorAttribution
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid GameEventId { get; private set; }
    public string SponsorName { get; private set; } = string.Empty;

    /// <summary>Base jackpot before the multiplier.</summary>
    public int BaseJackpot { get; private set; }
    public decimal Multiplier { get; private set; }

    /// <summary>Jackpot after the multiplier — what the winner actually received.</summary>
    public int EffectiveJackpot { get; private set; }

    /// <summary>The coins the sponsor funded on top of the base (Effective − Base).</summary>
    public int BoostAmount { get; private set; }

    /// <summary>The rank-1 winner who received the boosted jackpot, if any.</summary>
    public Guid? BeneficiaryPlayerId { get; private set; }

    public Guid? SeasonId { get; private set; }
    public DateTimeOffset RecordedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

    private GameEventSponsorAttribution() { } // EF

    public GameEventSponsorAttribution(
        Guid gameEventId,
        string sponsorName,
        int baseJackpot,
        decimal multiplier,
        int effectiveJackpot,
        Guid? beneficiaryPlayerId,
        Guid? seasonId)
    {
        GameEventId = gameEventId;
        SponsorName = sponsorName;
        BaseJackpot = baseJackpot;
        Multiplier = multiplier;
        EffectiveJackpot = effectiveJackpot;
        BoostAmount = effectiveJackpot - baseJackpot;
        BeneficiaryPlayerId = beneficiaryPlayerId;
        SeasonId = seasonId;
    }
}
