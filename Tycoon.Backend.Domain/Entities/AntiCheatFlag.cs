namespace Tycoon.Backend.Domain.Entities
{
    public enum AntiCheatSeverity
    {
        Info = 1,
        Warning = 2,
        Severe = 3
    }

    public enum AntiCheatAction
    {
        LogOnly = 1,
        Warn = 2,
        BlockRewards = 3
    }

    public sealed class AntiCheatFlag
    {
        public Guid Id { get; private set; } = Guid.NewGuid();

        public Guid MatchId { get; private set; }
        public Guid? PlayerId { get; private set; }

        public string RuleKey { get; private set; } = string.Empty;
        public AntiCheatSeverity Severity { get; private set; }
        public AntiCheatAction Action { get; private set; }

        public string Message { get; private set; } = string.Empty;
        public string? EvidenceJson { get; private set; }

        public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

        private AntiCheatFlag() { }

        public AntiCheatFlag(
            Guid matchId,
            Guid? playerId,
            string ruleKey,
            AntiCheatSeverity severity,
            AntiCheatAction action,
            string message,
            string? evidenceJson,
            DateTimeOffset createdAtUtc)
        {
            MatchId = matchId;
            PlayerId = playerId;
            RuleKey = (ruleKey ?? "").Trim();
            Severity = severity;
            Action = action;
            Message = message;
            EvidenceJson = evidenceJson;
            CreatedAtUtc = DateTimeOffset.UtcNow;
        }
        public static AntiCheatFlag LeaderLeftPartyDuringMatch(
            Guid playerId,
            Guid matchId,
            Guid partyId)
        {
            return new AntiCheatFlag(
                matchId: matchId,
                playerId: playerId,
                ruleKey: "party-leader-left",
                severity: AntiCheatSeverity.Warning,
                action: AntiCheatAction.Warn,
                message: $"Leader left party {partyId} during active match.",
                evidenceJson: null,
                createdAtUtc: DateTimeOffset.UtcNow
            );
        }

    }
}
