namespace Synaptix.Backend.Application.Study
{
    internal sealed record StoredStudyInteraction(
        string? FlashcardAction,
        int? Confidence,
        bool AnswerRevealed,
        DateTimeOffset? LastInteractedAtUtc
    );
}
