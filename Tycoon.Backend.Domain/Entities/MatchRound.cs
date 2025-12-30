namespace Tycoon.Backend.Domain.Entities
{
    public class MatchRound
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public Guid MatchId { get; private set; }
        public int Index { get; private set; }
        public bool Correct { get; private set; }
        public int AnswerTimeMs { get; private set; }
        public int Points { get; private set; }

        private MatchRound() { }
        public MatchRound(Guid matchId, int index, bool correct, int answerTimeMs, int points)
        { MatchId = matchId; Index = index; Correct = correct; AnswerTimeMs = answerTimeMs; Points = points; }
    }
}
