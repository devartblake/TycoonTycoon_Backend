using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Study
{
    internal static class StudySetHelpers
    {
        private const string CategoryPrefix = "category:";
        private const string WeakAreaPrefix = "weak-area:";
        private const string FavoritesId = "favorites";
        private const string CustomPrefix = "custom:";
        private const string DueReviewId = "due-review";

        public static string CreateCategoryId(string category) => $"{CategoryPrefix}{Uri.EscapeDataString(category)}";

        public static string CreateWeakAreaId(string category) => $"{WeakAreaPrefix}{Uri.EscapeDataString(category)}";

        public static string CreateFavoritesId() => FavoritesId;
        public static string CreateCustomId(Guid id) => $"{CustomPrefix}{id:D}";
        public static string CreateDueReviewId() => DueReviewId;

        public static bool TryParseId(string id, out string kind, out string category)
        {
            kind = string.Empty;
            category = string.Empty;

            if (string.IsNullOrWhiteSpace(id))
                return false;

            if (string.Equals(id.Trim(), FavoritesId, StringComparison.OrdinalIgnoreCase))
            {
                kind = StudySetKinds.Favorites;
                category = "Favorites";
                return true;
            }

            if (string.Equals(id.Trim(), DueReviewId, StringComparison.OrdinalIgnoreCase))
            {
                kind = StudySetKinds.DueReview;
                category = "DueReview";
                return true;
            }

            if (id.StartsWith(CustomPrefix, StringComparison.OrdinalIgnoreCase)
                && Guid.TryParse(id[CustomPrefix.Length..], out _))
            {
                kind = StudySetKinds.Custom;
                category = "Custom";
                return true;
            }

            if (id.StartsWith(CategoryPrefix, StringComparison.OrdinalIgnoreCase))
            {
                kind = StudySetKinds.Category;
                category = Uri.UnescapeDataString(id[CategoryPrefix.Length..]);
                return !string.IsNullOrWhiteSpace(category);
            }

            if (id.StartsWith(WeakAreaPrefix, StringComparison.OrdinalIgnoreCase))
            {
                kind = StudySetKinds.WeakArea;
                category = Uri.UnescapeDataString(id[WeakAreaPrefix.Length..]);
                return !string.IsNullOrWhiteSpace(category);
            }

            return false;
        }

        public static IQueryable<Tycoon.Backend.Domain.Entities.Question> BuildApprovedQuestionsQuery(
            IAppDb db,
            string? category = null)
        {
            var query = db.Questions
                .AsNoTracking()
                .Include(q => q.Options)
                .Where(q => q.Status == "Approved");

            if (!string.IsNullOrWhiteSpace(category))
                query = query.Where(q => q.Category == category.Trim());

            return query;
        }

        public static IQueryable<Tycoon.Backend.Domain.Entities.Question> BuildCategoryCountsQuery(IAppDb db)
        {
            return BuildApprovedQuestionsQuery(db)
                .AsQueryable();
        }

        public static async Task<StudySetDetailDto?> BuildStudySetDetailAsync(
            IAppDb db,
            string id,
            Guid? playerId,
            int count,
            CancellationToken ct)
        {
            if (!TryParseId(id, out var kind, out var category))
                return null;

            var take = count <= 0 ? 20 : Math.Clamp(count, 1, 50);

            List<StudySetQuestionDto> questions;

            if (kind == StudySetKinds.Favorites)
            {
                if (!playerId.HasValue || playerId.Value == Guid.Empty)
                    return null;

                questions = await db.QuestionStudyFavorites
                    .AsNoTracking()
                    .Where(x => x.PlayerId == playerId.Value)
                    .Join(
                        db.Questions
                            .AsNoTracking()
                            .Include(q => q.Options)
                            .Where(q => q.Status == "Approved"),
                        favorite => favorite.QuestionId,
                        question => question.Id,
                        (_, q) => q)
                    .OrderBy(q => q.Category)
                    .ThenBy(q => q.Difficulty)
                    .ThenBy(q => q.Text)
                    .Take(take)
                    .Select(q => new StudySetQuestionDto(
                        q.Id,
                        q.Text,
                        q.Category,
                        q.Difficulty,
                        q.Options.Select(o => new QuestionOptionDto(o.OptionId, o.Text)).ToList(),
                        q.CorrectOptionId,
                        q.MediaKey))
                    .ToListAsync(ct);
            }
            else if (kind == StudySetKinds.Custom)
            {
                if (!playerId.HasValue || !Guid.TryParse(id[CustomPrefix.Length..], out var studySetId))
                    return null;

                var studySet = await db.StudySets
                    .AsNoTracking()
                    .Include(x => x.Items)
                    .FirstOrDefaultAsync(x => x.Id == studySetId && x.PlayerId == playerId.Value, ct);

                if (studySet is null)
                    return null;

                var orderedIds = studySet.Items.OrderBy(x => x.Order).Select(x => x.QuestionId).Take(take).ToList();
                questions = await db.Questions
                    .AsNoTracking()
                    .Include(q => q.Options)
                    .Where(q => q.Status == "Approved" && orderedIds.Contains(q.Id))
                    .Select(q => new StudySetQuestionDto(
                        q.Id,
                        q.Text,
                        q.Category,
                        q.Difficulty,
                        q.Options.Select(o => new QuestionOptionDto(o.OptionId, o.Text)).ToList(),
                        q.CorrectOptionId,
                        q.MediaKey))
                    .ToListAsync(ct);

                questions = orderedIds
                    .Join(questions, idValue => idValue, q => q.Id, (_, q) => q)
                    .ToList();
            }
            else if (kind == StudySetKinds.DueReview)
            {
                if (!playerId.HasValue || playerId.Value == Guid.Empty)
                    return null;

                var now = DateTimeOffset.UtcNow;
                var dueQuestionIds = await db.StudyCardStates
                    .AsNoTracking()
                    .Where(x => x.PlayerId == playerId.Value && x.NextReviewAtUtc.HasValue)
                    .OrderBy(x => x.NextReviewAtUtc)
                    .ThenBy(x => x.EaseFactor)
                    .ThenBy(x => x.SuccessStreak)
                    .Select(x => x.QuestionId)
                    .Take(take)
                    .ToListAsync(ct);

                questions = await db.Questions
                    .AsNoTracking()
                    .Include(q => q.Options)
                    .Where(q => q.Status == "Approved" && dueQuestionIds.Contains(q.Id))
                    .Select(q => new StudySetQuestionDto(
                        q.Id,
                        q.Text,
                        q.Category,
                        q.Difficulty,
                        q.Options.Select(o => new QuestionOptionDto(o.OptionId, o.Text)).ToList(),
                        q.CorrectOptionId,
                        q.MediaKey))
                    .ToListAsync(ct);

                questions = dueQuestionIds
                    .Join(questions, idValue => idValue, q => q.Id, (_, q) => q)
                    .ToList();
            }
            else
            {
                questions = await BuildApprovedQuestionsQuery(db, category)
                    .OrderBy(q => q.Difficulty)
                    .ThenBy(q => q.Text)
                    .Take(take)
                    .Select(q => new StudySetQuestionDto(
                        q.Id,
                        q.Text,
                        q.Category,
                        q.Difficulty,
                        q.Options.Select(o => new QuestionOptionDto(o.OptionId, o.Text)).ToList(),
                        q.CorrectOptionId,
                        q.MediaKey))
                    .ToListAsync(ct);
            }

            if (questions.Count == 0)
                return null;

            var title = kind switch
            {
                StudySetKinds.WeakArea => $"Weak Area: {category}",
                StudySetKinds.Favorites => "Favorites Study Set",
                StudySetKinds.Custom => category,
                StudySetKinds.DueReview => "Due Review Study Set",
                _ => $"{category} Study Set"
            };

            var description = kind switch
            {
                StudySetKinds.WeakArea => $"Practice {category} questions selected for weak-area rehearsal.",
                StudySetKinds.Favorites => "Review your saved favorite questions in one dedicated study set.",
                StudySetKinds.Custom => "Review your saved custom study set.",
                StudySetKinds.DueReview => "Review cards that are due next based on spaced repetition state.",
                _ => $"Review approved {category} questions in a dedicated study set."
            };

            if (kind == StudySetKinds.Custom && Guid.TryParse(id[CustomPrefix.Length..], out var customStudySetId))
            {
                var customStudySet = await db.StudySets
                    .AsNoTracking()
                    .FirstAsync(x => x.Id == customStudySetId, ct);
                title = customStudySet.Title;
                description = customStudySet.Description ?? description;
            }

            return new StudySetDetailDto(
                id,
                title,
                description,
                kind,
                category,
                questions.Count,
                questions);
        }
    }
}
