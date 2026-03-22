using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog;
using Tycoon.Backend.Infrastructure.Persistence.Options;

namespace Tycoon.Backend.Infrastructure.Persistence.Startup;

/// <summary>
/// Scoped validator used by a hosted wrapper to gate application startup on schema readiness.
/// </summary>
public sealed class SchemaStartupGate
{
    private readonly IServiceProvider _sp;
    private readonly ILogger _log;
    private readonly SchemaGateOptions _opt;

    public SchemaStartupGate(IServiceProvider sp, IOptions<SchemaGateOptions> opt)
    {
        _sp = sp;
        _opt = opt.Value;
        _log = Log.ForContext<SchemaStartupGate>();
    }

    public async Task ValidateAsync(CancellationToken cancellationToken)
    {
        if (!_opt.Enabled || !_opt.StartupGateEnabled)
        {
            _log.Information("SchemaStartupGate disabled (SchemaGate:Enabled={Enabled}, StartupGateEnabled={StartupGateEnabled}).",
                _opt.Enabled, _opt.StartupGateEnabled);
            return;
        }

        var timeout = TimeSpan.FromSeconds(Math.Max(1, _opt.TimeoutSeconds));
        var pollInterval = TimeSpan.FromSeconds(Math.Min(5, _opt.TimeoutSeconds / 2.0));
        var deadline = DateTimeOffset.UtcNow + timeout;

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);

        _log.Information("SchemaStartupGate waiting up to {Timeout}s for schema readiness (polling every {Poll}s)...",
            _opt.TimeoutSeconds, pollInterval.TotalSeconds);

        string? lastFailure = null;

        while (DateTimeOffset.UtcNow < deadline)
        {
            timeoutCts.Token.ThrowIfCancellationRequested();

            lastFailure = await CheckOnceAsync(timeoutCts.Token);
            if (lastFailure is null)
            {
                _log.Information("SchemaStartupGate passed.");
                return;
            }

            _log.Warning("SchemaStartupGate: {Failure} — retrying in {Seconds}s...", lastFailure, pollInterval.TotalSeconds);

            try
            {
                await Task.Delay(pollInterval, timeoutCts.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                // Timeout expired during delay — fall through to Fail.
                break;
            }
        }

        Fail(lastFailure ?? "Schema validation timed out.");
    }

    /// <summary>
    /// Runs all schema checks once.  Returns null on success, or the failure reason.
    /// </summary>
    private async Task<string?> CheckOnceAsync(CancellationToken ct)
    {
        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();

        try
        {
            if (!await db.Database.CanConnectAsync(ct))
                return "DB not reachable.";
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return $"DB connection error: {ex.Message}";
        }

        if (_opt.RequireMigrationsHistoryTable)
        {
            var historyExists = await ExistsOrAutoMigrateAsync(db, _opt.Schema, _opt.MigrationsHistoryTable, timeoutCts.Token);
            if (!historyExists)
                return $"Schema missing: '{_opt.Schema}.{_opt.MigrationsHistoryTable}' not found. Run MigrationService first.";
        }

        foreach (var table in _opt.RequiredTables ?? Array.Empty<string>())
        {
            if (string.IsNullOrWhiteSpace(table)) continue;

            var exists = await ExistsOrAutoMigrateAsync(db, _opt.Schema, table, timeoutCts.Token);
            if (!exists)
                return $"Schema missing: critical table '{_opt.Schema}.{table}' not found. Run MigrationService first.";
        }

        return null;
    }

    private static async Task<bool> ExistsAsync(AppDb db, string schema, string table, CancellationToken ct)
    {
        // Postgres: to_regclass('schema.table') returns NULL if missing.
        var qualified = $"{schema}.{table}";

        var connection = db.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose)
            await connection.OpenAsync(ct);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT to_regclass(@qualified) IS NOT NULL";

            var parameter = command.CreateParameter();
            parameter.ParameterName = "qualified";
            parameter.Value = qualified;
            command.Parameters.Add(parameter);

            var scalar = await command.ExecuteScalarAsync(ct);
            return scalar is bool exists && exists;
        }
        finally
        {
            if (shouldClose)
                await connection.CloseAsync();
        }
    }

    private async Task<bool> ExistsOrAutoMigrateAsync(AppDb db, string schema, string table, CancellationToken ct)
    {
        if (await ExistsAsync(db, schema, table, ct))
            return true;

        if (!_opt.AutoMigrateIfMissing)
            return false;

        _log.Warning("SchemaStartupGate: '{Schema}.{Table}' missing. AutoMigrateIfMissing enabled; attempting Database.MigrateAsync().", schema, table);
        try
        {
            await db.Database.MigrateAsync(ct);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "SchemaStartupGate auto-migration failed.");
            return false;
        }

        return await ExistsAsync(db, schema, table, ct);
    }

    private void Fail(string message)
    {
        if (_opt.LogOnly || !_opt.FailStartupIfInvalid)
        {
            _log.Error("SchemaStartupGate failed: {Message}", message);
            return;
        }

        throw new InvalidOperationException(message);
    }
}
