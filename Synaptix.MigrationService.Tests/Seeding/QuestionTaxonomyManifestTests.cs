using System.Text.Json;
using FluentAssertions;
using Synaptix.Backend.Application.Questions;
using Synaptix.MigrationService.Seeding.SeedModels;

namespace Synaptix.MigrationService.Tests.Seeding;

public sealed class QuestionTaxonomyManifestTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task DatasetManifest_Deserializes_AndMapsKnownCategories()
    {
        var manifest = await ReadDatasetManifestAsync();

        manifest.Should().Contain(x => x.Key == "seeds/questions/science.json" &&
                                       x.CanonicalCategory == "science" &&
                                       x.Subject == "stem");
        foreach (var dataset in manifest)
        {
            QuestionTaxonomy.ResolveDefinition(dataset.CanonicalCategory ?? dataset.DisplayCategory ?? "general")
                .Should().NotBeNull();
        }
    }

    [Fact]
    public async Task DatasetManifest_MapsClassDatasetsToExpectedGradeBands()
    {
        var manifest = await ReadDatasetManifestAsync();

        manifest.Should().Contain(x => x.Key.EndsWith("class_K_questions.json", StringComparison.OrdinalIgnoreCase) &&
                                       x.GradeBand == "k_2" &&
                                       x.Audience == "kids");
        manifest.Should().Contain(x => x.Key.EndsWith("class_6_questions.json", StringComparison.OrdinalIgnoreCase) &&
                                       x.GradeBand == "middle_school" &&
                                       x.Audience == "teen");
        manifest.Should().Contain(x => x.Key.EndsWith("class_9_questions.json", StringComparison.OrdinalIgnoreCase) &&
                                       x.GradeBand == "high_school" &&
                                       x.Audience == "teen");
    }

    [Fact]
    public async Task TaxonomyManifest_ContainsFrontendFallbackAliases()
    {
        var json = await File.ReadAllTextAsync(FindRepoFile("Synaptix.MigrationService/seeds/questions/taxonomy-manifest.json"));
        using var document = JsonDocument.Parse(json);
        var categories = document.RootElement.GetProperty("canonicalCategories").EnumerateArray().ToArray();

        categories.Should().Contain(c => c.GetProperty("key").GetString() == "science" &&
                                         c.GetProperty("aliases").EnumerateArray().Any(a => a.GetString() == "natural_science"));
        categories.Should().Contain(c => c.GetProperty("key").GetString() == "mathematics" &&
                                         c.GetProperty("aliases").EnumerateArray().Any(a => a.GetString() == "math"));
        categories.Should().Contain(c => c.GetProperty("key").GetString() == "kids" &&
                                         c.GetProperty("aliases").EnumerateArray().Any(a => a.GetString() == "kids_grade2"));
    }

    private static async Task<List<QuestionDatasetManifestSeedModel>> ReadDatasetManifestAsync()
    {
        var json = await File.ReadAllTextAsync(FindRepoFile("Synaptix.MigrationService/seeds/questions/question-dataset-manifest.json"));
        return JsonSerializer.Deserialize<List<QuestionDatasetManifestSeedModel>>(json, JsonOptions) ?? [];
    }

    private static string FindRepoFile(string relativePath)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var path = Path.Combine(directory.FullName, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(path)) return path;
            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not find repository file '{relativePath}'.");
    }
}
