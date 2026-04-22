using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Backend.Infrastructure.Persistence;
using Tycoon.MigrationService.Seeding.SeedModels;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.MigrationService.Seeding;

/// <summary>
/// Reads JSON seed files from MinIO object storage and upserts catalog data into the database.
/// Each seed key is checked for existence; if the MinIO object is not found the step is skipped gracefully.
/// </summary>
public sealed class MinioSeeder
{
    private readonly IObjectStorage _storage;
    private readonly Serilog.ILogger _log;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private const string StoreItemsKey   = "seeds/store-items.json";
    private const string SkillNodesKey   = "seeds/skill-nodes.json";
    private const string SeasonRewardsKey = "seeds/season-rewards.json";
    private const string QuestionsKey    = "seeds/questions.json";

    public MinioSeeder(IObjectStorage storage)
    {
        _storage = storage;
        _log = Log.ForContext<MinioSeeder>();
    }

    public async Task SeedAsync(AppDb db, CancellationToken ct)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await SeedStoreItemsAsync(db, ct);
            await SeedSkillNodesAsync(db, ct);
            await SeedSeasonRewardsAsync(db, ct);
            await SeedQuestionsAsync(db, ct);
        });
    }

    // ── Store Items ──────────────────────────────────────────────────────────

    private async Task SeedStoreItemsAsync(AppDb db, CancellationToken ct)
    {
        var models = await ReadJsonAsync<List<StoreItemSeedModel>>(StoreItemsKey, ct);
        if (models is null) return;

        _log.Information("MinioSeeder: seeding {Count} store items…", models.Count);
        var seeded = 0;
        var updated = 0;

        foreach (var m in models)
        {
            var existing = await db.StoreItems.FirstOrDefaultAsync(x => x.Sku == m.Sku, ct);
            if (existing is null)
            {
                db.StoreItems.Add(new StoreItem
                {
                    Sku          = m.Sku,
                    Name         = m.Name,
                    Description  = m.Description ?? string.Empty,
                    ItemType     = m.ItemType,
                    PriceCoins   = m.PriceCoins,
                    PriceDiamonds = m.PriceDiamonds,
                    GrantQuantity = m.GrantQuantity > 0 ? m.GrantQuantity : 1,
                    MaxPerPlayer = m.MaxPerPlayer,
                    IsActive     = m.IsActive,
                    SortOrder    = m.SortOrder,
                    MediaKey     = m.MediaKey,
                    ThumbnailUrl = m.ThumbnailUrl,
                    IsFeatured   = m.IsFeatured,
                    Version      = m.Version,
                    UpdatedAtUtc = DateTimeOffset.UtcNow,
                });
                seeded++;
            }
            else
            {
                existing.Name         = m.Name;
                existing.Description  = m.Description ?? string.Empty;
                existing.ItemType     = m.ItemType;
                existing.PriceCoins   = m.PriceCoins;
                existing.PriceDiamonds = m.PriceDiamonds;
                existing.GrantQuantity = m.GrantQuantity > 0 ? m.GrantQuantity : 1;
                existing.MaxPerPlayer = m.MaxPerPlayer;
                existing.IsActive     = m.IsActive;
                existing.SortOrder    = m.SortOrder;
                existing.MediaKey     = m.MediaKey;
                existing.ThumbnailUrl = m.ThumbnailUrl;
                existing.IsFeatured   = m.IsFeatured;
                existing.Version      = m.Version;
                existing.UpdatedAtUtc = DateTimeOffset.UtcNow;
                updated++;
            }
        }

        await db.SaveChangesAsync(ct);
        _log.Information("MinioSeeder: store items — {Seeded} created, {Updated} updated.", seeded, updated);
    }

    // ── Skill Nodes ──────────────────────────────────────────────────────────

    private async Task SeedSkillNodesAsync(AppDb db, CancellationToken ct)
    {
        var models = await ReadJsonAsync<List<SkillNodeSeedModel>>(SkillNodesKey, ct);
        if (models is null) return;

        _log.Information("MinioSeeder: seeding {Count} skill nodes…", models.Count);
        var seeded = 0;
        var updated = 0;

        foreach (var m in models)
        {
            if (!Enum.TryParse<SkillBranch>(m.Branch, ignoreCase: true, out var branch))
            {
                _log.Warning("MinioSeeder: unknown SkillBranch '{Branch}' for node '{Key}' — skipping.", m.Branch, m.Key);
                continue;
            }

            var prereqJson  = JsonSerializer.Serialize(m.PrereqKeys  ?? Array.Empty<string>(),                   JsonOpts);
            var costsJson   = JsonSerializer.Serialize(MapCosts(m.Costs),                                         JsonOpts);
            var effectsJson = JsonSerializer.Serialize(m.Effects ?? new Dictionary<string, double>(),             JsonOpts);

            var existing = await db.SkillNodes.FirstOrDefaultAsync(x => x.Key == m.Key, ct);
            if (existing is null)
            {
                db.SkillNodes.Add(new SkillNode(m.Key, branch, m.Tier, m.Title, m.Description,
                    prereqJson, costsJson, effectsJson));
                seeded++;
            }
            else
            {
                // SkillNode uses private setters — update via reflection (same pattern as SkillTreeService)
                SetPrivate(existing, nameof(SkillNode.Branch),        branch);
                SetPrivate(existing, nameof(SkillNode.Tier),          m.Tier);
                SetPrivate(existing, nameof(SkillNode.Title),         m.Title);
                SetPrivate(existing, nameof(SkillNode.Description),   m.Description);
                SetPrivate(existing, nameof(SkillNode.PrereqKeysJson), prereqJson);
                SetPrivate(existing, nameof(SkillNode.CostsJson),     costsJson);
                SetPrivate(existing, nameof(SkillNode.EffectsJson),   effectsJson);
                existing.Touch();
                updated++;
            }
        }

        await db.SaveChangesAsync(ct);
        _log.Information("MinioSeeder: skill nodes — {Seeded} created, {Updated} updated.", seeded, updated);
    }

    // ── Season Reward Rules ──────────────────────────────────────────────────

    private async Task SeedSeasonRewardsAsync(AppDb db, CancellationToken ct)
    {
        var models = await ReadJsonAsync<List<SeasonRewardSeedModel>>(SeasonRewardsKey, ct);
        if (models is null) return;

        _log.Information("MinioSeeder: seeding {Count} season reward rules…", models.Count);
        var seeded = 0;

        foreach (var m in models)
        {
            var exists = await db.SeasonRewardRules
                .AsNoTracking()
                .AnyAsync(x => x.Tier == m.Tier && x.MaxTierRank == m.MaxTierRank, ct);

            if (!exists)
            {
                db.SeasonRewardRules.Add(new SeasonRewardRule(m.Tier, m.MaxTierRank, m.RewardXp, m.RewardCoins));
                seeded++;
            }
        }

        await db.SaveChangesAsync(ct);
        _log.Information("MinioSeeder: season reward rules — {Seeded} created (existing rows skipped).", seeded);
    }

    // ── Questions ────────────────────────────────────────────────────────────

    private async Task SeedQuestionsAsync(AppDb db, CancellationToken ct)
    {
        var models = await ReadJsonAsync<List<QuestionSeedModel>>(QuestionsKey, ct);
        if (models is null) return;

        _log.Information("MinioSeeder: seeding {Count} questions…", models.Count);
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
            var existing = await db.Questions
                .Include(q => q.Options)
                .Include(q => q.Tags)
                .FirstOrDefaultAsync(q => q.Text == normalizedText, ct);

            if (existing is null)
            {
                var q = new Question(normalizedText, m.Category, difficulty, m.CorrectOptionId, m.MediaKey);

                if (m.Options.Length > 0)
                    q.ReplaceOptions(m.Options.Select(o => new QuestionOption(q.Id, o.OptionId, o.Text)));

                if (m.Tags.Length > 0)
                    q.ReplaceTags(m.Tags);

                if (!string.IsNullOrWhiteSpace(m.Status))
                    q.SetStatus(m.Status);

                db.Questions.Add(q);
                seeded++;
            }
            else
            {
                existing.Update(normalizedText, m.Category, difficulty, m.CorrectOptionId, m.MediaKey);

                if (m.Options.Length > 0)
                    existing.ReplaceOptions(m.Options.Select(o => new QuestionOption(existing.Id, o.OptionId, o.Text)));

                if (m.Tags.Length > 0)
                    existing.ReplaceTags(m.Tags);

                if (!string.IsNullOrWhiteSpace(m.Status))
                    existing.SetStatus(m.Status);

                updated++;
            }
        }

        await db.SaveChangesAsync(ct);
        _log.Information("MinioSeeder: questions — {Seeded} created, {Updated} updated.", seeded, updated);
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
                Enum.TryParse<CurrencyType>(c.Currency, ignoreCase: true, out var ct) ? ct : CurrencyType.Coins,
                c.Amount))
            .ToArray();
    }

    private static void SetPrivate(object target, string propertyName, object? value) =>
        target.GetType()
              .GetProperty(propertyName)!
              .SetValue(target, value);
}
