using Microsoft.Extensions.Configuration;
using Npgsql;
using Serilog;

namespace Synaptix.Setup.Services;

public sealed class PostgresSetupTask : ISetupTask
{
    public string Name => "PostgreSQL";

    public async Task<SetupResult> RunAsync(IConfiguration cfg, CancellationToken ct = default)
    {
        var connStr = BuildConnectionString(cfg);
        if (connStr is null)
            return SetupResult.Fail("PostgreSQL connection string could not be constructed. Check POSTGRES_* env vars.");

        try
        {
            await using var conn = new NpgsqlConnection(connStr);
            await conn.OpenAsync(ct);

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT version()";
            var version = (string?)await cmd.ExecuteScalarAsync(ct);
            Log.Information("PostgreSQL connected: {Version}", version);

            // Report migration status
            var historyExists = await TableExistsAsync(conn, "__EFMigrationsHistory", ct);
            if (historyExists)
            {
                cmd.CommandText = "SELECT COUNT(*) FROM \"__EFMigrationsHistory\"";
                var count = (long)(await cmd.ExecuteScalarAsync(ct) ?? 0L);
                Log.Information("PostgreSQL: {Count} EF migration(s) applied.", count);
            }
            else
            {
                Log.Information("PostgreSQL: __EFMigrationsHistory table not found — migrations not yet applied.");
            }

            return SetupResult.Ok("PostgreSQL connection validated.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "PostgreSQL validation failed.");
            return SetupResult.Fail($"PostgreSQL connection failed: {ex.Message}");
        }
    }

    private static string? BuildConnectionString(IConfiguration cfg)
    {
        // Check hierarchical key first (Docker env with __ normalized to : by AddEnvironmentVariables)
        var explicit_ = cfg.GetConnectionString("db") ?? cfg["ConnectionStrings:db"];
        if (!string.IsNullOrWhiteSpace(explicit_)) return explicit_;

        var host     = cfg["POSTGRES_HOST"] ?? "localhost";
        var port     = cfg["POSTGRES_PORT"] ?? "5432";
        var db       = cfg["POSTGRES_DB"] ?? "synaptix_db";
        var user     = cfg["POSTGRES_USER"] ?? "synaptix_user";
        var password = cfg["POSTGRES_PASSWORD"];

        if (string.IsNullOrWhiteSpace(password)) return null;
        return $"Host={host};Port={port};Database={db ?? "synaptix_db"};Username={user};Password={password}";
    }

    private static async Task<bool> TableExistsAsync(NpgsqlConnection conn, string table, CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT EXISTS(SELECT 1 FROM information_schema.tables WHERE table_name = @t)";
        cmd.Parameters.AddWithValue("@t", table);
        return (bool)(await cmd.ExecuteScalarAsync(ct) ?? false);
    }
}
