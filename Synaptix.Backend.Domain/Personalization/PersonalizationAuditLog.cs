namespace Synaptix.Backend.Domain.Personalization;

public sealed class PersonalizationAuditLog
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }

    public Guid? RecommendationId { get; set; }

    public string DecisionType { get; set; } = "";
    public string Source { get; set; } = "backend";
    public string Reason { get; set; } = "";

    public string InputSignalsJson { get; set; } = "{}";
    public string CandidateJson { get; set; } = "{}";
    public string GuardrailsAppliedJson { get; set; } = "{}";
    public string FinalDecisionJson { get; set; } = "{}";

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
