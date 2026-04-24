using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Backend.Infrastructure.Persistence;
using Tycoon.MigrationService.Seeding.SeedModels;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.MigrationService.Seeding;

public sealed class MinioSeeder
{
    private readonly IObjectStorage _storage;
    private readonly Serilog.ILogger _log;

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    private const string StoreItemsKey    = "seeds/store-items.json";
    private const string SkillNodesKey    = "seeds/skill-nodes.json";
    private const string SeasonRewardsKey = "seeds/season-rewards.json";
    private const string QuestionsKey     = "seeds/questions.json";

    public MinioSeeder(IObjectStorage storage)
    {
        _storage = storage;
        _log = Log.ForContext<MinioSeeder>();
    }

    public async Task SeedAsync(AppDb db, CancellationToken ct)
    {
        // Read all seed files concurrently before touching the DB.
        var storeTask    = ReadJsonAsync<List<StoreItemSeedModel>>(StoreItemsKey, ct);
        var skillTask    = ReadJsonAsync<List<SkillNodeSeedModel>>(SkillNodesKey, ct);
        var seasonTask   = ReadJsonAsync<List<SeasonRewardSeedModel>>(SeasonRewardsKey, ct);
        var questionTask = ReadJsonAsync<List<QuestionSeedModel>>(QuestionsKey, ct);
        await Task.WhenAll(storeTask, skillTask, seasonTask, questionTask);

        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await SeedStoreItemsAsync(db, await storeTask, ct);
            await SeedSkillNodesAsync(db, await skillTask, ct);
            await SeedSeasonRewardsAsync(db, await seasonTask, ct);
            await SeedQuestionsAsync(db, await questionTask, ct);
        });
    }

    // ── Store Items ──────────────────────────────────────────────────────────

    private async Task SeedStoreItemsAsync(AppDb db, List<StoreItemSeedModel>? models, CancellationToken ct)
    {
        if (models is null) return;

        _log.Information("MinioSeeder: seeding {Count} store items…", models.Count);

        var seedSkus = models.ConvertAll(m => m.Sku);
        var existingMap = await db.StoreItems
            .Where(x => seedSkus.Contains(x.Sku))
            .ToDictionaryAsync(x => x.Sku, ct);

        var seeded = 0;
        var updated = 0;

        foreach (var m in models)
        {
            if (existingMap.TryGetValue(m.Sku, out var existing))
            {
                ApplyStoreItemFields(existing, m);
                updated++;
            }
            else
            {
                var item = new StoreItem { Sku = m.Sku };
                ApplyStoreItemFields(item, m);
                db.StoreItems.Add(item);
                seeded++;
            }
        }

        await db.SaveChangesAsync(ct);
        _log.Information("MinioSeeder: store items — {Seeded} created, {Updated} updated.", seeded, updated);
    }

    private static void ApplyStoreItemFields(StoreItem item, StoreItemSeedModel m)
    {
        item.Name          = m.Name;
        item.Description   = m.Description ?? string.Empty;
        item.ItemType      = m.ItemType;
        item.PriceCoins    = m.PriceCoins;
        item.PriceDiamonds = m.PriceDiamonds;
        item.GrantQuantity = m.GrantQuantity > 0 ? m.GrantQuantity : 1;
        item.MaxPerPlayer  = m.MaxPerPlayer;
        item.IsActive      = m.IsActive;
        item.SortOrder     = m.SortOrder;
        item.MediaKey      = m.MediaKey;
        item.ThumbnailUrl  = m.ThumbnailUrl;
        item.IsFeatured    = m.IsFeatured;
        item.Version       = m.Version;
        item.UpdatedAtUtc  = DateTimeOffset.UtcNow;
    }

    // ── Skill Nodes ──────────────────────────────────────────────────────────

    private async Task SeedSkillNodesAsync(AppDb db, List<SkillNodeSeedModel>? models, CancellationToken ct)
    {
        if (models is null) return;

        _log.Information("MinioSeeder: seeding {Count} skill nodes…", models.Count);

        var seedKeys = models.ConvertAll(m => m.Key);
        var existingMap = await db.SkillNodes
            .Where(x => seedKeys.Contains(x.Key))
            .ToDictionaryAsync(x => x.Key, ct);

        var seeded = 0;
        var updated = 0;

        foreach (var m in models)
        {
            if (!Enum.TryParse<SkillBranch>(m.Branch, ignoreCase: true, out var branch))
            {
                _log.Warning("MinioSeeder: unknown SkillBranch '{Branch}' for node '{Key}' — skipping.", m.Branch, m.Key);
                continue;
            }

            var prereqJson  = JsonSerializer.Serialize(m.PrereqKeys  ?? Array.Empty<string>(),             JsonOpts);
            var costsJson   = JsonSerializer.Serialize(MapCosts(m.Costs),                                   JsonOpts);
            var effectsJson = JsonSerializer.Serialize(m.Effects ?? new Dictionary<string, double>(),       JsonOpts);

            if (existingMap.TryGetValue(m.Key, out var existing))
            {
                // SkillNode uses private setters — reflection mirrors SkillTreeService.UpsertNodesAsync
                SetPrivate(existing, nameof(SkillNode.Branch),         branch);
                SetPrivate(existing, nameof(SkillNode.Tier),           m.Tier);
                SetPrivate(existing, nameof(SkillNode.Title),          m.Title);
                SetPrivate(existing, nameof(SkillNode.Description),    m.Description);
                SetPrivate(existing, nameof(SkillNode.PrereqKeysJson), prereqJson);
                SetPrivate(existing, nameof(SkillNode.CostsJson),      costsJson);
                SetPrivate(existing, nameof(SkillNode.EffectsJson),    effectsJson);
                existing.Touch();
                updated++;
            }
            else
            {
                db.SkillNodes.Add(new SkillNode(m.Key, branch, m.Tier, m.Title, m.Description,
                    prereqJson, costsJson, effectsJson));
                seeded++;
            }
        }

        await db.SaveChangesAsync(ct);
        _log.Information("MinioSeeder: skill nodes — {Seeded} created, {Updated} updated.", seeded, updated);
    }

    // ── Season Reward Rules ──────────────────────────────────────────────────

    private async Task SeedSeasonRewardsAsync(AppDb db, List<SeasonRewardSeedModel>? models, CancellationToken ct)
    {
        if (models is null) return;

        _log.Information("MinioSeeder: seeding {Count} season reward rules…", models.Count);

        var existingPairs = await db.SeasonRewardRules
            .AsNoTracking()
            .Select(x => new { x.Tier, x.MaxTierRank })
            .ToListAsync(ct);
        var existingSet = existingPairs.Select(x => (x.Tier, x.MaxTierRank)).ToHashSet();

        var seeded = 0;

        foreach (var m in models)
        {
            if (!existingSet.Contains((m.Tier, m.MaxTierRank)))
            {
                db.SeasonRewardRules.Add(new SeasonRewardRule(m.Tier, m.MaxTierRank, m.RewardXp, m.RewardCoins));
                seeded++;
            }
        }

        await db.SaveChangesAsync(ct);
        _log.Information("MinioSeeder: season reward rules — {Seeded} created (existing rows skipped).", seeded);
    }

    // ── Questions ────────────────────────────────────────────────────────────

    private async Task SeedQuestionsAsync(AppDb db, List<QuestionSeedModel>? models, CancellationToken ct)
    {
        if (models is null) return;

        _log.Information("MinioSeeder: seeding {Count} questions…", models.Count);

        var seedTexts = models.ConvertAll(m => m.Text.Trim());
        var existingMap = await db.Questions
            .Where(q => seedTexts.Contains(q.Text))
            .Include(q => q.Options)
            .Include(q => q.Tags)
            .ToDictionaryAsync(q => q.Text, ct);

        var seeded = 0;
        var updated = 0;

        foreach (var m in models)
        {
            if (!Enum.TryParse<QuestionDifficulty>(m.Difficulty, ignoreCase: true, out var difficulty))
            {
                _log.Warning("MinioSeeder: unknown difficulty '{Difficulty}' for question — skipping.", m.Difficulty);
                continue;
            }

            var normalizedText = m.Text.Trim();

            if (existingMap.TryGetValue(normalizedText, out var existing))
            {
                existing.Update(normalizedText, m.Category, difficulty, m.CorrectOptionId, m.MediaKey);
                ApplyQuestionRelations(existing, m);
                updated++;
            }
            else
            {
                var q = new Question(normalizedText, m.Category, difficulty, m.CorrectOptionId, m.MediaKey);
                ApplyQuestionRelations(q, m);
                db.Questions.Add(q);
                seeded++;
            }
        }

        await db.SaveChangesAsync(ct);
        _log.Information("MinioSeeder: questions — {Seeded} created, {Updated} updated.", seeded, updated);
    }

    private static void ApplyQuestionRelations(Question question, QuestionSeedModel m)
    {
        if (m.Options.Length > 0)
            question.ReplaceOptions(m.Options.Select(o => new QuestionOption(question.Id, o.OptionId, o.Text)));

        if (m.Tags.Length > 0)
            question.ReplaceTags(m.Tags);

        if (!string.IsNullOrWhiteSpace(m.Status))
            question.SetStatus(m.Status);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<T?> ReadJsonAsync<T>(string key, CancellationToken ct)
    {
        await using var stream = await _storage.GetAsync(key, ct);
        if (stream is null)
        {
            _log.Information("MinioSeeder: seed file '{Key}' not found in storage — skipping.", key);
            return default;
        }

        try
        {
            return await JsonSerializer.DeserializeAsync<T>(stream, JsonOpts, ct);
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "MinioSeeder: failed to deserialize '{Key}' — skipping.", key);
            return default;
        }
    }

    private static IReadOnlyList<SkillCostDto> MapCosts(SkillCostSeedModel[]? models)
    {
        if (models is null or { Length: 0 })
            return Array.Empty<SkillCostDto>();

        return models
            .Select(c => new SkillCostDto(
                Enum.TryParse<CurrencyType>(c.Currency, ignoreCase: true, out var currency)
                    ? currency
                    : CurrencyType.Coins,
                c.Amount))
            .ToArray();
    }

    private static void SetPrivate(object target, string propertyName, object? value) =>
        target.GetType().GetProperty(propertyName)!.SetValue(target, value);
}
