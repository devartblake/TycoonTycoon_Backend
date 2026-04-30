using System.Text.Json;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Domain.Personalization;

namespace Tycoon.Backend.Application.Personalization;

public sealed class PersonalizationAuditService : IPersonalizationAuditService
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    private readonly IAppDb _db;

    public PersonalizationAuditService(IAppDb db)
    {
        _db = db;
    }

    public async Task LogDecisionAsync(
        Guid playerId,
        Guid? recommendationId,
        string decisionType,
        string source,
        string reason,
        object inputSignals,
        object candidate,
        object guardrails,
        object finalDecision,
        CancellationToken ct = default)
    {
        _db.PersonalizationAuditLogs.Add(new PersonalizationAuditLog
        {
            Id = Guid.NewGuid(),
            PlayerId = playerId,
            RecommendationId = recommendationId,
            DecisionType = decisionType,
            Source = source,
            Reason = reason,
            InputSignalsJson = JsonSerializer.Serialize(inputSignals, _json),
            CandidateJson = JsonSerializer.Serialize(candidate, _json),
            GuardrailsAppliedJson = JsonSerializer.Serialize(guardrails, _json),
            FinalDecisionJson = JsonSerializer.Serialize(finalDecision, _json),
            CreatedAt = DateTimeOffset.UtcNow
        });

        await _db.SaveChangesAsync(ct);
    }
}
