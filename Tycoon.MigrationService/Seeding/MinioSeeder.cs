using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Backend.Infrastructure.Persistence;
using Tycoon.MigrationService.Options;
using Tycoon.MigrationService.Seeding.SeedModels;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.MigrationService.Seeding;

public sealed class MinioSeeder
{
    private readonly IObjectStorage _storage;
    private readonly MinioSeedOptions _seedOptions;
    private readonly MigrationServiceOptions _migrationOptions;
    private readonly Serilog.ILogger _log;

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public MinioSeeder(IObjectStorage storage, 
        IOptions<MinioSeedOptions> seedOptions,
        IOptions<MigrationServiceOptions> migrationOptions)
    {
        _storage = storage;
        _seedOptions = seedOptions.Value;
        _migrationOptions = migrationOptions.Value;
        _log = Log.ForContext<MinioSeeder>();
    }

    public async Task SeedAsync(AppDb db, CancellationToken ct)
    {
        // Read all seed files concurrently before touching the DB.
        var storeTask    = ReadJsonAsync<List<StoreItemSeedModel>>(_seedOptions.StoreItemsKey, ct);
        var skillTask    = ReadJsonAsync<List<SkillNodeSeedModel>>(_seedOptions.SkillNodesKey, ct);
        var seasonTask   = ReadJsonAsync<List<SeasonRewardSeedModel>>(_seedOptions.SeasonRewardsKey, ct);
        var questionTask = ReadJsonAsync<List<QuestionSeedModel>>(_seedOptions.QuestionsKey, ct);
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

        var seedSkus = models.Select(GetStoreSku)
            .Where(sku => !string.IsNullOrWhiteSpace(sku))
            .Select(sku => sku!)
            .ToList();
        var existingMap = await db.StoreItems
            .Where(x => seedSkus.Contains(x.Sku))
            .ToDictionaryAsync(x => x.Sku, ct);

        var seeded = 0;
        var updated = 0;

        foreach (var m in models)
        {
            var sku = GetStoreSku(m);
            if (string.IsNullOrWhiteSpace(sku))
            {
                _log.Warning("MinioSeeder: store item missing sku/id — skipping.");
                continue;
            }

            if (existingMap.TryGetValue(sku, out var existing))
            {
                ApplyStoreItemFields(existing, m);
                updated++;
            }
            else
            {
                var item = new StoreItem { Sku = sku };
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
        item.ItemType      = m.ItemType ?? m.Category ?? "catalog";
        item.PriceCoins    = m.PriceCoins > 0 ? m.PriceCoins : PriceForCurrency(m, "coins");
        item.PriceDiamonds = m.PriceDiamonds > 0 ? m.PriceDiamonds : PriceForCurrency(m, "diamonds");
        item.GrantQuantity = FirstPositive(m.GrantQuantity, m.Quantity, m.Amount, 1);
        item.MaxPerPlayer  = m.MaxPerPlayer;
        item.IsActive      = m.Active ?? m.IsActive;
        item.SortOrder     = m.SortOrder;
        item.MediaKey      = m.MediaKey ?? m.IconPath;
        item.ThumbnailUrl  = m.ThumbnailUrl ?? m.IconPath;
        item.IsFeatured    = m.IsFeatured;
        item.Version       = m.Version;
        item.UpdatedAtUtc  = DateTimeOffset.UtcNow;
    }

    // ── Skill Nodes ──────────────────────────────────────────────────────────

    private async Task SeedSkillNodesAsync(AppDb db, List<SkillNodeSeedModel>? models, CancellationToken ct)
    {
        if (models is null) return;

        _log.Information("MinioSeeder: seeding {Count} skill nodes…", models.Count);

        var seedKeys = models.Select(GetSkillKey)
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .Select(key => key!)
            .ToList();
        var existingMap = await db.SkillNodes
            .Where(x => seedKeys.Contains(x.Key))
            .ToDictionaryAsync(x => x.Key, ct);

        var seeded = 0;
        var updated = 0;

        foreach (var m in models)
        {
            var key = GetSkillKey(m);
            if (string.IsNullOrWhiteSpace(key))
            {
                _log.Warning("MinioSeeder: skill node missing key/id — skipping.");
                continue;
            }

            if (!TryParseSkillBranch(m.Branch ?? m.Category, out var branch))
            {
                _log.Warning("MinioSeeder: unknown SkillBranch '{Branch}' for node '{Key}' — skipping.", m.Branch ?? m.Category, key);
                continue;
            }

            var prereqJson  = JsonSerializer.Serialize(m.PrereqKeys  ?? Array.Empty<string>(),             JsonOpts);
            var costsJson   = JsonSerializer.Serialize(MapCosts(m),                                         JsonOpts);
            var effectsJson = JsonSerializer.Serialize(m.Effects ?? new Dictionary<string, double>(),       JsonOpts);

            if (existingMap.TryGetValue(key, out var existing))
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
                db.SkillNodes.Add(new SkillNode(key, branch, m.Tier, m.Title, m.Description,
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

        var seedTexts = models
            .Select(m => (m.Text ?? m.Question ?? string.Empty).Trim())
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .ToList();
        var existingMap = await db.Questions
            .Where(q => seedTexts.Contains(q.Text))
            .Include(q => q.Options)
            .Include(q => q.Tags)
            .ToDictionaryAsync(q => q.Text, ct);

        var seeded = 0;
        var updated = 0;

        foreach (var m in models)
        {
            if (!TryParseQuestionDifficulty(m.Difficulty, out var difficulty))
            {
                _log.Warning("MinioSeeder: unknown difficulty '{Difficulty}' for question — skipping.", m.Difficulty);
                continue;
            }

            var normalizedText = (m.Text ?? m.Question ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedText))
            {
                _log.Warning("MinioSeeder: question missing text/question — skipping.");
                continue;
            }

            if (existingMap.TryGetValue(normalizedText, out var existing))
            {
                existing.Update(normalizedText, m.Category, difficulty, ResolveCorrectOptionId(m), m.MediaKey);
                ApplyQuestionRelations(existing, m);
                updated++;
            }
            else
            {
                var q = new Question(normalizedText, m.Category, difficulty, ResolveCorrectOptionId(m), m.MediaKey);
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
        var options = NormalizeOptions(m).ToArray();
        if (options.Length > 0)
            question.ReplaceOptions(options.Select(o => new QuestionOption(question.Id, o.OptionId!, o.Text)));

        if (m.Tags.Length > 0)
            question.ReplaceTags(m.Tags);

        if (!string.IsNullOrWhiteSpace(m.Status))
            question.SetStatus(m.Status);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<T?> ReadJsonAsync<T>(string key, CancellationToken ct)
    {
        var seedSource = ParseSeedSource(_migrationOptions.SeedSource);

        if (seedSource is SeedSource.Bundled)
            return await ReadBundledJsonAsync<T>(key, ct);

        Stream? stream = null;
        try
        {
            stream = await _storage.GetAsync(key, ct);
        }
        catch (Exception ex) when (seedSource is SeedSource.Auto)
        {
            _log.Warning(ex, "MinioSeeder: failed to read '{Key}' from object storage; trying bundled seed file.", key);
        }

        if (stream is null)
        {
            if (seedSource is SeedSource.Auto)
            {
                _log.Information("MinioSeeder: seed file '{Key}' not found in object storage; trying bundled seed file.", key);
                return await ReadBundledJsonAsync<T>(key, ct);
            }

            _log.Information("MinioSeeder: seed file '{Key}' not found in object storage — skipping.", key);
            return default;
        }

        await using (stream)
        {
            return await DeserializeSeedAsync<T>(stream, key, ct);
        }
    }

    private async Task<T?> ReadBundledJsonAsync<T>(string key, CancellationToken ct)
    {
        var root = string.IsNullOrWhiteSpace(_seedOptions.BundledRootPath)
            ? AppContext.BaseDirectory
            : _seedOptions.BundledRootPath!;

        var rootPath = Path.GetFullPath(root);
        var fullPath = Path.GetFullPath(Path.Combine(rootPath, key.Replace('/', Path.DirectorySeparatorChar)));
        var relativePath = Path.GetRelativePath(rootPath, fullPath);
        if (relativePath.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relativePath))
            throw new InvalidOperationException($"Seed path '{key}' resolves outside bundled seed root.");

        if (!File.Exists(fullPath))
        {
            _log.Information("MinioSeeder: bundled seed file '{Path}' not found — skipping.", fullPath);
            return default;
        }

        await using var stream = File.OpenRead(fullPath);
        return await DeserializeSeedAsync<T>(stream, fullPath, ct);
    }

    private async Task<T?> DeserializeSeedAsync<T>(Stream stream, string source, CancellationToken ct)
    {
        try
        {
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
            var normalized = NormalizeSeedJson<T>(document.RootElement);
            return normalized.Deserialize<T>(JsonOpts);
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "MinioSeeder: failed to deserialize '{Source}' — skipping.", source);
            return default;
        }
    }

    private static SeedSource ParseSeedSource(string? value) =>
        Enum.TryParse<SeedSource>(value, ignoreCase: true, out var parsed) ? parsed : SeedSource.Auto;

    private enum SeedSource
    {
        Auto,
        Bundled,
        MinIO
    }

    private static JsonElement NormalizeSeedJson<T>(JsonElement root)
    {
        if (typeof(T) == typeof(List<SkillNodeSeedModel>) && root.ValueKind == JsonValueKind.Object &&
            root.TryGetProperty("nodes", out var nodes))
            return nodes;

        if (typeof(T) == typeof(List<SeasonRewardSeedModel>) && root.ValueKind == JsonValueKind.Object &&
            root.TryGetProperty("dailyTierRewards", out var dailyTierRewards))
            return NormalizeSeasonRewards(dailyTierRewards);

        return root;
    }

    private static JsonElement NormalizeSeasonRewards(JsonElement dailyTierRewards)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartArray();
            foreach (var reward in dailyTierRewards.EnumerateArray())
            {
                writer.WriteStartObject();
                writer.WriteNumber("tier", TierNameToNumber(reward.TryGetProperty("tier", out var tier) ? tier.GetString() : null));
                writer.WriteNumber("maxTierRank", reward.TryGetProperty("rankMax", out var rankMax) ? rankMax.GetInt32() : 0);
                writer.WriteNumber("rewardXp", reward.TryGetProperty("xp", out var xp) ? xp.GetInt32() : 0);
                writer.WriteNumber("rewardCoins", reward.TryGetProperty("coins", out var coins) ? coins.GetInt32() : 0);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
        }

        return JsonDocument.Parse(stream.ToArray()).RootElement.Clone();
    }

    private static IReadOnlyList<SkillCostDto> MapCosts(SkillNodeSeedModel model)
    {
        if (model.Costs is null or { Length: 0 })
        {
            return model.Cost > 0
                ? [new SkillCostDto(CurrencyType.Coins, model.Cost)]
                : Array.Empty<SkillCostDto>();
        }

        return model.Costs
            .Select(c => new SkillCostDto(
                Enum.TryParse<CurrencyType>(c.Currency, ignoreCase: true, out var currency)
                    ? currency
                    : CurrencyType.Coins,
                c.Amount))
            .ToArray();
    }

    private static string? GetStoreSku(StoreItemSeedModel model) => FirstNonBlank(model.Sku, model.Id);

    private static string? GetSkillKey(SkillNodeSeedModel model) => FirstNonBlank(model.Key, model.Id);

    private static string? FirstNonBlank(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

    private static int FirstPositive(params int[] values) => values.FirstOrDefault(v => v > 0);

    private static int PriceForCurrency(StoreItemSeedModel model, string currency) =>
        string.Equals(model.Currency, currency, StringComparison.OrdinalIgnoreCase) && model.Price is > 0
            ? (int)Math.Round(model.Price.Value, MidpointRounding.AwayFromZero)
            : 0;

    private static bool TryParseSkillBranch(string? value, out SkillBranch branch)
    {
        if (Enum.TryParse(value, ignoreCase: true, out branch))
            return true;

        branch = value?.Trim().ToLowerInvariant() switch
        {
            "scholar" => SkillBranch.Knowledge,
            "knowledge" => SkillBranch.Knowledge,
            "strategist" => SkillBranch.Strategy,
            "strategy" => SkillBranch.Strategy,
            "power-up" => SkillBranch.Powerups,
            "powerup" => SkillBranch.Powerups,
            "powerups" => SkillBranch.Powerups,
            _ => default
        };
        return branch != default;
    }

    private static bool TryParseQuestionDifficulty(JsonElement value, out QuestionDifficulty difficulty)
    {
        if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var numeric) &&
            Enum.IsDefined(typeof(QuestionDifficulty), numeric))
        {
            difficulty = (QuestionDifficulty)numeric;
            return true;
        }

        if (value.ValueKind == JsonValueKind.String &&
            Enum.TryParse(value.GetString(), ignoreCase: true, out difficulty))
            return true;

        difficulty = default;
        return false;
    }

    private static IReadOnlyList<QuestionOptionSeedModel> NormalizeOptions(QuestionSeedModel model)
    {
        var source = model.Options.Length > 0 ? model.Options : model.Answers;
        for (var i = 0; i < source.Length; i++)
            source[i].OptionId ??= ((char)('A' + i)).ToString();

        return source;
    }

    private static string ResolveCorrectOptionId(QuestionSeedModel model)
    {
        if (!string.IsNullOrWhiteSpace(model.CorrectOptionId))
            return model.CorrectOptionId;

        var options = NormalizeOptions(model);
        var correct = options.FirstOrDefault(o => o.IsCorrect) ??
                      options.FirstOrDefault(o => string.Equals(o.Text, model.CorrectAnswer, StringComparison.OrdinalIgnoreCase));

        return correct?.OptionId ?? options.FirstOrDefault()?.OptionId ?? "A";
    }

    private static int TierNameToNumber(string? value) =>
        value?.Trim().ToLowerInvariant() switch
        {
            "bronze" => 1,
            "silver" => 2,
            "gold" => 3,
            "platinum" => 4,
            "diamond" => 5,
            "legend" => 6,
            _ => 1
        };

    private static void SetPrivate(object target, string propertyName, object? value) =>
        target.GetType().GetProperty(propertyName)!.SetValue(target, value);
}
