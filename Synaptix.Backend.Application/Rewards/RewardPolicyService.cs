using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Domain.Entities;

namespace Synaptix.Backend.Application.Rewards;

public sealed class RewardPolicyService
{
    private const int DailyReactorCap = 1;
    private static readonly TimeSpan ReactorCooldown = TimeSpan.FromHours(24);
    private static readonly TimeSpan ArcadeSpinCooldown = TimeSpan.FromHours(8);
    private const int DailyArcadeSpinCap = 3;

    private readonly IAppDb _db;

    public RewardPolicyService(IAppDb db)
    {
        _db = db;
    }

    public async Task<RewardPolicyResult> CheckAsync(
        Guid playerId,
        RewardMechanism mechanism,
        CancellationToken ct)
    {
        var (dailyCap, cooldown) = mechanism switch
        {
            RewardMechanism.Reactor => (DailyReactorCap, ReactorCooldown),
            RewardMechanism.ArcadeSpin => (DailyArcadeSpinCap, ArcadeSpinCooldown),
            _ => (1, TimeSpan.FromHours(24))
        };

        var utcNow = DateTimeOffset.UtcNow;
        var todayStart = new DateTimeOffset(utcNow.Year, utcNow.Month, utcNow.Day, 0, 0, 0, TimeSpan.Zero);

        var todayCount = await _db.RewardClaimLedger
            .CountAsync(r =>
                r.PlayerId == playerId &&
                r.Mechanism == mechanism &&
                r.ClaimStatus == "Applied" &&
                r.AppliedAtUtc >= todayStart, ct);

        if (todayCount >= dailyCap)
        {
            var resetAt = todayStart.AddDays(1);
            return new RewardPolicyResult(false, "REWARD_DAILY_LIMIT_REACHED",
                "Daily reward limit reached.", resetAt);
        }

        var lastSession = await _db.RewardSessions
            .Where(s =>
                s.PlayerId == playerId &&
                s.Mechanism == mechanism &&
                (s.Status == RewardSessionStatus.PendingClaim || s.Status == RewardSessionStatus.Applied))
            .OrderByDescending(s => s.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);

        if (lastSession is not null)
        {
            var cooldownUntil = lastSession.CreatedAtUtc + cooldown;
            if (utcNow < cooldownUntil)
                return new RewardPolicyResult(false, "REWARD_COOLDOWN_ACTIVE",
                    "Cooldown is still active.", cooldownUntil);
        }

        return new RewardPolicyResult(true, null, null, null);
    }
}
