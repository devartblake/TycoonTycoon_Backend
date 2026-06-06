namespace Synaptix.Backend.Application.Questions;

public sealed class QuestionTaxonomySidecarOptions
{
    public const string SectionName = "Sidecar";

    public string BaseUrl { get; set; } = "http://localhost:8100";
    public bool QuestionTaxonomyEnabled { get; set; } = false;
    public int QuestionTaxonomyTimeoutMs { get; set; } = 3000;
    public bool QuestionTaxonomyAutoApplyEnabled { get; set; } = false;
    public decimal QuestionTaxonomyAutoApplyMinConfidence { get; set; } = 0.85m;
}
