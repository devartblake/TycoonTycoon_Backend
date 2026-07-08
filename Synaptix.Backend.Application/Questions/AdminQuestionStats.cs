using Mediator;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.Questions
{
    // Aggregate review counts for the operator content dashboard.
    public sealed record AdminQuestionStats() : IRequest<AdminQuestionStatsDto>;

    // Distinct question categories for filter dropdowns.
    public sealed record AdminListQuestionCategories() : IRequest<IReadOnlyList<string>>;

    // Bulk approve/reject in a single round trip (verdict already normalized to "approve"/"reject").
    public sealed record AdminBulkReviewQuestions(IReadOnlyList<Guid> Ids, string Verdict) : IRequest<BulkReviewResultDto>;

    public sealed class AdminQuestionStatsHandler(IAppDb db) : IRequestHandler<AdminQuestionStats, AdminQuestionStatsDto>
    {
        public async ValueTask<AdminQuestionStatsDto> Handle(AdminQuestionStats r, CancellationToken ct)
        {
            var counts = await db.Questions.AsNoTracking()
                .GroupBy(x => x.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            int CountFor(string status) => counts.FirstOrDefault(c => c.Status == status)?.Count ?? 0;

            // "Draft" is the review queue; Approved/Rejected are terminal verdicts. Archived is excluded.
            var totalPending = CountFor("Draft");
            var totalApproved = CountFor("Approved");
            var totalRejected = CountFor("Rejected");
            var reviewed = totalApproved + totalRejected;
            var approvalRate = reviewed == 0 ? 0d : (double)totalApproved / reviewed;

            // Average review latency = time from creation to the approve/reject decision.
            var reviewedRows = await db.Questions.AsNoTracking()
                .Where(x => (x.Status == "Approved" || x.Status == "Rejected") && x.StatusChangedAtUtc != null)
                .Select(x => new { x.CreatedAtUtc, x.StatusChangedAtUtc })
                .ToListAsync(ct);

            var avgReviewTime = reviewedRows.Count == 0
                ? 0d
                : reviewedRows.Average(x => (x.StatusChangedAtUtc!.Value - x.CreatedAtUtc).TotalMinutes);

            return new AdminQuestionStatsDto(totalPending, totalApproved, totalRejected, approvalRate, avgReviewTime);
        }
    }

    public sealed class AdminListQuestionCategoriesHandler(IAppDb db) : IRequestHandler<AdminListQuestionCategories, IReadOnlyList<string>>
    {
        public async ValueTask<IReadOnlyList<string>> Handle(AdminListQuestionCategories r, CancellationToken ct)
        {
            var categories = await db.Questions.AsNoTracking()
                .Select(x => x.Category)
                .Distinct()
                .ToListAsync(ct);

            return categories
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }

    public sealed class AdminBulkReviewQuestionsHandler(IAppDb db, ILogger<AdminBulkReviewQuestionsHandler> logger)
        : IRequestHandler<AdminBulkReviewQuestions, BulkReviewResultDto>
    {
        public async ValueTask<BulkReviewResultDto> Handle(AdminBulkReviewQuestions r, CancellationToken ct)
        {
            var status = r.Verdict == "approve" ? "Approved" : "Rejected";
            var ids = (r.Ids ?? Array.Empty<Guid>()).Distinct().ToArray();
            if (ids.Length == 0) return new BulkReviewResultDto(true, 0);

            var questions = await db.Questions.Where(x => ids.Contains(x.Id)).ToListAsync(ct);
            foreach (var q in questions)
                q.SetStatus(status);

            await db.SaveChangesAsync(ct);
            logger.LogInformation("Admin question bulk review: Verdict={Verdict}, Requested={Requested}, Reviewed={Reviewed}",
                r.Verdict, ids.Length, questions.Count);

            return new BulkReviewResultDto(true, questions.Count);
        }
    }
}
