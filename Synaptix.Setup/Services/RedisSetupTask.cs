using Microsoft.Extensions.Configuration;
using Serilog;
using StackExchange.Redis;

namespace Synaptix.Setup.Services;

public sealed class RedisSetupTask : ISetupTask
{
    public string Name => "Redis";

    // Logical database assignments as defined in the bootstrap manifest.
    private static readonly (int Db, string Name)[] LogicalDatabases =
    [
        (0, "cache"),
        (1, "rateLimit"),
        (2, "sessions"),
        (3, "locks"),
        (4, "jobs"),
    ];

    public async Task<SetupResult> RunAsync(IConfiguration cfg, CancellationToken ct = default)
    {
        var options = BuildOptions(cfg);
        if (options is null)
            return SetupResult.Fail("Redis connection could not be configured. Check REDIS_* env vars.");

        try
        {
            await using var redis = await ConnectionMultiplexer.ConnectAsync(options);
            var warnings = new List<string>();

            foreach (var (dbIndex, dbName) in LogicalDatabases)
            {
                var db = redis.GetDatabase(dbIndex);
                var testKey = $"synaptix:setup:ping:{dbName}";
                await db.StringSetAsync(testKey, "1", TimeSpan.FromSeconds(10));
                var result = await db.StringGetAsync(testKey);
                await db.KeyDeleteAsync(testKey);

                if (result != "1")
                    warnings.Add($"Redis logical DB {dbIndex} ({dbName}) write/read test failed.");
                else
                    Log.Debug("Redis logical DB {Index} ({Name}) OK.", dbIndex, dbName);
            }

            Log.Information("Redis validated: {Count} logical database(s) accessible.", LogicalDatabases.Length);
            return SetupResult.Ok("Redis connection and logical databases validated.", warnings);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Redis setup failed.");
            return SetupResult.Fail($"Redis setup failed: {ex.Message}");
        }
    }

    private static ConfigurationOptions? BuildOptions(IConfiguration cfg)
    {
        // Try flat env var first, then extract from structured connection string
        var password = cfg["REDIS_PASSWORD"];
        if (string.IsNullOrWhiteSpace(password))
        {
            var connStr = cfg.GetConnectionString("redis") ?? cfg["ConnectionStrings:redis"];
            password = connStr?.Split(',')
                .FirstOrDefault(p => p.TrimStart().StartsWith("password=", StringComparison.OrdinalIgnoreCase))
                ?.Split('=', 2).LastOrDefault();
        }
        if (string.IsNullOrWhiteSpace(password)) return null;

        var host = cfg["REDIS_HOST"] ?? "localhost";
        var port = int.TryParse(cfg["REDIS_PORT"], out var p) ? p : 6379;

        return new ConfigurationOptions
        {
            EndPoints          = { { host, port } },
            Password           = password,
            AbortOnConnectFail = false,
            ConnectTimeout     = 5000,
            SyncTimeout        = 5000,
        };
    }
}
