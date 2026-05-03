using MediatR;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Application.Personalization;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Study
{
    public sealed record GetRecommendedStudySets(Guid? PlayerId, int Count)
        : IRequest<RecommendedStudySetsResponseDto>;

    public sealed class GetRecommendedStudySetsHandler
        : IRequestHandler<GetRecommendedStudySets, RecommendedStudySetsResponseDto>
    {
        private readonly IAppDb _db;
        private readonly IPlayerMindProfileService? _mindProfiles;

        public GetRecommendedStudySetsHandler(IAppDb db, IPlayerMindProfileService? mindProfiles = null)
        {
            _db = db;
            _mindProfiles = mindProfiles;
        }

        public async Task<RecommendedStudySetsResponseDto> Handle(GetRecommendedStudySets request, CancellationToken ct)
        {
            var take = request.Count <= 0 ? 5 : Math.Clamp(request.Count, 1, 20);
            var rows = await StudySetHelpers.BuildCategoryCountsQuery(_db)
                .GroupBy(q => q.Category)
                .Select(g => new { Category = g.Key, QuestionCount = g.Count() })
                .OrderByDescending(x => x.QuestionCount)
                .ThenBy(x => x.Category)
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

                var weakArea = await _db.QuestionAnsweredPlayerDailyRollups
                    .AsNoTracking()
                    .Where(x => x.PlayerId == request.PlayerId.Value)
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

                if (weakArea is not null && weakArea.WrongAnswers > 0)
                {
                    var questionCount = await StudySetHelpers.BuildApprovedQuestionsQuery(_db, weakArea.Category).CountAsync(ct);
                    if (questionCount > 0)
                    {
                        items.Insert(0, new StudySetListItemDto(
                            StudySetHelpers.CreateWeakAreaId(weakArea.Category),
                            $"Weak Area: {weakArea.Category}",
                            $"Practice more {weakArea.Category} questions based on recent answer history.",
                            StudySetKinds.WeakArea,
                            weakArea.Category,
                            questionCount));
                    }
                }

                // Add a personalization-profile-backed weak area set when the player's profile
                // identifies a weak category that is not already surfaced by the rollup-based one.
                if (_mindProfiles is not null)
                {
                    try
                    {
                        var profile = await _mindProfiles.GetOrCreateAsync(request.PlayerId.Value, ct);
                        var topWeakCategory = profile.CategoryWeaknesses
                            .Where(kv => kv.Value > 0)
                            .OrderByDescending(kv => kv.Value)
                            .Select(kv => kv.Key)
                            .FirstOrDefault();

                        if (topWeakCategory is not null &&
                            !string.Equals(topWeakCategory, weakArea?.Category, StringComparison.OrdinalIgnoreCase))
                        {
                            var profileQuestionCount = await StudySetHelpers
                                .BuildApprovedQuestionsQuery(_db, topWeakCategory).CountAsync(ct);
                            if (profileQuestionCount > 0)
                            {
                                // Insert after any rollup-based weak area so game-data signal leads.
                                var insertAt = items.FindIndex(x => x.Kind == StudySetKinds.WeakArea);
                                insertAt = insertAt >= 0 ? insertAt + 1 : 0;
                                items.Insert(insertAt, new StudySetListItemDto(
                                    StudySetHelpers.CreateWeakAreaId(topWeakCategory),
                                    $"Recommended Focus: {topWeakCategory}",
                                    $"Your personalization profile suggests practising {topWeakCategory} to strengthen your skills.",
                                    StudySetKinds.WeakArea,
                                    topWeakCategory,
                                    profileQuestionCount));
                            }
                        }
                    }
                    catch
                    {
                        // Personalization must never break study set recommendations.
                    }
                }
            }

            return new RecommendedStudySetsResponseDto(items.Take(take).ToList());
        }
    }
}
