namespace Tycoon.Shared.Contracts.Dtos
{
    public sealed record RunEscalationRequest(
        int WindowHours,          // e.g. 24
        int MaxPlayers,           // safety cap per run (e.g. 500)
        bool DryRun               // if true: report only, do not write DB
    );

    public sealed record EscalationDecisionDto(
        Guid PlayerId,
        int CurrentStatus,
        int ProposedStatus,
        int SevereCount,
        int WarningCount,
        DateTimeOffset WindowStartUtc,
        DateTimeOffset WindowEndUtc,
        string Reason
    );

    public sealed record RunEscalationResponse(
        bool DryRun,
        int EvaluatedPlayers,
        int ChangedPlayers,
        IReadOnlyList<EscalationDecisionDto> Decisions
    );
}
