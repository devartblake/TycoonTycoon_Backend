namespace Synaptix.Backend.Domain.Entities;

public sealed class QuestionTaxonomySuggestion
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid? QuestionId { get; private set; }
    public string? SourceDataset { get; private set; }
    public string? SourceQuestionId { get; private set; }
    public string SuggestedTaxonomyJson { get; private set; } = "{}";
    public string ConfidenceJson { get; private set; } = "{}";
    public string WarningsJson { get; private set; } = "[]";
    public decimal OverallConfidence { get; private set; }
    public string ModelVersion { get; private set; } = "unknown";
    public string Status { get; private set; } = "Pending";
    public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? AppliedAtUtc { get; private set; }
    public string? ReviewedBy { get; private set; }
    public string? ReviewNote { get; private set; }

    private QuestionTaxonomySuggestion() { }

    public QuestionTaxonomySuggestion(
        Guid? questionId,
        string? sourceDataset,
        string? sourceQuestionId,
        string suggestedTaxonomyJson,
        string confidenceJson,
        string warningsJson,
        decimal overallConfidence,
        string modelVersion)
    {
        QuestionId = questionId;
        SourceDataset = Normalize(sourceDataset);
        SourceQuestionId = Normalize(sourceQuestionId);
        SuggestedTaxonomyJson = string.IsNullOrWhiteSpace(suggestedTaxonomyJson) ? "{}" : suggestedTaxonomyJson.Trim();
        ConfidenceJson = string.IsNullOrWhiteSpace(confidenceJson) ? "{}" : confidenceJson.Trim();
        WarningsJson = string.IsNullOrWhiteSpace(warningsJson) ? "[]" : warningsJson.Trim();
        OverallConfidence = overallConfidence;
        ModelVersion = string.IsNullOrWhiteSpace(modelVersion) ? "unknown" : modelVersion.Trim();
    }

    public void MarkApplied(string? reviewedBy, string? reviewNote)
    {
        Status = "Applied";
        AppliedAtUtc = DateTimeOffset.UtcNow;
        ReviewedBy = Normalize(reviewedBy);
        ReviewNote = Normalize(reviewNote);
    }

    public void MarkRejected(string? reviewedBy, string? reviewNote)
    {
        Status = "Rejected";
        ReviewedBy = Normalize(reviewedBy);
        ReviewNote = Normalize(reviewNote);
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
