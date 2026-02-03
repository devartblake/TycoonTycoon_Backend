using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Tycoon.Backend.Infrastructure.Persistence.Options;

namespace Tycoon.Backend.Infrastructure.Persistence.HealthChecks;

public sealed class SchemaHealthCheck : IHealthCheck
{
    private readonly AppDb _db;
    private readonly SchemaGateOptions _opt;

    public SchemaHealthCheck(AppDb db, IOptions<SchemaGateOptions> opt)
    {
        _db = db;
        _opt = opt.Value;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (!_opt.Enabled || !_opt.HealthCheckEnabled)
            return HealthCheckResult.Healthy("Schema health check disabled.");

        try
        {
            if (!await _db.Database.CanConnectAsync(cancellationToken))
                return HealthCheckResult.Unhealthy("Cannot connect to database.");

            if (_opt.RequireMigrationsHistoryTable)
            {
                var historyExists = await ExistsAsync(_opt.MigrationsHistoryTable, cancellationToken);
                if (!historyExists)
                    return HealthCheckResult.Unhealthy($"Missing {_opt.Schema}.{_opt.MigrationsHistoryTable}. Migrations likely not applied.");
            }

            foreach (var table in _opt.RequiredTables ?? Array.Empty<string>())
            {
                if (string.IsNullOrWhiteSpace(table)) continue;

                if (!await ExistsAsync(table, cancellationToken))
                    return HealthCheckResult.Unhealthy($"Missing critical table '{_opt.Schema}.{table}'.");
            }

            return HealthCheckResult.Healthy("Schema present.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Schema validation failed.", ex);
        }
    }

    private async Task<bool> ExistsAsync(string tableName, CancellationToken ct)
    {
        var qualified = $"{_opt.Schema}.{tableName}";
        return await _db.Database
            .SqlQueryRaw<bool>("SELECT to_regclass(@p0) IS NOT NULL", qualified)
            .SingleAsync(ct);
    }
}
