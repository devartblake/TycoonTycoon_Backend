namespace Synaptix.Backend.Domain.Entities;

public sealed class PlayerEconomySafeguardState
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid PlayerId { get; private set; }
    public int SessionsStarted { get; private set; }
    public int LossStreak { get; private set; }
    public int CurrentEnergy { get; private set; }
    public DateTimeOffset LastEnergyRegenAtUtc { get; private set; } = DateTimeOffset.UtcNow;
    public DateOnly? LastFreeTicketClaimDate { get; private set; }
    public int FreeTicketsClaimedToday { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

    private PlayerEconomySafeguardState() { } // EF

    public PlayerEconomySafeguardState(Guid playerId, int startEnergy = 20)
    {
        PlayerId = playerId;
        CurrentEnergy = Math.Max(0, startEnergy);
        LastEnergyRegenAtUtc = DateTimeOffset.UtcNow;
    }

    public int StartSession()
    {
        SessionsStarted++;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        return SessionsStarted;
    }

    // Energy is initialized in the constructor; this is a no-op for all new entities.
    // Kept for call-site compatibility.
    public void EnsureEnergyInitialized(int startEnergy) { }

    public void RegenerateEnergy(int maxEnergy, int regenMinutesPerEnergy)
    {
        var now = DateTimeOffset.UtcNow;
        var minutes = Math.Max(1, regenMinutesPerEnergy);
        var elapsedMinutes = (int)Math.Max(0, (now - LastEnergyRegenAtUtc).TotalMinutes);
        var regenPoints = elapsedMinutes / minutes;
        if (regenPoints <= 0) return;

        CurrentEnergy = Math.Min(maxEnergy, CurrentEnergy + regenPoints);
        LastEnergyRegenAtUtc = now;
        UpdatedAtUtc = now;
    }

    public bool TryConsumeEnergy(int amount)
    {
        if (amount <= 0) return true;
        if (CurrentEnergy < amount) return false;
        CurrentEnergy -= amount;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        return true;
    }

    public int ReportLoss()
    {
        LossStreak++;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        return LossStreak;
    }

    public void ResetLossStreak()
    {
        LossStreak = 0;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public (bool Granted, int RemainingToday) TryClaimDailyTicket(int dailyLimit)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (LastFreeTicketClaimDate != today)
        {
            LastFreeTicketClaimDate = today;
            FreeTicketsClaimedToday = 0;
        }

        if (FreeTicketsClaimedToday >= dailyLimit)
            return (false, 0);

        FreeTicketsClaimedToday++;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
        return (true, Math.Max(0, dailyLimit - FreeTicketsClaimedToday));
    }

    public bool TryConsumeTicket(int dailyLimit)
    {
        var claim = TryClaimDailyTicket(dailyLimit);
        return claim.Granted;
    }

    public bool HasTicketAvailable(int dailyLimit)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var claimedToday = LastFreeTicketClaimDate == today ? FreeTicketsClaimedToday : 0;
        return claimedToday < dailyLimit;
    }
}
