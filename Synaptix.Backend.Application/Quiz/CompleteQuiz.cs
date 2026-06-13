using Mediator;

namespace Synaptix.Backend.Application.Quiz;

public sealed record CompleteQuiz(
    Guid PlayerId,
    Guid EventId,
    int CorrectAnswers,
    int TotalQuestions,
    int AwardedXp,
    int AwardedCoins
) : IRequest<CompleteQuizResponse>;

public sealed record CompleteQuizResponse(
    string Status,
    int Xp,
    int Coins,
    int Diamonds,
    int AwardedXp,
    int AwardedCoins,
    int CorrectAnswers,
    int TotalQuestions
);
