using Mediator;
using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.Study
{
    public sealed record GetStudySets(Guid? PlayerId, int Count)
        : IRequest<StudySetsResponseDto>;

    public sealed class GetStudySetsHandler
        : IRequestHandler<GetStudySets, StudySetsResponseDto>
    {
        private readonly IAppDb _db;

        public GetStudySetsHandler(IAppDb db) => _db = db;

        public async ValueTask<StudySetsResponseDto> Handle(GetStudySets request, CancellationToken ct)
        {
            var take = request.Count <= 0 ? 20 : Math.Clamp(request.Count, 1, 100);

            var rows = await StudySetHelpers.BuildCategoryCountsQuery(_db)
                .GroupBy(q => q.Category)
                .Select(g => new { Category = g.Key, QuestionCount = g.Count() })
                .OrderBy(x => x.Category)
                .Take(take)
                .ToListAsync(ct);

            var items = rows
                .Select(x => new StudySetListItemDto(
                    StudySetHelpers.CreateCategoryId(x.Category),
                    $"{x.Category} Study Set",
                    $"Review approved {x.Category} questions in a dedicated study set.",
                    StudySetKinds.Category,
                    x.Category,
                    x.QuestionCount))
                .ToList();

            if (request.PlayerId.HasValue)
            {
                var customSets = await _db.StudySets
                    .AsNoTracking()
                    .Include(x => x.Items)
                    .Where(x => x.PlayerId == request.PlayerId.Value)
                    .OrderByDescending(x => x.UpdatedAtUtc)
                    .Take(10)
                    .Select(x => new StudySetListItemDto(
                        StudySetHelpers.CreateCustomId(x.Id),
                        x.Title,
                        x.Description ?? "Review your saved custom study set.",
                        StudySetKinds.Custom,
                        "Custom",
                        x.Items.Count))
                    .ToListAsync(ct);
                items.InsertRange(0, customSets);

                var favoritesCount = await _db.QuestionStudyFavorites
                    .AsNoTracking()
                    .Where(x => x.PlayerId == request.PlayerId.Value)
                    .Join(
                        _db.Questions.AsNoTracking().Where(q => q.Status == "Approved"),
                        favorite => favorite.QuestionId,
                        question => question.Id,
                        (_, __) => 1)
                    .CountAsync(x => x == 1, ct);
                if (favoritesCount > 0)
                {
                    items.Insert(0, new StudySetListItemDto(
                        StudySetHelpers.CreateFavoritesId(),
                        "Favorites Study Set",
                        "Review your saved favorite questions in one dedicated study set.",
                        StudySetKinds.Favorites,
                        "Favorites",
                        favoritesCount));
                }

                var dueReviewCount = await _db.StudyCardStates
                    .AsNoTracking()
                    .CountAsync(x => x.PlayerId == request.PlayerId.Value && x.NextReviewAtUtc.HasValue, ct);
                if (dueReviewCount > 0)
                {
                    items.Insert(0, new StudySetListItemDto(
                        StudySetHelpers.CreateDueReviewId(),
                        "Due Review Study Set",
                        "Review cards that are due next based on spaced repetition state.",
                        StudySetKinds.DueReview,
                        "DueReview",
                        dueReviewCount));
                }

                var weakArea = await GetWeakAreaItemAsync(request.PlayerId.Value, ct);
                if (weakArea is not null)
                    items.Insert(0, weakArea);
            }

            return new StudySetsResponseDto(items);
        }

        private async Task<StudySetListItemDto?> GetWeakAreaItemAsync(Guid playerId, CancellationToken ct)
        {
            var weakArea = await _db.QuestionAnsweredPlayerDailyRollups
                .AsNoTracking()
                .Where(x => x.PlayerId == playerId)
                .GroupBy(x => x.Category)
                .Select(g => new
                {
                    Category = g.Key,
                    WrongAnswers = g.Sum(x => x.WrongAnswers),
                    TotalAnswers = g.Sum(x => x.TotalAnswers)
                })
                .Where(x => x.TotalAnswers > 0)
                .OrderByDescending(x => x.WrongAnswers)
                .ThenBy(x => x.TotalAnswers)
                .FirstOrDefaultAsync(ct);

            if (weakArea is null || weakArea.WrongAnswers <= 0)
                return null;

            var questionCount = await StudySetHelpers.BuildApprovedQuestionsQuery(_db, weakArea.Category).CountAsync(ct);
            if (questionCount == 0)
                return null;

            return new StudySetListItemDto(
                StudySetHelpers.CreateWeakAreaId(weakArea.Category),
                $"Weak Area: {weakArea.Category}",
                $"Practice more {weakArea.Category} questions based on recent answer history.",
                StudySetKinds.WeakArea,
                weakArea.Category,
                questionCount);
        }
    }
}
