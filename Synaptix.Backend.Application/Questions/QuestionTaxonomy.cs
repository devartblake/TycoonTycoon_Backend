using System.Text.Json;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.Questions;

public sealed record QuestionTaxonomyDefinition(
    string Key,
    string DisplayName,
    string Description,
    string Subject,
    string? Topic,
    string? GradeBand,
    string? AgeGroup,
    string Audience,
    IReadOnlyList<string> Aliases);

public sealed record ResolvedQuestionTaxonomy(
    string CanonicalCategory,
    string DisplayCategory,
    string Subject,
    string? Topic,
    string? Subtopic,
    string? GradeBand,
    string? AgeGroup,
    string Audience,
    string? SourceDataset,
    string? SourceQuestionId,
    string QuestionType,
    string MediaType,
    IReadOnlyList<string> TaxonomyTags);

public static class QuestionTaxonomy
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static readonly IReadOnlyList<QuestionTaxonomyDefinition> Definitions =
    [
        Def("arts", "Arts", "Visual arts, music, theater, and creative expressions", "arts", aliases: ["art", "creative_arts", "visual_arts"]),
        Def("science", "Science", "Physics, chemistry, biology, and scientific principles", "science", aliases: ["natural_science", "biology", "chemistry", "physics"]),
        Def("mathematics", "Mathematics", "Mathematical concepts, problems, and applications", "stem", aliases: ["math", "maths"]),
        Def("history", "History", "Historical events, civilizations, and world history", "humanities", aliases: ["world_history", "historical"]),
        Def("geography", "Geography", "World geography, countries, capitals, and landmarks", "social_studies"),
        Def("literature", "Literature", "Books, authors, poetry, and literary works", "humanities", aliases: ["world_literature"]),
        Def("technology", "Technology", "Computing, programming, and modern technology", "stem", aliases: ["tech", "computing", "computer_science", "engineering_technology"]),
        Def("health", "Health", "Health, medicine, nutrition, and wellness", "health", aliases: ["health_medicine"]),
        Def("sports", "Sports", "Sports, athletics, games, and fitness", "sports"),
        Def("entertainment", "Entertainment", "Movies, music, celebrities, and popular culture", "culture", aliases: ["film", "movies", "pop_culture"]),
        Def("economics", "Economics", "Economic principles, markets, and financial concepts", "business", aliases: ["economics_finance", "finance"]),
        Def("philosophy", "Philosophy", "Philosophical concepts, ethics, and critical thinking", "humanities"),
        Def("psychology", "Psychology", "Psychological concepts, behavior, and mental processes", "social_studies"),
        Def("politics", "Politics", "Political systems, governance, and civic knowledge", "civics", aliases: ["current_events", "current_affairs", "news"]),
        Def("law", "Law", "Legal concepts, rights, and judicial systems", "civics", aliases: ["civics_law"]),
        Def("environment", "Environment", "Environmental science, climate, and sustainability", "science", aliases: ["environmental_science"]),
        Def("media", "Media", "Media, journalism, communication, and digital platforms", "culture"),
        Def("social_studies", "Social Studies", "Social sciences, sociology, and human behavior", "social_studies", aliases: ["social"]),
        Def("architecture", "Architecture", "Architecture, building design, and structural concepts", "arts"),
        Def("art_history", "Art History", "History of art, artistic movements, and famous artworks", "arts"),
        Def("astronomy", "Astronomy", "Space, stars, planets, and astronomical phenomena", "science"),
        Def("business", "Business", "Business concepts, entrepreneurship, and management", "business"),
        Def("comparative_religions", "Comparative Religions", "World religions, beliefs, and spiritual practices", "humanities"),
        Def("global_cultures", "Global Cultures", "World cultures, traditions, and cultural studies", "humanities"),
        Def("statistics_data", "Statistics & Data", "Statistics, data analysis, and data literacy", "stem", aliases: ["statistics", "data"]),
        Def("kids", "Kids Questions", "Age-appropriate questions for children and families", "general", gradeBand: "k_5", ageGroup: "kids", audience: "k12", aliases: ["kids_questions"]),
        Def("kids_grade2", "Kids Grade 2", "Specific questions designed for grade 2 students", "general", gradeBand: "grade_2", ageGroup: "kids", audience: "k12", aliases: ["kidsgrade2", "class_2"]),
        Def("general", "General Knowledge", "Mixed categories and general knowledge", "general", aliases: ["general_knowledge", "mixed"])
    ];

    private static readonly Dictionary<string, QuestionTaxonomyDefinition> DefinitionsByAlias =
        Definitions
            .SelectMany(d => new[] { d.Key, d.DisplayName }.Concat(d.Aliases).Select(alias => (Alias: NormalizeKey(alias), Definition: d)))
            .GroupBy(x => x.Alias)
            .ToDictionary(g => g.Key, g => g.First().Definition, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyDictionary<string, IReadOnlyList<string>> Aliases =>
        Definitions.ToDictionary(d => d.Key, d => d.Aliases, StringComparer.OrdinalIgnoreCase);

    public static ResolvedQuestionTaxonomy Resolve(
        string? category,
        QuestionTaxonomyInputDto? input,
        string? sourceDataset,
        string? sourceQuestionId,
        string? questionType,
        string? mediaType,
        string? mediaKey,
        IEnumerable<string>? tags = null)
    {
        var categoryKey = FirstNonBlank(input?.CanonicalCategory, category, "general")!;
        var definition = ResolveDefinition(categoryKey);
        var inferredDataset = FirstNonBlank(input?.SourceDataset, sourceDataset);
        var datasetHints = InferFromDataset(inferredDataset);
        var taxonomyTags = (input?.TaxonomyTags ?? Array.Empty<string>())
            .Concat(tags ?? Array.Empty<string>())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => NormalizeKey(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(t => t)
            .ToList();

        return new ResolvedQuestionTaxonomy(
            definition.Key,
            FirstNonBlank(input?.DisplayCategory, definition.DisplayName, category) ?? "General",
            FirstNonBlank(input?.Subject, datasetHints.Subject, definition.Subject) ?? "general",
            FirstNonBlank(input?.Topic, datasetHints.Topic, definition.Topic),
            Clean(input?.Subtopic),
            FirstNonBlank(input?.GradeBand, datasetHints.GradeBand, definition.GradeBand),
            FirstNonBlank(input?.AgeGroup, datasetHints.AgeGroup, definition.AgeGroup),
            FirstNonBlank(input?.Audience, datasetHints.Audience, definition.Audience) ?? "general",
            Clean(inferredDataset),
            Clean(sourceQuestionId),
            NormalizeKey(FirstNonBlank(questionType, "multiple_choice")!),
            NormalizeKey(FirstNonBlank(mediaType, InferMediaType(mediaKey))!),
            taxonomyTags);
    }

    public static QuestionTaxonomyDefinition ResolveDefinition(string? value)
    {
        var key = NormalizeKey(value ?? "general");
        return DefinitionsByAlias.TryGetValue(key, out var definition)
            ? definition
            : Definitions.First(d => d.Key == "general");
    }

    public static string NormalizeKey(string value)
    {
        var chars = value.Trim().ToLowerInvariant()
            .Replace("&", "and")
            .Replace("+", "and")
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '_')
            .ToArray();
        var normalized = new string(chars);
        while (normalized.Contains("__", StringComparison.Ordinal))
            normalized = normalized.Replace("__", "_");
        return normalized.Trim('_');
    }

    public static bool MatchesCategory(Question question, IEnumerable<string> categories)
    {
        var keys = categories
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => ResolveDefinition(c).Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return keys.Count == 0 || keys.Contains(question.CanonicalCategory) || keys.Contains(NormalizeKey(question.Category));
    }

    public static string ToTagsJson(IEnumerable<string> tags) =>
        JsonSerializer.Serialize(tags.Where(t => !string.IsNullOrWhiteSpace(t)).Select(NormalizeKey).Distinct().OrderBy(t => t), JsonOptions);

    public static IReadOnlyList<string> ParseTagsJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }

    public static QuestionTaxonomyDto ToDto(Question q) => new(
        q.CanonicalCategory,
        q.DisplayCategory,
        q.Subject,
        q.Topic,
        q.Subtopic,
        q.GradeBand,
        q.AgeGroup,
        q.Audience,
        q.SourceDataset,
        q.SourceQuestionId,
        q.QuestionType,
        q.MediaType,
        ParseTagsJson(q.TaxonomyTagsJson));

    private static QuestionTaxonomyDefinition Def(
        string key,
        string displayName,
        string description,
        string subject,
        string? topic = null,
        string? gradeBand = null,
        string? ageGroup = null,
        string audience = "general",
        IReadOnlyList<string>? aliases = null) =>
        new(key, displayName, description, subject, topic, gradeBand, ageGroup, audience, aliases ?? []);

    private static (string? Subject, string? Topic, string? GradeBand, string? AgeGroup, string? Audience) InferFromDataset(string? dataset)
    {
        if (string.IsNullOrWhiteSpace(dataset)) return default;
        var key = NormalizeKey(dataset);
        if (key.Contains("class_k", StringComparison.Ordinal)) return ("general", "class_k", "kindergarten", "kids", "k12");
        for (var grade = 1; grade <= 12; grade++)
        {
            if (key.Contains($"class_{grade}", StringComparison.Ordinal))
            {
                var band = grade <= 5 ? "k_5" : grade <= 8 ? "middle_school" : "high_school";
                var age = grade <= 5 ? "kids" : grade <= 8 ? "teen" : "teen";
                return ("general", $"grade_{grade}", band, age, "k12");
            }
        }

        if (key.Contains("kids", StringComparison.Ordinal)) return ("general", "kids", "k_5", "kids", "k12");
        return default;
    }

    private static string InferMediaType(string? mediaKey)
    {
        if (string.IsNullOrWhiteSpace(mediaKey)) return "text";
        var lower = mediaKey.Trim().ToLowerInvariant();
        if (lower.EndsWith(".mp3") || lower.EndsWith(".wav") || lower.EndsWith(".ogg")) return "audio";
        if (lower.EndsWith(".mp4") || lower.EndsWith(".webm") || lower.EndsWith(".mov")) return "video";
        return "image";
    }

    private static string? FirstNonBlank(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim();

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
