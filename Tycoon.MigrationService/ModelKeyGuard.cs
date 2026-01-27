using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;

namespace Tycoon.MigrationService;

internal static class ModelKeyGuard
{
    public static void LogKeylessEntities(DbContext db, ILogger logger)
    {
        var offenders = db.Model
            .GetEntityTypes()
            .Where(et => !et.IsOwned() && !et.GetIsKeyless() && et.FindPrimaryKey() is null)
            .Select(et => et.DisplayName())
            .OrderBy(n => n)
            .ToList();

        if (offenders.Count == 0)
        {
            logger.LogInformation("EF model key guard: all entity types have primary keys (or are owned/keyless).");
            return;
        }

        logger.LogWarning(
            "EF model key guard: found {Count} entity types with NO primary key: {Entities}",
            offenders.Count,
            string.Join(", ", offenders)
        );

        // Helpful hint: these often are join entities that forgot HasKey(...)
        var likelyJoinEntities = offenders
            .Where(n => n.Contains("Member", StringComparison.OrdinalIgnoreCase)
                     || n.Contains("Join", StringComparison.OrdinalIgnoreCase)
                     || n.Contains("Link", StringComparison.OrdinalIgnoreCase)
                     || n.Contains("Map", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (likelyJoinEntities.Count > 0)
        {
            logger.LogWarning(
                "EF model key guard: likely join-entity offenders: {Entities}",
                string.Join(", ", likelyJoinEntities)
            );
        }
    }
}
