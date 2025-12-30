using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Infrastructure.Persistence;

namespace Tycoon.MigrationService.Seeding
{
    /// <summary>
    /// Performs idempotent Daily/Weekly mission claim resets based on UTC windows.
    /// </summary>
    public sealed class MissionResetService
    {
        public async Task ResetAsync(AppDb db, CancellationToken ct)
        {
            var now = DateTime.UtcNow;

            var dailyStart = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, DateTimeKind.Utc);

            // Weekly reset window: Monday 00:00 UTC
            var daysSinceMonday = ((int)dailyStart.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
            var weeklyStart = dailyStart.AddDays(-daysSinceMonday);

            // Reset Daily claims that haven't been reset since dailyStart
            await ResetByTypeAsync(db, type: "Daily", windowStartUtc: dailyStart, nowUtc: now, ct);

            // Reset Weekly claims that haven't been reset since weeklyStart
            await ResetByTypeAsync(db, type: "Weekly", windowStartUtc: weeklyStart, nowUtc: now, ct);
        }

        private static async Task ResetByTypeAsync(
            AppDb db,
            string type,
            DateTime windowStartUtc,
            DateTime nowUtc,
            CancellationToken ct)
        {
            // Join MissionClaims -> Missions to determine type
            var claimsToReset = await (
                from c in db.MissionClaims
                join m in db.Missions on c.MissionId equals m.Id
                where m.Active
                      && m.Type == type
                      && (c.LastResetAtUtc == null || c.LastResetAtUtc < windowStartUtc)
                select c
            ).ToListAsync(ct);

            if (claimsToReset.Count == 0)
                return;

            foreach (var claim in claimsToReset)
            {
                claim.Reset(nowUtc);
            }

            await db.SaveChangesAsync(ct);
        }
    }
}
