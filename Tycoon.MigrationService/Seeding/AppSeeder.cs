using Serilog;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Backend.Infrastructure.Persistence;

namespace Tycoon.MigrationService.Seeding;

public sealed class AppSeeder
{
    private readonly Serilog.ILogger _log;

    public AppSeeder()
    {
        _log = Log.ForContext<AppSeeder>();
    }

    public async Task SeedAsync(AppDb db, CancellationToken ct)
    {
        // One transaction for consistency
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        await SeedTiersAsync(db);
        await SeedMissionsAsync(db);

        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    private static async Task SeedTiersAsync(AppDb db)
    {
        // If you already have tiers, do nothing.
        if (await db.Tiers.AsNoTracking().AnyAsync()) return;

        // NOTE: This assumes Tier has a public constructor or settable properties.
        // If your Tier entity differs, tell me the exact fields and I’ll adapt this seed.
        var tiers = new List<Tier>
        {
            new Tier("Bronze", 1, minScore: 0,    maxScore: 999),
            new Tier("Silver", 2, minScore: 1000, maxScore: 2499),
            new Tier("Gold",   3, minScore: 2500, maxScore: 4999),
            new Tier("Platinum",4,minScore: 5000, maxScore: 7999),
            new Tier("Diamond", 5, minScore: 8000, maxScore: 15000),
        };

        db.Tiers.AddRange(tiers);
    }

    private static async Task SeedMissionsAsync(AppDb db)
    {
        // If you already have missions, do nothing.
        if (await db.Missions.AsNoTracking().AnyAsync()) return;

        var missions = new List<Mission>
        {
            // Daily missions (Xp, Coins, Diamonds)
            new Mission(type: "Daily", key: "daily_win_1", title: "Daily win", description: "", goal: 1,  rewardXp: 50, rewardCoins: 25, rewardDiamonds: 0,  active: true),
            new Mission(type: "Daily", key: "daily_play_3", title: "Daily win", description: "", goal: 3,  rewardXp: 75, rewardCoins: 40, rewardDiamonds: 0,  active: true),
            new Mission(type: "Daily", key: "daily_streak_5", title: "Daily win", description: "", goal: 5,  rewardXp: 125, rewardCoins: 60, rewardDiamonds: 1, active: true),
            new Mission(type: "Daily", key: "daily_streak_7", title: "Daily win", description: "", goal: 7, rewardXp: 150, rewardCoins: 70, rewardDiamonds: 1, active: true),
            new Mission(type : "Daily", key : "daily_streak_14", title : "Daily win", description : "", goal : 14, rewardXp : 200, rewardCoins : 80, rewardDiamonds : 2, active : true),
            new Mission(type : "Daily", key : "daily_answer_20", title : "Daily win", description : "", goal : 20, rewardXp : 120, rewardCoins : 50, rewardDiamonds : 0, active : true),

            // Weekly missions
            new Mission(type: "Weekly", key: "weekly_win_10",title: "Daily win", description: "", goal: 10, rewardXp: 500, rewardCoins: 250, rewardDiamonds: 3, active: true),
            new Mission(type: "Weekly", key: "weekly_play_25", title: "Daily win", description: "", goal: 25, rewardXp: 650, rewardCoins: 300, rewardDiamonds: 4, active: true),
            new Mission(type : "Weekly", key : "weekly_perfect_5", title : "Daily win", description : "", goal : 5, rewardXp : 750, rewardCoins : 350, rewardDiamonds : 5, active : true),
            new Mission(type : "Weekly", key : "weekly_perfect_7", title : "Daily win", description : "", goal : 7, rewardXp : 600, rewardCoins : 275, rewardDiamonds : 3, active : true),
        };

        db.Missions.AddRange(missions);
    }
}
