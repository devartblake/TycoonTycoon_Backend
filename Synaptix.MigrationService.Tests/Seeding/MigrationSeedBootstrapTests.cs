using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Backend.Infrastructure.Persistence;
using Synaptix.MigrationService.Options;
using Synaptix.MigrationService.Seeding;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.MigrationService.Tests.Seeding;

public sealed class MigrationSeedBootstrapTests
{
    [Fact]
    public async Task AppSeeder_SeedsSuperAdminAcl_Idempotently()
    {
        await using var db = NewDb();
        const string configuredEmail = "Admin@Synaptix.Local";
        var expectedEmail = configuredEmail.ToLowerInvariant();
        var seeder = new AppSeeder(Config(("SuperAdmin:Email", configuredEmail), ("SuperAdmin:Password", "ChangeMe123!")));

        await seeder.SeedAsync(db, CancellationToken.None);
        await seeder.SeedAsync(db, CancellationToken.None);

        db.Users.Count(u => u.Email == expectedEmail).Should().Be(1);
        db.AdminEmailAcls.Count(e => e.NormalizedEmail == expectedEmail).Should().Be(1);

        var acl = await db.AdminEmailAcls.SingleAsync(e => e.NormalizedEmail == expectedEmail);
        acl.ListType.Should().Be(AdminAclListType.Allow);
        acl.Role.Should().Be(AdminRole.SuperAdmin);
    }

    [Fact]
    public async Task MinioSeeder_BundledMode_LoadsBundledSeedFiles()
    {
        await using var db = NewDb();
        var root = CreateSeedRoot();
        await WriteSeedFilesAsync(root);

        var seeder = new MinioSeeder(
            new FakeObjectStorage(),
            Microsoft.Extensions.Options.Options.Create(new MinioSeedOptions { BundledRootPath = root }),
            Microsoft.Extensions.Options.Options.Create(new MigrationServiceOptions { SeedSource = "Bundled" }));

        await seeder.SeedAsync(db, CancellationToken.None);

        db.StoreItems.Should().ContainSingle(x => x.Sku == "test_boost");
        db.SkillNodes.Should().ContainSingle(x => x.Key == "test_skill");
        db.SeasonRewardRules.Should().ContainSingle();
        db.Questions.Should().ContainSingle(x => x.Text == "What is Synaptix?");
    }

    [Fact]
    public async Task MinioSeeder_AutoMode_FallsBackToBundledSeedFiles()
    {
        await using var db = NewDb();
        var root = CreateSeedRoot();
        await WriteSeedFilesAsync(root);

        var seeder = new MinioSeeder(
            new FakeObjectStorage(),
            Microsoft.Extensions.Options.Options.Create(new MinioSeedOptions { BundledRootPath = root }),
            Microsoft.Extensions.Options.Options.Create(new MigrationServiceOptions { SeedSource = "Auto" }));

        await seeder.SeedAsync(db, CancellationToken.None);

        db.StoreItems.Should().ContainSingle(x => x.Sku == "test_boost");
        db.Questions.Should().ContainSingle(x => x.Text == "What is Synaptix?");
    }

    [Fact]
    public async Task MinioSeeder_MinioMode_LoadsObjectStorageSeedFiles()
    {
        await using var db = NewDb();
        var storage = new FakeObjectStorage();
        storage.PutJson("seeds/store-items.json", StoreItemsJson);
        storage.PutJson("seeds/skill-nodes.json", SkillNodesJson);
        storage.PutJson("seeds/season-rewards.json", SeasonRewardsJson);
        storage.PutJson("seeds/questions.json", QuestionsJson);

        var seeder = new MinioSeeder(
            storage,
            Microsoft.Extensions.Options.Options.Create(new MinioSeedOptions()),
            Microsoft.Extensions.Options.Options.Create(new MigrationServiceOptions { SeedSource = "MinIO" }));

        await seeder.SeedAsync(db, CancellationToken.None);

        db.StoreItems.Should().ContainSingle(x => x.Sku == "test_boost");
        db.SkillNodes.Should().ContainSingle(x => x.Key == "test_skill");
        db.SeasonRewardRules.Should().ContainSingle();
        db.Questions.Should().ContainSingle(x => x.Text == "What is Synaptix?");
    }

    [Fact]
    public async Task DashboardReadinessValidator_Passes_WhenRequiredDataExists()
    {
        await using var db = NewDb();
        SeedReadinessData(db, includeQuestion: true);
        await db.SaveChangesAsync();

        var validator = NewValidator(strict: true);

        var act = async () => await validator.ValidateAsync(db, CancellationToken.None);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DashboardReadinessValidator_FailsInStrictMode_WhenCriticalSeedDataIsMissing()
    {
        await using var db = NewDb();
        SeedReadinessData(db, includeQuestion: false);
        await db.SaveChangesAsync();

        var validator = NewValidator(strict: true);

        var act = async () => await validator.ValidateAsync(db, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*questions*");
    }

    private static AppDb NewDb()
    {
        var opts = new DbContextOptionsBuilder<AppDb>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDb(opts, dispatcher: null);
    }

    private static DashboardReadinessValidator NewValidator(bool strict) =>
        new(
            Microsoft.Extensions.Options.Options.Create(new MigrationServiceOptions
            {
                DashboardReadiness = new MigrationServiceOptions.DashboardReadinessOptions
                {
                    Enabled = true,
                    Strict = strict
                }
            }),
            Config(("SuperAdmin:Email", "admin@tycoon.local")));

    private static IConfiguration Config(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value)))
            .Build();

    private static string CreateSeedRoot()
    {
        var root = Path.Combine(Path.GetTempPath(), "synaptix-seeds-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "seeds"));
        return root;
    }

    private static async Task WriteSeedFilesAsync(string root)
    {
        await File.WriteAllTextAsync(Path.Combine(root, "seeds", "store-items.json"), StoreItemsJson);
        await File.WriteAllTextAsync(Path.Combine(root, "seeds", "skill-nodes.json"), SkillNodesJson);
        await File.WriteAllTextAsync(Path.Combine(root, "seeds", "season-rewards.json"), SeasonRewardsJson);
        await File.WriteAllTextAsync(Path.Combine(root, "seeds", "questions.json"), QuestionsJson);
    }

    private static void SeedReadinessData(AppDb db, bool includeQuestion)
    {
        db.Tiers.Add(new Tier("Neural Initiate", 1, 0, 999));
        db.Missions.Add(new Mission("Daily", "daily_test", "Daily Test", "Complete a test mission.", 1, 10));
        db.StoreItems.Add(new StoreItem { Sku = "test_boost", Name = "Test Boost", ItemType = "Boost", IsActive = true });
        db.SkillNodes.Add(new SkillNode("test_skill", SkillBranch.Knowledge, 1, "Test Skill", "Test skill.", "[]", "[]", "{}"));
        db.SeasonRewardRules.Add(new SeasonRewardRule(1, 10, 100, 50));
        db.Users.Add(new User("admin@tycoon.local", "superadmin", BCrypt.Net.BCrypt.HashPassword("ChangeMe123!")));
        db.AdminEmailAcls.Add(new AdminEmailAcl("admin@tycoon.local", AdminAclListType.Allow, AdminRole.SuperAdmin, "test"));

        if (includeQuestion)
            db.Questions.Add(new Question("What is Synaptix?", "General", QuestionDifficulty.Easy, "A", null));
    }

    private const string StoreItemsJson = """
    [
      {
        "sku": "test_boost",
        "name": "Test Boost",
        "description": "A test boost.",
        "itemType": "Boost",
        "priceCoins": 10,
        "priceDiamonds": 0,
        "grantQuantity": 1,
        "maxPerPlayer": 5,
        "isActive": true,
        "sortOrder": 1,
        "mediaKey": null,
        "thumbnailUrl": null,
        "isFeatured": false,
        "version": "test"
      }
    ]
    """;

    private const string SkillNodesJson = """
    [
      {
        "key": "test_skill",
        "branch": "Knowledge",
        "tier": 1,
        "title": "Test Skill",
        "description": "A test skill.",
        "prereqKeys": [],
        "costs": [{ "currency": "Coins", "amount": 10 }],
        "effects": { "accuracy": 0.01 }
      }
    ]
    """;

    private const string SeasonRewardsJson = """
    [
      { "tier": 1, "maxTierRank": 10, "rewardXp": 100, "rewardCoins": 50 }
    ]
    """;

    private const string QuestionsJson = """
    [
      {
        "text": "What is Synaptix?",
        "category": "General",
        "difficulty": "Easy",
        "correctOptionId": "A",
        "mediaKey": null,
        "options": [
          { "optionId": "A", "text": "A learning platform" },
          { "optionId": "B", "text": "A database" }
        ],
        "tags": ["test"],
        "status": "Approved"
      }
    ]
    """;

    private sealed class FakeObjectStorage : IObjectStorage
    {
        private readonly Dictionary<string, byte[]> _objects = new(StringComparer.OrdinalIgnoreCase);

        public Task PutAsync(string key, Stream content, string contentType, long size = -1, CancellationToken ct = default)
        {
            using var ms = new MemoryStream();
            content.CopyTo(ms);
            _objects[key] = ms.ToArray();
            return Task.CompletedTask;
        }

        public void PutJson(string key, string json) =>
            _objects[key] = Encoding.UTF8.GetBytes(json);

        public string GetPublicUrl(string key) => $"/{key}";

        public Task<Stream?> GetAsync(string key, CancellationToken ct = default) =>
            Task.FromResult<Stream?>(_objects.TryGetValue(key, out var bytes) ? new MemoryStream(bytes) : null);
    }
}
