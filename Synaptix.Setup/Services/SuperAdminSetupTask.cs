using Microsoft.Extensions.Configuration;
using Npgsql;
using Serilog;

namespace Synaptix.Setup.Services;

public sealed class SuperAdminSetupTask : ISetupTask
{
    public string Name => "SuperAdmin";

    public async Task<SetupResult> RunAsync(IConfiguration cfg, CancellationToken ct = default)
    {
        var email    = cfg["SuperAdmin:Email"]    ?? cfg["SUPER_ADMIN_EMAIL"];
        var password = cfg["SuperAdmin:Password"] ?? cfg["SUPER_ADMIN_PASSWORD"];
        var handle   = cfg["SuperAdmin:Handle"]   ?? cfg["SUPER_ADMIN_HANDLE"] ?? "superadmin";

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return SetupResult.Skip("SUPER_ADMIN_EMAIL or SUPER_ADMIN_PASSWORD not set — skipping super admin creation.");

        var connStr = cfg.GetConnectionString("db") ?? cfg["ConnectionStrings:db"];
        if (string.IsNullOrWhiteSpace(connStr))
            return SetupResult.Fail("PostgreSQL connection string not configured.");

        try
        {
            await using var conn = new NpgsqlConnection(connStr);
            await conn.OpenAsync(ct);

            var normalizedEmail = email.ToLowerInvariant();
            var passwordHash    = BCrypt.Net.BCrypt.HashPassword(password);

            // Check if user already exists
            await using var checkCmd = conn.CreateCommand();
            checkCmd.CommandText = "SELECT COUNT(*) FROM users WHERE email = @email";
            checkCmd.Parameters.AddWithValue("@email", normalizedEmail);
            var existingCount = (long)(await checkCmd.ExecuteScalarAsync(ct) ?? 0L);

            if (existingCount > 0)
            {
                Log.Information("SuperAdmin: account '{Email}' already exists.", normalizedEmail);
            }
            else
            {
                var userId = Guid.NewGuid();
                await using var insertCmd = conn.CreateCommand();
                insertCmd.CommandText = """
                    INSERT INTO users (id, email, handle, password_hash, created_at_utc, updated_at_utc)
                    VALUES (@id, @email, @handle, @hash, NOW(), NOW())
                    ON CONFLICT (email) DO NOTHING
                    """;
                insertCmd.Parameters.AddWithValue("@id",     userId);
                insertCmd.Parameters.AddWithValue("@email",  normalizedEmail);
                insertCmd.Parameters.AddWithValue("@handle", handle);
                insertCmd.Parameters.AddWithValue("@hash",   passwordHash);
                await insertCmd.ExecuteNonQueryAsync(ct);
                Log.Information("SuperAdmin: created account '{Email}' (handle: '{Handle}').", normalizedEmail, handle);
            }

            // Ensure admin ACL entry
            await UpsertAdminAclAsync(conn, normalizedEmail, ct);

            return SetupResult.Ok($"Super admin '{normalizedEmail}' ensured.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Super admin setup failed.");
            return SetupResult.Fail($"Super admin setup failed: {ex.Message}");
        }
    }

    public async Task<SetupResult> RotatePasswordAsync(IConfiguration cfg, string newPassword, CancellationToken ct = default)
    {
        var email   = cfg["SuperAdmin:Email"] ?? cfg["SUPER_ADMIN_EMAIL"];
        var connStr = cfg.GetConnectionString("db") ?? cfg["ConnectionStrings:db"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(connStr))
            return SetupResult.Fail("SuperAdmin email or DB connection not configured.");

        try
        {
            await using var conn = new NpgsqlConnection(connStr);
            await conn.OpenAsync(ct);

            var hash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            await using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE users SET password_hash = @hash, updated_at_utc = NOW() WHERE email = @email";
            cmd.Parameters.AddWithValue("@hash",  hash);
            cmd.Parameters.AddWithValue("@email", email.ToLowerInvariant());
            var rows = await cmd.ExecuteNonQueryAsync(ct);

            return rows > 0
                ? SetupResult.Ok($"Super admin password updated for '{email}'.")
                : SetupResult.Fail($"Super admin '{email}' not found in database.");
        }
        catch (Exception ex)
        {
            return SetupResult.Fail($"Password rotation failed: {ex.Message}");
        }
    }

    private static async Task UpsertAdminAclAsync(NpgsqlConnection conn, string email, CancellationToken ct)
    {
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO admin_email_acls (id, normalized_email, list_type, role, added_by, notes, created_at_utc, updated_at_utc)
            VALUES (gen_random_uuid(), @email, 'Allow', 'SuperAdmin', 'setup-cli', 'Created by Synaptix.Setup', NOW(), NOW())
            ON CONFLICT (normalized_email) DO UPDATE SET list_type = 'Allow', role = 'SuperAdmin', updated_at_utc = NOW()
            """;
        cmd.Parameters.AddWithValue("@email", email);
        await cmd.ExecuteNonQueryAsync(ct);
        Log.Information("SuperAdmin: ACL entry ensured for '{Email}'.", email);
    }
}
