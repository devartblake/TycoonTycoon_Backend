using Mediator;

namespace Synaptix.Backend.Application.Quiz;

public sealed record CompleteQuiz(
    Guid PlayerId,
    Guid EventId,
    int XpEarned,
    int CoinsEarned
) : IRequest<CompleteQuizResponse>;

public sealed record CompleteQuizResponse(
    string Status,
    int Xp,
    int Coins,
    int Diamonds
);
