using MediatR;
using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.Abstractions;

namespace Synaptix.Backend.Application.Matches;

public sealed record EvaluateMatchAnswer(Guid QuestionId, string SelectedOptionId)
    : IRequest<MatchAnswerEvaluationResult>;

public sealed record MatchAnswerEvaluationResult(
    string CorrectOptionId,
    bool IsCorrect,
    int PointsAwarded
);

public sealed class EvaluateMatchAnswerHandler(IAppDb db)
    : IRequestHandler<EvaluateMatchAnswer, MatchAnswerEvaluationResult>
{
    public async Task<MatchAnswerEvaluationResult> Handle(EvaluateMatchAnswer r, CancellationToken ct)
    {
        var question = await db.Questions.AsNoTracking()
            .FirstOrDefaultAsync(q => q.Id == r.QuestionId, ct);

        if (question is null || string.IsNullOrWhiteSpace(question.CorrectOptionId))
            return new MatchAnswerEvaluationResult(string.Empty, false, 0);

        var isCorrect = string.Equals(r.SelectedOptionId, question.CorrectOptionId, StringComparison.OrdinalIgnoreCase);
        var points = isCorrect ? 100 : 0;

        return new MatchAnswerEvaluationResult(
            question.CorrectOptionId,
            isCorrect,
            points);
    }
}
