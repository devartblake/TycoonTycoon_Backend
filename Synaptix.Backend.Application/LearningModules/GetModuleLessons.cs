using Mediator;
using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.LearningModules
{
    public sealed record GetModuleLessons(Guid ModuleId)
        : IRequest<IReadOnlyList<ModuleLessonDto>?>;

    public sealed class GetModuleLessonsHandler
        : IRequestHandler<GetModuleLessons, IReadOnlyList<ModuleLessonDto>?>
    {
        private readonly IAppDb _db;

        public GetModuleLessonsHandler(IAppDb db) => _db = db;

        public async ValueTask<IReadOnlyList<ModuleLessonDto>?> Handle(
            GetModuleLessons request,
            CancellationToken ct)
        {
            // Confirm module exists and is published
            var moduleExists = await _db.LearningModules
                .AsNoTracking()
                .AnyAsync(m => m.Id == request.ModuleId && m.IsPublished, ct);

            if (!moduleExists)
                return null;

            // Load lessons joined with their questions and options
            var lessons = await _db.ModuleLessons
                .AsNoTracking()
                .Where(l => l.ModuleId == request.ModuleId)
                .OrderBy(l => l.Order)
                .Join(
                    _db.Questions.AsNoTracking(),
                    lesson => lesson.QuestionId,
                    question => question.Id,
                    (lesson, question) => new { lesson, question }
                )
                .ToListAsync(ct);

            if (lessons.Count == 0)
                return Array.Empty<ModuleLessonDto>();

            // Load options for all questions in one query
            var questionIds = lessons.Select(x => x.question.Id).ToList();
            var options = await _db.QuestionOptions
                .AsNoTracking()
                .Where(o => questionIds.Contains(o.QuestionId))
                .ToListAsync(ct);

            var optionsByQuestion = options
                .GroupBy(o => o.QuestionId)
                .ToDictionary(
                    g => g.Key,
                    g => (IReadOnlyList<QuestionOptionDto>)g
                        .OrderBy(o => o.OptionId)
                        .Select(o => new QuestionOptionDto(o.OptionId, o.Text))
                        .ToList()
                );

            return lessons
                .Select(x => new ModuleLessonDto(
                    LessonId: x.lesson.Id,
                    Order: x.lesson.Order,
                    QuestionId: x.question.Id,
                    QuestionText: x.question.Text,
                    QuestionCategory: x.question.Category,
                    Options: optionsByQuestion.TryGetValue(x.question.Id, out var opts)
                        ? opts
                        : Array.Empty<QuestionOptionDto>(),
                    CorrectOptionId: x.question.CorrectOptionId,
                    Explanation: x.lesson.Explanation
                ))
                .ToList();
        }
    }
}
