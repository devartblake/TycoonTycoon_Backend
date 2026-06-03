using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Serilog;

namespace Synaptix.Setup.Services;

public sealed class MongoSetupTask : ISetupTask
{
    public string Name => "MongoDB";

    public async Task<SetupResult> RunAsync(IConfiguration cfg, CancellationToken ct = default)
    {
        var connStr  = BuildConnectionString(cfg);
        var appDb    = cfg["MONGO_ANALYTICS_DB"] ?? "synaptix_analytics";
        var cryptoDb = cfg["MONGO_CRYPTO_DB"]    ?? "synaptix_crypto";
        var appUser  = cfg["MONGO_APP_USER"]      ?? "synaptix_app_user";
        var appPass  = cfg["MONGO_APP_PASSWORD"];

        if (string.IsNullOrWhiteSpace(connStr))
            return SetupResult.Fail("MongoDB connection string could not be constructed.");

        try
        {
            var client   = new MongoClient(connStr);
            var adminDb  = client.GetDatabase("admin");
            await adminDb.RunCommandAsync<MongoDB.Bson.BsonDocument>(
                new MongoDB.Driver.JsonCommand<MongoDB.Bson.BsonDocument>("{ping:1}"), cancellationToken: ct);
            Log.Information("MongoDB root connection verified.");

            // Ensure app databases and collections exist
            await EnsureCollectionsAsync(client, appDb, ["events", "rollups", "personalization"], ct);
            await EnsureCollectionsAsync(client, cryptoDb, ["settlements", "ledger_events"], ct);

            // Create app user if app password is provided
            if (!string.IsNullOrWhiteSpace(appPass))
                await EnsureAppUserAsync(adminDb, appUser, appPass, appDb, cryptoDb, ct);

            return SetupResult.Ok($"MongoDB provisioned — databases: {appDb}, {cryptoDb}.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "MongoDB setup failed.");
            return SetupResult.Fail($"MongoDB setup failed: {ex.Message}");
        }
    }

    private static string? BuildConnectionString(IConfiguration cfg)
    {
        var explicit_ = cfg.GetConnectionString("mongo") ?? cfg["ConnectionStrings:mongo"];
        if (!string.IsNullOrWhiteSpace(explicit_)) return explicit_;

        var host     = cfg["MONGO_HOST"] ?? "localhost";
        var port     = cfg["MONGO_PORT"] ?? "27017";
        var rootUser = cfg["MONGO_INITDB_ROOT_USERNAME"] ?? "synaptix_admin";
        var rootPass = cfg["MONGO_INITDB_ROOT_PASSWORD"];

        return string.IsNullOrWhiteSpace(rootPass)
            ? null
            : $"mongodb://{rootUser}:{Uri.EscapeDataString(rootPass)}@{host}:{port}/?authSource=admin";
    }

    private static async Task EnsureCollectionsAsync(IMongoClient client, string dbName, IEnumerable<string> collections, CancellationToken ct)
    {
        var db = client.GetDatabase(dbName);
        var existing = (await db.ListCollectionNamesAsync(cancellationToken: ct)).ToList();

        foreach (var col in collections)
        {
            if (!existing.Contains(col))
            {
                await db.CreateCollectionAsync(col, cancellationToken: ct);
                Log.Information("MongoDB: created collection {Database}.{Collection}.", dbName, col);
            }
        }
    }

    private static async Task EnsureAppUserAsync(IMongoDatabase adminDb, string user, string password,
        string analyticsDb, string cryptoDb, CancellationToken ct)
    {
        try
        {
            var cmd = new MongoDB.Bson.BsonDocument
            {
                ["createUser"] = user,
                ["pwd"]        = password,
                ["roles"]      = new MongoDB.Bson.BsonArray
                {
                    new MongoDB.Bson.BsonDocument { ["role"] = "readWrite", ["db"] = analyticsDb },
                    new MongoDB.Bson.BsonDocument { ["role"] = "readWrite", ["db"] = cryptoDb    },
                },
            };
            await adminDb.RunCommandAsync<MongoDB.Bson.BsonDocument>(new MongoDB.Driver.BsonDocumentCommand<MongoDB.Bson.BsonDocument>(cmd), cancellationToken: ct);
            Log.Information("MongoDB: created app user '{User}'.", user);
        }
        catch (MongoCommandException ex) when (ex.CodeName == "Location51003" || ex.Message.Contains("already exists"))
        {
            Log.Information("MongoDB: app user '{User}' already exists.", user);
        }
    }
}
