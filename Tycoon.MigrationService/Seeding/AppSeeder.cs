using Serilog;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Backend.Infrastructure.Persistence;

namespace Tycoon.MigrationService.Seeding;

public sealed class AppSeeder
{
    private readonly Serilog.ILogger _log;
    private readonly IConfiguration _cfg;

    public AppSeeder(IConfiguration cfg)
    {
        _cfg = cfg;
        _log = Log.ForContext<AppSeeder>();
    }

    public async Task SeedAsync(AppDb db, CancellationToken ct)
    {
        var executionStrategy = db.Database.CreateExecutionStrategy();

        await executionStrategy.ExecuteAsync(async () =>
        {
            // One transaction for consistency
            await using var tx = await db.Database.BeginTransactionAsync(ct);

            await SeedTiersAsync(db, ct);
            await SeedMissionsAsync(db, ct);
            await SeedSuperAdminAsync(db, ct);

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
        });
    }

    private static async Task SeedTiersAsync(AppDb db, CancellationToken ct)
    {
        // Synaptix tier ladder — upsert by order so existing rows get renamed.
        var definitions = new (string Name, int Order, int MinScore, int MaxScore)[]
        {
            ("Neural Initiate",    1, 0,      999),
            ("Synapse Adept",      2, 1000,   2999),
            ("Cortex Strategist",  3, 3000,   5999),
            ("Mind Architect",     4, 6000,   9999),
            ("Neural Overlord",    5, 10000,  14999),
            ("Synaptix Prime",     6, 15000,  int.MaxValue),
        };

        var existing = await db.Tiers.ToListAsync(ct);

        foreach (var (name, order, min, max) in definitions)
        {
            var tier = existing.FirstOrDefault(t => t.Order == order);
            if (tier is null)
            {
                db.Tiers.Add(new Tier(name, order, minScore: min, maxScore: max));
            }
            else if (tier.Name != name)
            {
                tier.UpdateDefinition(name, min, max);
            }
        }
    }

    private static async Task SeedMissionsAsync(AppDb db, CancellationToken ct)
    {
        // Synaptix mission definitions — upsert by key so existing rows get proper copy.
        var definitions = new[]
        {
            // Daily Signals
            new Mission(type: "Daily", key: "daily_win_1",      title: "First Signal",       description: "Win a match to send your first neural signal today.",               goal: 1,  rewardXp: 50,  rewardCoins: 25,  rewardDiamonds: 0, active: true),
            new Mission(type: "Daily", key: "daily_play_3",     title: "Signal Burst",        description: "Play 3 matches and keep your cognitive momentum going.",            goal: 3,  rewardXp: 75,  rewardCoins: 40,  rewardDiamonds: 0, active: true),
            new Mission(type: "Daily", key: "daily_streak_5",   title: "Synapse Chain",       description: "Win 5 matches in a row to activate your synaptic chain.",           goal: 5,  rewardXp: 125, rewardCoins: 60,  rewardDiamonds: 1, active: true),
            new Mission(type: "Daily", key: "daily_streak_7",   title: "Neural Surge",        description: "Hit a 7-win streak and prove your neural dominance.",               goal: 7,  rewardXp: 150, rewardCoins: 70,  rewardDiamonds: 1, active: true),
            new Mission(type: "Daily", key: "daily_streak_14",  title: "Mind Overclocked",    description: "14-win streak — your cortex is firing on all cylinders.",           goal: 14, rewardXp: 200, rewardCoins: 80,  rewardDiamonds: 2, active: true),
            new Mission(type: "Daily", key: "daily_answer_20",  title: "Knowledge Flood",     description: "Answer 20 questions to flood your neural network with data.",       goal: 20, rewardXp: 120, rewardCoins: 50,  rewardDiamonds: 0, active: true),

            // Weekly Signals
            new Mission(type: "Weekly", key: "weekly_win_10",    title: "Arena Ascendant",    description: "Secure 10 arena victories this week to climb the neural ladder.",  goal: 10, rewardXp: 500, rewardCoins: 250, rewardDiamonds: 3, active: true),
            new Mission(type: "Weekly", key: "weekly_play_25",   title: "Cognitive Marathon", description: "Complete 25 matches — endurance is the foundation of mastery.",   goal: 25, rewardXp: 650, rewardCoins: 300, rewardDiamonds: 4, active: true),
            new Mission(type: "Weekly", key: "weekly_perfect_5", title: "Precision Protocol", description: "Achieve 5 perfect rounds — no errors, pure cognitive clarity.",   goal: 5,  rewardXp: 750, rewardCoins: 350, rewardDiamonds: 5, active: true),
            new Mission(type: "Weekly", key: "weekly_perfect_7", title: "Flawless Circuit",   description: "7 perfect rounds in one week — your neural pathways are elite.",   goal: 7,  rewardXp: 600, rewardCoins: 275, rewardDiamonds: 3, active: true),
        };

        var existingKeys = await db.Missions.AsNoTracking()
            .Select(m => m.Key)
            .ToHashSetAsync(ct);

        foreach (var mission in definitions)
        {
            if (!existingKeys.Contains(mission.Key))
                db.Missions.Add(mission);
        }
    }

    private async Task SeedSuperAdminAsync(AppDb db, CancellationToken ct)
    {
        var email = _cfg["SuperAdmin:Email"];
        var password = _cfg["SuperAdmin:Password"];
        var handle = _cfg["SuperAdmin:Handle"] ?? "superadmin";

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            _log.Information("SuperAdmin:Email or SuperAdmin:Password not configured — skipping super admin seed.");
            return;
        }

        var normalizedEmail = email.ToLowerInvariant();
        var exists = await db.Users.AsNoTracking().AnyAsync(u => u.Email == normalizedEmail, ct);
        if (!exists)
        {
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
            var admin = new User(normalizedEmail, handle, passwordHash);
            db.Users.Add(admin);

            _log.Information("Seeded super admin account: {Email} (handle: {Handle})", normalizedEmail, handle);
        }
        else
        {
            _log.Information("Super admin account {Email} already exists — ensuring ACL entry.", normalizedEmail);
        }

        var acl = await db.AdminEmailAcls
            .FirstOrDefaultAsync(e => e.NormalizedEmail == normalizedEmail, ct);

        const string seedActor = "migration-service";
        const string notes = "Seeded for Django Operator Dashboard access.";

        if (acl is null)
        {
            db.AdminEmailAcls.Add(new AdminEmailAcl(normalizedEmail, AdminAclListType.Allow, AdminRole.SuperAdmin, seedActor, notes));
            _log.Information("Seeded super admin ACL allowlist entry: {Email}", normalizedEmail);
        }
        else if (acl.ListType != AdminAclListType.Allow || acl.Role != AdminRole.SuperAdmin)
        {
            acl.Update(AdminAclListType.Allow, AdminRole.SuperAdmin, notes);
            _log.Information("Updated super admin ACL entry to Allow/SuperAdmin: {Email}", normalizedEmail);
        }
        else
        {
            _log.Information("Super admin ACL allowlist entry already exists: {Email}", normalizedEmail);
        }
    }
}
