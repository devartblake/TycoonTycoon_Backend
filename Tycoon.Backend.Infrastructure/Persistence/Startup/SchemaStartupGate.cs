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

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, _opt.TimeoutSeconds)));

        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();

        if (!await db.Database.CanConnectAsync(timeoutCts.Token))
        {
            Fail("DB not reachable.");
            return;
        }

        if (_opt.RequireMigrationsHistoryTable)
        {
            var historyExists = await ExistsAsync(db, _opt.Schema, _opt.MigrationsHistoryTable, timeoutCts.Token);
            if (!historyExists)
            {
                Fail($"Schema missing: '{_opt.Schema}.{_opt.MigrationsHistoryTable}' not found. Run MigrationService first.");
                return;
            }
        }

        foreach (var table in _opt.RequiredTables ?? Array.Empty<string>())
        {
            if (string.IsNullOrWhiteSpace(table)) continue;

            var exists = await ExistsAsync(db, _opt.Schema, table, timeoutCts.Token);
            if (!exists)
            {
                Fail($"Schema missing: critical table '{_opt.Schema}.{table}' not found. Run MigrationService first.");
                return;
            }
        }

        _log.Information("SchemaStartupGate passed.");
    }

    private static async Task<bool> ExistsAsync(AppDb db, string schema, string table, CancellationToken ct)
    {
        // Postgres: to_regclass('schema.table') returns NULL if missing.
        var qualified = $"{schema}.{table}";
        return await db.Database
            .SqlQueryRaw<bool>("SELECT to_regclass(@p0) IS NOT NULL", qualified)
            .SingleAsync(ct);
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
