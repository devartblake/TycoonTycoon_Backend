namespace Synaptix.Backend.Application.Personalization;

public interface IPersonalizationAuditService
{
    Task LogDecisionAsync(
        Guid playerId,
        Guid? recommendationId,
        string decisionType,
        string source,
        string reason,
        object inputSignals,
        object candidate,
        object guardrails,
        object finalDecision,
        CancellationToken ct = default);
}
