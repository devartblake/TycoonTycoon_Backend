using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Synaptix.Backend.Api.Features.AdminMongo;

public static class AdminMongoEndpoints
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> ExpectedIndexes = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
    {
        ["analytics_events"] =
        [
            "_id_",
            "ux_analytics_events_event_id",
            "ix_analytics_events_user_received",
            "ix_analytics_events_type_received",
        ],
        ["question_answered_events"] =
        [
            "_id_",
            "ux_question_answered_events_player_question_answered",
            "ix_question_answered_events_player_answered",
            "ix_question_answered_events_match",
        ],
        ["qa_daily_rollups"] =
        [
            "_id_",
            "ix_qa_daily_rollups_day_dimensions",
            "ix_qa_daily_rollups_updated",
        ],
        ["qa_player_daily_rollups"] =
        [
            "_id_",
            "ix_qa_player_daily_rollups_player_day",
            "ix_qa_player_daily_rollups_day",
            "ix_qa_player_daily_rollups_updated",
        ],
        ["crypto_settlements"] =
        [
            "_id_",
            "ux_crypto_settlements_withdrawal_id",
            "ix_crypto_settlements_status_created",
            "ix_crypto_settlements_player_created",
        ],
    };

    private static readonly string[] AnalyticsCollections =
    [
        "analytics_events",
        "question_answered_events",
        "qa_daily_rollups",
        "qa_player_daily_rollups",
    ];

    private static readonly string[] CryptoCollections =
    [
        "crypto_settlements",
    ];

    public static void Map(RouteGroupBuilder admin)
    {
        var g = admin.MapGroup("/mongodb").WithTags("Admin/MongoDB");
        g.MapGet("/status", GetStatus);
    }

    private static async Task<IResult> GetStatus(IConfiguration cfg, CancellationToken ct)
    {
        var connectionString = cfg.GetConnectionString("mongo") ?? cfg["Mongo:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return Results.Ok(new MongoStatusResponse(
                OverallStatus: "degraded",
                Configured: false,
                AnalyticsDatabase: cfg["Mongo:Database"] ?? "synaptix_analytics",
                CryptoDatabase: cfg["Mongo:CryptoDatabase"] ?? "synaptix_crypto",
                Collections: [],
                Warnings: ["Mongo connection string is not configured."],
                ServerVersion: null));
        }

        var analyticsDb = cfg["Mongo:Database"]
            ?? cfg["MongoSettings:Database"]
            ?? Environment.GetEnvironmentVariable("MONGO_ANALYTICS_DB")
            ?? "synaptix_analytics";
        var cryptoDb = cfg["Mongo:CryptoDatabase"]
            ?? Environment.GetEnvironmentVariable("MONGO_CRYPTO_DB")
            ?? "synaptix_crypto";

        var warnings = new List<string>();
        var collections = new List<MongoCollectionStatus>();
        string? version = null;

        try
        {
            var client = new MongoClient(connectionString);
            var admin = client.GetDatabase("admin");
            var buildInfo = await admin.RunCommandAsync<BsonDocument>(new BsonDocument("buildInfo", 1), cancellationToken: ct);
            version = buildInfo.GetValue("version", BsonNull.Value).IsBsonNull ? null : buildInfo["version"].AsString;

            collections.AddRange(await DescribeCollections(client.GetDatabase(analyticsDb), AnalyticsCollections, warnings, ct));
            collections.AddRange(await DescribeCollections(client.GetDatabase(cryptoDb), CryptoCollections, warnings, ct));

            var overall = warnings.Count == 0 ? "healthy" : "degraded";
            return Results.Ok(new MongoStatusResponse(overall, true, analyticsDb, cryptoDb, collections, warnings, version));
        }
        catch (Exception ex)
        {
            warnings.Add(ex.Message);
            return Results.Ok(new MongoStatusResponse("offline", true, analyticsDb, cryptoDb, collections, warnings, version));
        }
    }

    private static async Task<IReadOnlyList<MongoCollectionStatus>> DescribeCollections(
        IMongoDatabase db,
        IReadOnlyList<string> expectedCollections,
        List<string> warnings,
        CancellationToken ct)
    {
        var existing = await (await db.ListCollectionNamesAsync(cancellationToken: ct)).ToListAsync(ct);
        var result = new List<MongoCollectionStatus>();

        foreach (var name in expectedCollections)
        {
            if (!existing.Contains(name, StringComparer.Ordinal))
            {
                warnings.Add($"{db.DatabaseNamespace.DatabaseName}.{name} is missing.");
                result.Add(new MongoCollectionStatus(
                    db.DatabaseNamespace.DatabaseName,
                    name,
                    false,
                    0,
                    [],
                    0,
                    0,
                    0,
                    null,
                    [],
                    GetExpectedIndexes(name),
                    GetExpectedIndexes(name),
                    [$"{db.DatabaseNamespace.DatabaseName}.{name} is missing."]));
                continue;
            }

            var collection = db.GetCollection<BsonDocument>(name);
            var count = await collection.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty, cancellationToken: ct);
            var indexes = await (await collection.Indexes.ListAsync(ct)).ToListAsync(ct);
            var indexDetails = indexes.Select(ToIndexStatus).OrderBy(x => x.Name, StringComparer.Ordinal).ToArray();
            var indexNames = indexDetails.Select(x => x.Name).ToArray();
            var expectedIndexes = GetExpectedIndexes(name);
            var missingIndexes = expectedIndexes.Except(indexNames, StringComparer.Ordinal).ToArray();
            var collectionWarnings = new List<string>();

            if (count == 0)
            {
                collectionWarnings.Add($"{db.DatabaseNamespace.DatabaseName}.{name} is empty; this is expected before analytics smoke or gameplay writes valid events.");
            }

            foreach (var indexName in missingIndexes)
            {
                collectionWarnings.Add($"{db.DatabaseNamespace.DatabaseName}.{name} missing index {indexName}.");
            }

            var stats = await GetCollectionStats(db, name, collectionWarnings, ct);
            warnings.AddRange(collectionWarnings);
            result.Add(new MongoCollectionStatus(
                db.DatabaseNamespace.DatabaseName,
                name,
                true,
                count,
                indexNames,
                stats.SizeBytes,
                stats.StorageSizeBytes,
                stats.TotalIndexSizeBytes,
                stats.AverageObjectSizeBytes,
                indexDetails,
                expectedIndexes,
                missingIndexes,
                collectionWarnings));
        }

        return result;
    }

    private static IReadOnlyList<string> GetExpectedIndexes(string collectionName)
        => ExpectedIndexes.TryGetValue(collectionName, out var indexes) ? indexes : [];

    private static MongoIndexStatus ToIndexStatus(BsonDocument index)
    {
        var name = index.TryGetValue("name", out var nameValue) && nameValue.IsString ? nameValue.AsString : "(unnamed)";
        var keyPattern = index.TryGetValue("key", out var keyValue) && keyValue.IsBsonDocument
            ? keyValue.AsBsonDocument.Elements.ToDictionary(x => x.Name, x => x.Value.ToString() ?? string.Empty, StringComparer.Ordinal)
            : new Dictionary<string, string>(StringComparer.Ordinal);

        return new MongoIndexStatus(
            name,
            keyPattern,
            GetBoolean(index, "unique"),
            GetBoolean(index, "sparse"),
            GetInt(index, "expireAfterSeconds"),
            index.TryGetValue("partialFilterExpression", out var partial) && partial.IsBsonDocument ? partial.AsBsonDocument.ToJson() : null,
            null);
    }

    private static async Task<MongoCollectionStats> GetCollectionStats(IMongoDatabase db, string collectionName, List<string> warnings, CancellationToken ct)
    {
        try
        {
            var stats = await db.RunCommandAsync<BsonDocument>(
                new BsonDocument
                {
                    ["collStats"] = collectionName,
                    ["scale"] = 1,
                },
                cancellationToken: ct);

            return new MongoCollectionStats(
                GetLong(stats, "size"),
                GetLong(stats, "storageSize"),
                GetLong(stats, "totalIndexSize"),
                GetNullableDouble(stats, "avgObjSize"));
        }
        catch (Exception ex)
        {
            warnings.Add($"{db.DatabaseNamespace.DatabaseName}.{collectionName} stats unavailable: {ex.Message}");
            return new MongoCollectionStats(0, 0, 0, null);
        }
    }

    private static bool GetBoolean(BsonDocument doc, string key)
        => doc.TryGetValue(key, out var value) && value.IsBoolean && value.AsBoolean;

    private static int? GetInt(BsonDocument doc, string key)
        => doc.TryGetValue(key, out var value) && value.IsNumeric ? value.ToInt32() : null;

    private static long GetLong(BsonDocument doc, string key)
        => doc.TryGetValue(key, out var value) && value.IsNumeric ? value.ToInt64() : 0;

    private static double? GetNullableDouble(BsonDocument doc, string key)
        => doc.TryGetValue(key, out var value) && value.IsNumeric ? value.ToDouble() : null;

    private sealed record MongoStatusResponse(
        string OverallStatus,
        bool Configured,
        string AnalyticsDatabase,
        string CryptoDatabase,
        IReadOnlyList<MongoCollectionStatus> Collections,
        IReadOnlyList<string> Warnings,
        string? ServerVersion);

    private sealed record MongoCollectionStatus(
        string Database,
        string Collection,
        bool Exists,
        long Count,
        IReadOnlyList<string> Indexes,
        long SizeBytes,
        long StorageSizeBytes,
        long TotalIndexSizeBytes,
        double? AverageObjectSizeBytes,
        IReadOnlyList<MongoIndexStatus> IndexDetails,
        IReadOnlyList<string> ExpectedIndexes,
        IReadOnlyList<string> MissingIndexes,
        IReadOnlyList<string> Warnings);

    private sealed record MongoIndexStatus(
        string Name,
        IReadOnlyDictionary<string, string> KeyPattern,
        bool Unique,
        bool Sparse,
        int? ExpireAfterSeconds,
        string? PartialFilterExpression,
        string? Status);

    private sealed record MongoCollectionStats(
        long SizeBytes,
        long StorageSizeBytes,
        long TotalIndexSizeBytes,
        double? AverageObjectSizeBytes);
}
