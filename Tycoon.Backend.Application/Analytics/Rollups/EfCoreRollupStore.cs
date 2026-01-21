using Tycoon.Backend.Application.Analytics.Abstractions;
using Tycoon.Backend.Application.Analytics.Models;
using Tycoon.Backend.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Tycoon.Backend.Application.Analytics.Rollups
{
    public sealed class EfCoreRollupStore : IRollupStore
    {
        private readonly IAppDb _db;

        public EfCoreRollupStore(IAppDb db)
        {
            _db = db;
        }

        public async Task<DailyRollup> UpsertDailyRollupAsync(
            DateOnly utcDate,
            string mode,
            string category,
            int difficulty,
            bool isCorrect,
            int answerTimeMs,
            DateTime answeredAtUtc,
            CancellationToken ct)
        {
            var rollup = await _db.DailyRollups
                .SingleOrDefaultAsync(x =>
                    x.UtcDate == utcDate &&
                    x.Mode == mode &&
                    x.Category == category &&
                    x.Difficulty == difficulty,
                    ct);

            if (rollup is null)
            {
                rollup = DailyRollup.Create(
                    utcDate, mode, category, difficulty,
                    isCorrect, answerTimeMs, answeredAtUtc);

                _db.DailyRollups.Add(rollup);
            }
            else
            {
                rollup.Apply(isCorrect, answerTimeMs, answeredAtUtc);
            }

            return rollup;
        }

        public async Task<PlayerDailyRollup> UpsertPlayerDailyRollupAsync(
            DateOnly utcDate,
            Guid playerId,
            string mode,
            string category,
            int difficulty,
            bool isCorrect,
            int answerTimeMs,
            DateTime answeredAtUtc,
            CancellationToken ct)
        {
            var rollup = await _db.PlayerDailyRollups
                .SingleOrDefaultAsync(x =>
                    x.UtcDate == utcDate &&
                    x.PlayerId == playerId &&
                    x.Mode == mode &&
                    x.Category == category &&
                    x.Difficulty == difficulty,
                    ct);

            if (rollup is null)
            {
                rollup = PlayerDailyRollup.Create(
                    utcDate, playerId, mode, category,
                    difficulty, isCorrect, answerTimeMs, answeredAtUtc);

                _db.PlayerDailyRollups.Add(rollup);
            }
            else
            {
                rollup.Apply(isCorrect, answerTimeMs, answeredAtUtc);
            }

            return rollup;
        }
    }
}
