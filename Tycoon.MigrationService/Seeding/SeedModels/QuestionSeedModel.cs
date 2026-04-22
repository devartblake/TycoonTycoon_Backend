namespace Tycoon.MigrationService.Seeding.SeedModels;

public sealed record QuestionSeedModel(
    string Text,
    string Category,
    string Difficulty,
    string CorrectOptionId,
    string? MediaKey,
    QuestionOptionSeedModel[] Options,
    string[] Tags,
    string Status
);

public sealed record QuestionOptionSeedModel(
    string OptionId,
    string Text
);
