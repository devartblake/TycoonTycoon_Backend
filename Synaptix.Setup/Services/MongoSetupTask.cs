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

            await EnsureCollectionsAsync(client, appDb, ["events", "rollups", "personalization"], ct);
            await EnsureCollectionsAsync(client, cryptoDb, ["settlements", "ledger_events"], ct);

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
        catch (MongoCommandException ex) when (ex.CodeName == "Location51003" || ex.Message.Contains("already exists"))
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
