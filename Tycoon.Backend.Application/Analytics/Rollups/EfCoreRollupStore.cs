using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Application.Analytics.Abstractions;
using Tycoon.Backend.Application.Analytics.Models;

namespace Tycoon.Backend.Application.Analytics.Rollups;

/// <summary>
/// EF Core implementation of IRollupStore.
/// Performs upsert behavior for daily rollups using (UtcDate, Mode, Category, Difficulty) keys,
/// and for player daily rollups using (UtcDate, PlayerId, Mode, Category, Difficulty) keys.
/// </summary>
public sealed class EfCoreRollupStore : IRollupStore
{
    private readonly IAppDb _db;

    public EfCoreRollupStore(IAppDb db)
    {
        _db = db;
    }

    public async Task<QuestionAnsweredDailyRollup> UpsertDailyRollupAsync(
        DateOnly day,
        string mode,
        string category,
        int difficulty,
        bool isCorrect,
        int answerTimeMs,
        DateTime answeredAtUtc,
        CancellationToken ct)
    {
        // Normalize strings to ensure consistent keys
        mode = (mode ?? string.Empty).Trim();
        category = (category ?? string.Empty).Trim();

        var existing = await _db.QuestionAnsweredDailyRollups
            .FirstOrDefaultAsync(r =>
                r.Day == day &&
                r.Mode == mode &&
                r.Category == category &&
                r.Difficulty == difficulty,
                ct);

        if (existing is null)
        {
            existing = new QuestionAnsweredDailyRollup
            {
                Day = day,
                Mode = mode,
                Category = category,
                Difficulty = difficulty,

                TotalAnswers = 0,
                CorrectAnswers = 0,
                WrongAnswers = 0,
                SumAnswerTimeMs = 0,
                MinAnswerTimeMs = answerTimeMs, // Initialize with current
                MaxAnswerTimeMs = answerTimeMs, // Initialize with current

                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = answeredAtUtc
            };

            _db.QuestionAnsweredDailyRollups.Add(existing);
        }
        else
        {
            // Update Min/Max for existing records
            existing.MinAnswerTimeMs = existing.MinAnswerTimeMs == 0
                ? answerTimeMs
                : Math.Min(existing.MinAnswerTimeMs, answerTimeMs);

            existing.MaxAnswerTimeMs = Math.Max(existing.MaxAnswerTimeMs, answerTimeMs);
        }

        // Increment counters
        existing.TotalAnswers += 1;
        if (isCorrect)
            existing.CorrectAnswers += 1;
        else
            existing.WrongAnswers += 1;

        if (answerTimeMs > 0)
            existing.SumAnswerTimeMs += answerTimeMs;

        existing.UpdatedAtUtc = answeredAtUtc;

        await _db.SaveChangesAsync(ct);
        return existing;
    }

    public async Task<QuestionAnsweredPlayerDailyRollup> UpsertPlayerDailyRollupAsync(
        DateOnly day,
        Guid playerId,
        string mode,
        string category,
        int difficulty,
        bool isCorrect,
        int answerTimeMs,
        DateTime answeredAtUtc,
        CancellationToken ct)
    {
        mode = (mode ?? string.Empty).Trim();
        category = (category ?? string.Empty).Trim();

        var existing = await _db.QuestionAnsweredPlayerDailyRollups
            .FirstOrDefaultAsync(r =>
                r.Day == day &&
                r.PlayerId == playerId &&
                r.Mode == mode &&
                r.Category == category &&
                r.Difficulty == difficulty,
                ct);

        if (existing is null)
        {
            existing = new QuestionAnsweredPlayerDailyRollup
            {
                Day = day,
                PlayerId = playerId,
                Mode = mode,
                Category = category,
                Difficulty = difficulty,

                TotalAnswers = 0,
                CorrectAnswers = 0,
                WrongAnswers = 0,
                SumAnswerTimeMs = 0,
                MinAnswerTimeMs = answerTimeMs,
                MaxAnswerTimeMs = answerTimeMs,

                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = answeredAtUtc
            };

            _db.QuestionAnsweredPlayerDailyRollups.Add(existing);
        }
        else
        {
            existing.MinAnswerTimeMs = existing.MinAnswerTimeMs == 0
                ? answerTimeMs
                : Math.Min(existing.MinAnswerTimeMs, answerTimeMs);

            existing.MaxAnswerTimeMs = Math.Max(existing.MaxAnswerTimeMs, answerTimeMs);
        }

        existing.TotalAnswers += 1;

        if (isCorrect) existing.CorrectAnswers += 1;
        else existing.WrongAnswers += 1;

        if (answerTimeMs > 0) existing.SumAnswerTimeMs += answerTimeMs;
        
        existing.UpdatedAtUtc = answeredAtUtc;

        await _db.SaveChangesAsync(ct);
        return existing;
    }
}