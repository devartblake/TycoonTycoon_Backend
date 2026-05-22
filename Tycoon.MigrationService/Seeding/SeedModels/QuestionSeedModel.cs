using System.Text.Json;

namespace Tycoon.MigrationService.Seeding.SeedModels;

public sealed class QuestionSeedModel
{
    public string? Text { get; set; }
    public string? Question { get; set; }
    public string Category { get; set; } = "General";
    public JsonElement Difficulty { get; set; }
    public string? CorrectOptionId { get; set; }
    public string? CorrectAnswer { get; set; }
    public string? MediaKey { get; set; }
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
