using System.Text.Json;

namespace Synaptix.MigrationService.Seeding.SeedModels;

public sealed class QuestionSeedModel
{
    public string? Id { get; set; }
    public string? SourceDataset { get; set; }
    public string? Text { get; set; }
    public string? Question { get; set; }
    public string Category { get; set; } = "General";
    public string? CanonicalCategory { get; set; }
    public string? DisplayCategory { get; set; }
    public string? Subject { get; set; }
    public string? Topic { get; set; }
    public string? Subtopic { get; set; }
    public string? GradeBand { get; set; }
    public string? AgeGroup { get; set; }
    public string? Audience { get; set; }
    public string? Type { get; set; }
    public string? MediaType { get; set; }
    public JsonElement Difficulty { get; set; }
    public string? CorrectOptionId { get; set; }
    public string? CorrectAnswer { get; set; }
    public string? MediaKey { get; set; }
    public string? ImageUrl { get; set; }
    public string? VideoUrl { get; set; }
    public string? AudioUrl { get; set; }
    public QuestionOptionSeedModel[] Options { get; set; } = [];
    public QuestionOptionSeedModel[] Answers { get; set; } = [];
    public string[] Tags { get; set; } = [];
    public string Status { get; set; } = "Approved";
}

public sealed class QuestionOptionSeedModel
{
    public string? OptionId { get; set; }
    public string Text { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
}

public sealed class QuestionDatasetManifestSeedModel
{
    public string Key { get; set; } = string.Empty;
    public string? Dataset { get; set; }
    public string? CanonicalCategory { get; set; }
    public string? DisplayCategory { get; set; }
    public string? Subject { get; set; }
    public string? Topic { get; set; }
    public string? Subtopic { get; set; }
    public string? GradeBand { get; set; }
    public string? AgeGroup { get; set; }
    public string? Audience { get; set; }
    public string[] Tags { get; set; } = [];
}
