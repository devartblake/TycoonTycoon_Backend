using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Synaptix.Backend.Api.Features.AdminMongo;

public static class AdminMongoEndpoints
{
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
                result.Add(new MongoCollectionStatus(db.DatabaseNamespace.DatabaseName, name, false, 0, []));
                continue;
            }

            var collection = db.GetCollection<BsonDocument>(name);
            var count = await collection.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty, cancellationToken: ct);
            var indexes = await (await collection.Indexes.ListAsync(ct)).ToListAsync(ct);
            var indexNames = indexes
                .Select(x => x.TryGetValue("name", out var value) ? value.AsString : "(unnamed)")
                .OrderBy(x => x, StringComparer.Ordinal)
                .ToArray();

            if (count == 0)
            {
                warnings.Add($"{db.DatabaseNamespace.DatabaseName}.{name} is empty.");
            }

            result.Add(new MongoCollectionStatus(db.DatabaseNamespace.DatabaseName, name, true, count, indexNames));
        }

        return result;
    }

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
        IReadOnlyList<string> Indexes);
}
