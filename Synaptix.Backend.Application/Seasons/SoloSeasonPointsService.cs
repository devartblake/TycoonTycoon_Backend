using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.Seasons;

/// <summary>
/// Awards season rank points for solo quiz play (server-graded results only).
/// Sits on top of <see cref="SeasonPointsService"/>'s idempotent ledger and
/// adds the solo-specific rules: points-per-correct formula and a per-UTC-day
/// cap so grinding solo quizzes can't out-earn ranked matches.
/// </summary>
public sealed class SoloSeasonPointsService(
    IAppDb db,
    SeasonPointsService points,
    IOptions<SeasonSoloPointsOptions> options)
{
    public const string Kind = "solo-quiz";

    /// <summary>
    /// Award capped solo points for a graded quiz. Returns the points actually
    /// applied (0 when disabled, no active season, cap reached, or the event
    /// was already applied). <paramref name="eventId"/> is the ledger
    /// idempotency key — derive it deterministically from the quiz session so
    /// retries never double-credit (see <see cref="DeriveEventId"/>).
    /// </summary>
    public async Task<int> AwardAsync(
        Guid eventId,
        Guid playerId,
        int correctAnswers,
        string? note,
        CancellationToken ct)
    {
        var opts = options.Value;
        if (!opts.Enabled || correctAnswers <= 0 || playerId == Guid.Empty)
            return 0;

        var season = await db.Seasons.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Status == SeasonStatus.Active, ct);
        if (season is null)
            return 0;

        var raw = correctAnswers * opts.PointsPerCorrect;

        // Remaining daily headroom from the ledger itself — no extra schema.
        // Two in-flight quizzes can both pass this check and slightly overshoot
        // the cap; that's an accepted race (bounded by one quiz's points).
        var utcMidnight = new DateTimeOffset(DateTime.UtcNow.Date, TimeSpan.Zero);
        var awardedToday = await db.SeasonPointTransactions.AsNoTracking()
            .Where(x => x.PlayerId == playerId
                && x.SeasonId == season.Id
                && x.Kind == Kind
                && x.Delta > 0
                && x.CreatedAtUtc >= utcMidnight)
            .SumAsync(x => (int?)x.Delta, ct) ?? 0;

        var capped = Math.Min(raw, Math.Max(0, opts.DailyCap - awardedToday));
        if (capped <= 0)
            return 0;

        var result = await points.ApplyAsync(new ApplySeasonPointsRequest(
            eventId,
            season.Id,
            playerId,
            Kind,
            capped,
            note), ct);

        return result.Status == "Applied" ? capped : 0;
    }

    /// <summary>
    /// Deterministic ledger event id bound to the player and quiz session, so
    /// a replayed or retried submission maps to the same transaction and a
    /// session id captured from another player can't block their award.
    /// </summary>
    public static Guid DeriveEventId(Guid playerId, string sessionKey)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes($"solo-quiz:{playerId:N}:{sessionKey}"));
        return new Guid(bytes);
    }
}
