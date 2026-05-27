using Mediator;
using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.Questions
{
    public sealed record AdminGetQuestion(Guid Id) : IRequest<QuestionDto?>;

    public sealed class AdminGetQuestionHandler(IAppDb db)
        : IRequestHandler<AdminGetQuestion, QuestionDto?>
    {
        public async ValueTask<QuestionDto?> Handle(AdminGetQuestion r, CancellationToken ct)
        {
            var q = await db.Questions.AsNoTracking().FirstOrDefaultAsync(x => x.Id == r.Id, ct);
            if (q is null) return null;

            var options = await db.QuestionOptions.AsNoTracking()
                .Where(o => o.QuestionId == q.Id)
                .OrderBy(o => o.OptionId)
                .Select(o => new QuestionOptionDto(o.OptionId, o.Text))
                .ToListAsync(ct);

            var tags = await db.QuestionTags.AsNoTracking()
                .Where(t => t.QuestionId == q.Id)
                .Select(t => t.Tag)
                .OrderBy(t => t)
                .ToListAsync(ct);

            return new QuestionDto(
                q.Id,
                q.Text,
                q.Category,
                q.Difficulty,
                q.Status,
                options,
                q.CorrectOptionId,
                tags,
                q.MediaKey,
                MediaUrl: null,
                q.CreatedAtUtc,
                q.UpdatedAtUtc
            );
        }
    }
}
