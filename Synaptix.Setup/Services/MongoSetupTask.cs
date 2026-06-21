using Microsoft.Extensions.Configuration;
using MongoDB.Bson;
using MongoDB.Driver;
using Serilog;

namespace Synaptix.Setup.Services;

public sealed class MongoSetupTask : ISetupTask
{
    public string Name => "MongoDB";

    public async Task<SetupResult> RunAsync(IConfiguration cfg, CancellationToken ct = default)
    {
        var connStr  = BuildAdminConnectionString(cfg);
        var appDb    = cfg["MONGO_ANALYTICS_DB"] ?? "synaptix_analytics";
        var cryptoDb = cfg["MONGO_CRYPTO_DB"]    ?? "synaptix_crypto";
        var authDb   = ResolveAuthDatabase(cfg, appDb);
        var appUser  = cfg["MONGO_APP_USER"]      ?? "synaptix_app_user";
        var appPass  = cfg["MONGO_APP_PASSWORD"];

        if (string.IsNullOrWhiteSpace(connStr))
            return SetupResult.Fail("MongoDB admin connection string could not be constructed.");

        try
        {
            var client  = new MongoClient(connStr);
            var adminDb = client.GetDatabase("admin");
            await adminDb.RunCommandAsync<BsonDocument>(
                new JsonCommand<BsonDocument>("{ping:1}"), cancellationToken: ct);
            Log.Information("MongoDB root connection verified.");

            await EnsureAnalyticsSchemaAsync(client.GetDatabase(appDb), ct);
            await EnsureCryptoSchemaAsync(client.GetDatabase(cryptoDb), ct);

            if (!string.IsNullOrWhiteSpace(appPass))
            {
                var authDatabase = client.GetDatabase(authDb);
                await EnsureAppUserAsync(authDatabase, appUser, appPass, appDb, cryptoDb, ct);
                await ValidateAppUserAsync(connStr, appUser, appPass, authDb, appDb, ct);
                await HandleLegacyAdminUserAsync(cfg, adminDb, authDb, appUser, ct);
            }

            return SetupResult.Ok($"MongoDB provisioned - databases: {appDb}, {cryptoDb}.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "MongoDB setup failed.");
            return SetupResult.Fail($"MongoDB setup failed: {ex.Message}");
        }
    }

    internal static string? BuildAdminConnectionString(IConfiguration cfg)
    {
        var explicitConnection = cfg.GetConnectionString("mongoAdmin")
                                 ?? cfg["ConnectionStrings:mongoAdmin"]
                                 ?? cfg.GetConnectionString("mongo")
                                 ?? cfg["ConnectionStrings:mongo"];
        if (!string.IsNullOrWhiteSpace(explicitConnection)) return explicitConnection;

        var host     = cfg["MONGO_HOST"] ?? "localhost";
        var port     = int.TryParse(cfg["MONGO_PORT"], out var parsedPort) ? parsedPort : 27017;
        var rootUser = cfg["MONGO_INITDB_ROOT_USERNAME"] ?? "synaptix_admin";
        var rootPass = cfg["MONGO_INITDB_ROOT_PASSWORD"];
        if (string.IsNullOrWhiteSpace(rootPass)) return null;

        return new MongoUrlBuilder
        {
            Server = new MongoServerAddress(host, port),
            Username = rootUser,
            Password = rootPass,
            AuthenticationSource = "admin",
        }.ToString();
    }

    internal static string ResolveAuthDatabase(IConfiguration cfg, string appDb) =>
        cfg["MONGO_AUTH_DB"] ?? appDb;

    private static async Task EnsureCollectionsAsync(IMongoDatabase db, IEnumerable<string> collections, CancellationToken ct)
    {
        var existing = (await db.ListCollectionNamesAsync(cancellationToken: ct)).ToList();

        foreach (var col in collections)
        {
            if (!existing.Contains(col))
            {
                await db.CreateCollectionAsync(col, cancellationToken: ct);
                Log.Information("MongoDB: created collection {Database}.{Collection}.",
                    db.DatabaseNamespace.DatabaseName, col);
            }
        }
    }

    private static async Task EnsureAnalyticsSchemaAsync(IMongoDatabase db, CancellationToken ct)
    {
        await EnsureCollectionsAsync(
            db,
            [
                "events",
                "rollups",
                "personalization",
                "analytics_events",
                "question_answered_events",
                "qa_daily_rollups",
                "qa_player_daily_rollups",
            ],
            ct);

        await CreateIndexesAsync(db.GetCollection<BsonDocument>("analytics_events"), ct,
            new CreateIndexModel<BsonDocument>(
                new BsonDocument("event_id", 1),
                new CreateIndexOptions { Name = "ux_analytics_events_event_id", Unique = true }),
            new CreateIndexModel<BsonDocument>(
                new BsonDocument { ["user_id"] = 1, ["received_at"] = -1 },
                new CreateIndexOptions { Name = "ix_analytics_events_user_received" }),
            new CreateIndexModel<BsonDocument>(
                new BsonDocument { ["event_type"] = 1, ["received_at"] = -1 },
                new CreateIndexOptions { Name = "ix_analytics_events_type_received" }));

        await CreateIndexesAsync(db.GetCollection<BsonDocument>("question_answered_events"), ct,
            new CreateIndexModel<BsonDocument>(
                new BsonDocument { ["PlayerId"] = 1, ["QuestionId"] = 1, ["AnsweredAtUtc"] = 1 },
                new CreateIndexOptions { Name = "ux_question_answered_events_player_question_answered", Unique = true }),
            new CreateIndexModel<BsonDocument>(
                new BsonDocument { ["PlayerId"] = 1, ["AnsweredAtUtc"] = -1 },
                new CreateIndexOptions { Name = "ix_question_answered_events_player_answered" }),
            new CreateIndexModel<BsonDocument>(
                new BsonDocument("MatchId", 1),
                new CreateIndexOptions { Name = "ix_question_answered_events_match" }));

        await CreateIndexesAsync(db.GetCollection<BsonDocument>("qa_daily_rollups"), ct,
            new CreateIndexModel<BsonDocument>(
                new BsonDocument
                {
                    ["Day"] = 1,
                    ["Mode"] = 1,
                    ["Category"] = 1,
                    ["Difficulty"] = 1,
                    ["SynaptixMode"] = 1,
                    ["Surface"] = 1,
                    ["AudienceSegment"] = 1,
                    ["EntryPoint"] = 1,
                    ["BrandVersion"] = 1,
                },
                new CreateIndexOptions { Name = "ix_qa_daily_rollups_day_dimensions" }),
            new CreateIndexModel<BsonDocument>(
                new BsonDocument("UpdatedAtUtc", -1),
                new CreateIndexOptions { Name = "ix_qa_daily_rollups_updated" }));

        await CreateIndexesAsync(db.GetCollection<BsonDocument>("qa_player_daily_rollups"), ct,
            new CreateIndexModel<BsonDocument>(
                new BsonDocument { ["PlayerId"] = 1, ["Day"] = -1 },
                new CreateIndexOptions { Name = "ix_qa_player_daily_rollups_player_day" }),
            new CreateIndexModel<BsonDocument>(
                new BsonDocument("Day", 1),
                new CreateIndexOptions { Name = "ix_qa_player_daily_rollups_day" }),
            new CreateIndexModel<BsonDocument>(
                new BsonDocument("UpdatedAtUtc", -1),
                new CreateIndexOptions { Name = "ix_qa_player_daily_rollups_updated" }));

        await DropLegacyIdIndexIfPresentAsync(db.GetCollection<BsonDocument>("question_answered_events"), "ux_question_answered_events_id", ct);
        await DropLegacyIdIndexIfPresentAsync(db.GetCollection<BsonDocument>("qa_daily_rollups"), "ux_qa_daily_rollups_id", ct);
        await DropLegacyIdIndexIfPresentAsync(db.GetCollection<BsonDocument>("qa_player_daily_rollups"), "ux_qa_player_daily_rollups_id", ct);
    }

    private static async Task EnsureCryptoSchemaAsync(IMongoDatabase db, CancellationToken ct)
    {
        await EnsureCollectionsAsync(db, ["settlements", "ledger_events", "crypto_settlements"], ct);
        await CreateIndexesAsync(db.GetCollection<BsonDocument>("crypto_settlements"), ct,
            new CreateIndexModel<BsonDocument>(
                new BsonDocument("withdrawal_id", 1),
                new CreateIndexOptions { Name = "ux_crypto_settlements_withdrawal_id", Unique = true }),
            new CreateIndexModel<BsonDocument>(
                new BsonDocument { ["status"] = 1, ["created_at"] = -1 },
                new CreateIndexOptions { Name = "ix_crypto_settlements_status_created" }),
            new CreateIndexModel<BsonDocument>(
                new BsonDocument { ["player_id"] = 1, ["created_at"] = -1 },
                new CreateIndexOptions { Name = "ix_crypto_settlements_player_created" }));
    }

    private static async Task CreateIndexesAsync(
        IMongoCollection<BsonDocument> collection,
        CancellationToken ct,
        params CreateIndexModel<BsonDocument>[] indexes)
    {
        await collection.Indexes.CreateManyAsync(indexes, ct);
    }

    private static async Task DropLegacyIdIndexIfPresentAsync(
        IMongoCollection<BsonDocument> collection,
        string indexName,
        CancellationToken ct)
    {
        var indexes = await (await collection.Indexes.ListAsync(ct)).ToListAsync(ct);
        var legacy = indexes.FirstOrDefault(x =>
            x.TryGetValue("name", out var name) &&
            name.IsString &&
            name.AsString == indexName &&
            x.TryGetValue("key", out var key) &&
            key.IsBsonDocument &&
            key.AsBsonDocument.ElementCount == 1 &&
            key.AsBsonDocument.TryGetValue("Id", out var direction) &&
            direction.IsNumeric &&
            direction.ToInt32() == 1);

        if (legacy is null)
            return;

        await collection.Indexes.DropOneAsync(indexName, ct);
        Log.Information("MongoDB: dropped legacy index {Collection}.{IndexName}; event Id is stored as MongoDB _id.",
            collection.CollectionNamespace.CollectionName,
            indexName);
    }

    private static async Task EnsureAppUserAsync(IMongoDatabase authDb, string user, string password,
        string analyticsDb, string cryptoDb, CancellationToken ct)
    {
        var roles = new BsonArray
        {
            new BsonDocument { ["role"] = "readWrite", ["db"] = analyticsDb },
            new BsonDocument { ["role"] = "readWrite", ["db"] = cryptoDb },
        };

        try
        {
            var cmd = new BsonDocument
            {
                ["createUser"] = user,
                ["pwd"] = password,
                ["roles"] = roles,
            };
            await authDb.RunCommandAsync<BsonDocument>(
                new BsonDocumentCommand<BsonDocument>(cmd), cancellationToken: ct);
            Log.Information("MongoDB: created app user '{User}' in auth database '{AuthDatabase}'.",
                user, authDb.DatabaseNamespace.DatabaseName);
        }
        catch (MongoCommandException ex) when (
            ex.CodeName is "Location51003" or "UserAlreadyExists" ||
            ex.Message.Contains("already exists", StringComparison.OrdinalIgnoreCase))
        {
            var cmd = new BsonDocument
            {
                ["updateUser"] = user,
                ["pwd"] = password,
                ["roles"] = roles,
            };
            await authDb.RunCommandAsync<BsonDocument>(
                new BsonDocumentCommand<BsonDocument>(cmd), cancellationToken: ct);
            Log.Information("MongoDB: updated app user '{User}' in auth database '{AuthDatabase}'.",
                user, authDb.DatabaseNamespace.DatabaseName);
        }
    }

    private static async Task ValidateAppUserAsync(string adminConnectionString, string user, string password,
        string authDb, string appDb, CancellationToken ct)
    {
        var builder = new MongoUrlBuilder(adminConnectionString)
        {
            Username = user,
            Password = password,
            AuthenticationSource = authDb,
            DatabaseName = appDb,
        };

        var appClient = new MongoClient(builder.ToString());
        await appClient.GetDatabase(appDb).RunCommandAsync<BsonDocument>(
            new JsonCommand<BsonDocument>("{ping:1}"), cancellationToken: ct);
        Log.Information("MongoDB: app user '{User}' authentication verified via '{AuthDatabase}'.", user, authDb);
    }

    private static async Task HandleLegacyAdminUserAsync(IConfiguration cfg, IMongoDatabase adminDb,
        string authDb, string appUser, CancellationToken ct)
    {
        if (string.Equals(authDb, "admin", StringComparison.OrdinalIgnoreCase)) return;

        var users = await adminDb.RunCommandAsync<BsonDocument>(
            new BsonDocumentCommand<BsonDocument>(new BsonDocument("usersInfo", appUser)), cancellationToken: ct);
        if (!users.TryGetValue("users", out var userList) || userList.AsBsonArray.Count == 0) return;

        Log.Warning("MongoDB: legacy app user '{User}' exists in admin; runtime auth database is '{AuthDatabase}'.",
            appUser, authDb);

        if (!cfg.GetValue("Setup:Mongo:RemoveLegacyAdminAppUser", false)) return;

        await adminDb.RunCommandAsync<BsonDocument>(
            new BsonDocumentCommand<BsonDocument>(new BsonDocument("dropUser", appUser)), cancellationToken: ct);
        Log.Warning("MongoDB: removed legacy app user '{User}' from admin after validating the '{AuthDatabase}' user.",
            appUser, authDb);
    }
}
