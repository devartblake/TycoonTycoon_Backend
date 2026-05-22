using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Serilog;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Backend.Infrastructure.Persistence;
using Tycoon.MigrationService.Options;

namespace Tycoon.MigrationService.Seeding;

public sealed class DashboardReadinessValidator
{
    private readonly MigrationServiceOptions _options;
    private readonly IConfiguration _cfg;
    private readonly Serilog.ILogger _log;

    public DashboardReadinessValidator(IOptions<MigrationServiceOptions> options, IConfiguration cfg)
    {
        _options = options.Value;
        _cfg = cfg;
        _log = Log.ForContext<DashboardReadinessValidator>();
    }

    public async Task ValidateAsync(AppDb db, CancellationToken ct)
    {
        if (!_options.DashboardReadiness.Enabled)
        {
            _log.Information("Dashboard readiness validation disabled.");
            return;
        }

        var failures = new List<string>();

        if (db.Database.IsRelational())
        {
            var appliedMigrations = await db.Database.GetAppliedMigrationsAsync(ct);
            if (!appliedMigrations.Any())
                failures.Add("No EF migrations are recorded as applied.");
        }

        await RequireAnyAsync(db.Tiers, "tiers", failures, ct);
        await RequireAnyAsync(db.Missions, "missions", failures, ct);
        await RequireAnyAsync(db.Questions, "questions", failures, ct);
        await RequireAnyAsync(db.StoreItems, "store items", failures, ct);
        await RequireAnyAsync(db.SkillNodes, "skill nodes", failures, ct);
        await RequireAnyAsync(db.SeasonRewardRules, "season reward rules", failures, ct);

        var configuredEmail = _cfg["SuperAdmin:Email"];
        if (!string.IsNullOrWhiteSpace(configuredEmail))
        {
            var normalizedEmail = configuredEmail.Trim().ToLowerInvariant();
            var userExists = await db.Users.AsNoTracking()
                .AnyAsync(u => u.Email == normalizedEmail, ct);
            if (!userExists)
                failures.Add($"Configured super admin user '{normalizedEmail}' does not exist.");

            var aclExists = await db.AdminEmailAcls.AsNoTracking()
                .AnyAsync(e =>
                    e.NormalizedEmail == normalizedEmail &&
                    e.ListType == AdminAclListType.Allow &&
                    e.Role == AdminRole.SuperAdmin,
                    ct);
            if (!aclExists)
                failures.Add($"Configured super admin '{normalizedEmail}' is missing an Allow/SuperAdmin ACL entry.");
        }
        else
        {
            _log.Information("SuperAdmin:Email not configured; skipping super admin readiness checks.");
        }

        if (failures.Count == 0)
        {
            _log.Information("Dashboard readiness validation passed.");
            return;
        }

        var message = "Dashboard readiness validation failed: " + string.Join("; ", failures);
        if (_options.DashboardReadiness.Strict)
            throw new InvalidOperationException(message);

        _log.Warning("{Message}", message);
    }

    private static async Task RequireAnyAsync<T>(
        IQueryable<T> query,
        string label,
        ICollection<string> failures,
        CancellationToken ct)
        where T : class
    {
        if (!await query.AsNoTracking().AnyAsync(ct))
            failures.Add($"Missing required {label} seed data.");
    }
}
