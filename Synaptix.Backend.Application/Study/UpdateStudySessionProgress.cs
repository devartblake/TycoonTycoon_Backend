using Mediator;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.Study
{
    public sealed record UpdateStudySessionProgress(
        Guid SessionId,
        Guid PlayerId,
        Guid? QuestionId,
        string? SelectedOptionId,
        int? CurrentQuestionIndex,
        string? FlashcardAction,
        int? Confidence,
        bool? AnswerRevealed,
        bool IsCompleted) : IRequest<StudySessionProgressResult>;

    public sealed record StudySessionProgressResult(
        string Status,
        StudySessionDto? Session = null,
        string? ErrorCode = null,
        string? ErrorMessage = null);

    public sealed class UpdateStudySessionProgressHandler
        : IRequestHandler<UpdateStudySessionProgress, StudySessionProgressResult>
    {
        private readonly IAppDb _db;

        public UpdateStudySessionProgressHandler(IAppDb db) => _db = db;

        public async ValueTask<StudySessionProgressResult> Handle(UpdateStudySessionProgress request, CancellationToken ct)
        {
            var session = await _db.StudySessions
                .FirstOrDefaultAsync(x => x.Id == request.SessionId && x.PlayerId == request.PlayerId, ct);

            if (session is null)
                return new StudySessionProgressResult("NotFound", ErrorCode: "NOT_FOUND", ErrorMessage: "Study session not found.");

            var questionIds = StudySessionMapper.ReadQuestionIds(session);
            var answerKey = StudySessionMapper.ReadAnswerKey(session);
            var answeredResults = StudySessionMapper.ReadAnsweredResults(session);
            var interactionStates = StudySessionMapper.ReadInteractionStates(session);

            if (request.QuestionId.HasValue)
            {
                if (string.IsNullOrWhiteSpace(request.SelectedOptionId)
                    && string.IsNullOrWhiteSpace(request.FlashcardAction)
                    && !request.AnswerRevealed.HasValue
                    && !request.Confidence.HasValue)
                {
                    return new StudySessionProgressResult(
                        "ValidationError",
                        ErrorCode: "VALIDATION_ERROR",
                        ErrorMessage: "One of selectedOptionId, flashcardAction, answerRevealed, or confidence is required when questionId is provided.");
                }

                if (!questionIds.Contains(request.QuestionId.Value) || !answerKey.TryGetValue(request.QuestionId.Value, out var correctOptionId))
                {
                    return new StudySessionProgressResult(
                        "ValidationError",
                        ErrorCode: "VALIDATION_ERROR",
                        ErrorMessage: "questionId does not belong to this study session.");
                }

                if (!string.IsNullOrWhiteSpace(request.SelectedOptionId))
                {
                    var isCorrect = string.Equals(
                        request.SelectedOptionId.Trim(),
                        correctOptionId,
                        StringComparison.Ordinal);
                    answeredResults[request.QuestionId.Value] = isCorrect;

                    var cardState = await GetOrCreateCardStateAsync(request.PlayerId, request.QuestionId.Value, ct);
                    cardState.ApplySelfTest(isCorrect);
                }

                interactionStates[request.QuestionId.Value] = new StoredStudyInteraction(
                    request.FlashcardAction,
                    request.Confidence.HasValue ? Math.Clamp(request.Confidence.Value, 1, 5) : interactionStates.GetValueOrDefault(request.QuestionId.Value)?.Confidence,
                    request.AnswerRevealed ?? interactionStates.GetValueOrDefault(request.QuestionId.Value)?.AnswerRevealed ?? false,
                    DateTimeOffset.UtcNow);

                if (!string.IsNullOrWhiteSpace(request.FlashcardAction))
                {
                    var cardState = await GetOrCreateCardStateAsync(request.PlayerId, request.QuestionId.Value, ct);
                    cardState.ApplyFlashcard(request.FlashcardAction, request.Confidence);
                }
            }

            var currentQuestionIndex = request.CurrentQuestionIndex ?? session.CurrentQuestionIndex;
            session.ApplyProgress(answeredResults, currentQuestionIndex, request.IsCompleted);
            session.ReplaceInteractionStates(JsonSerializer.Serialize(
                interactionStates.ToDictionary(x => x.Key.ToString("D"), x => x.Value)));
            await _db.SaveChangesAsync(ct);

            return new StudySessionProgressResult("Ok", StudySessionMapper.ToDto(session));
        }

        private async Task<StudyCardState> GetOrCreateCardStateAsync(Guid playerId, Guid questionId, CancellationToken ct)
        {
            var cardState = await _db.StudyCardStates
                .FirstOrDefaultAsync(x => x.PlayerId == playerId && x.QuestionId == questionId, ct);

            if (cardState is not null)
                return cardState;

            cardState = new StudyCardState(playerId, questionId);
            _db.StudyCardStates.Add(cardState);
            return cardState;
        }
    }
}
