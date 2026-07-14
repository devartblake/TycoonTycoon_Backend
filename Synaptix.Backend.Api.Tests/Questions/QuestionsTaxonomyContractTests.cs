using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Synaptix.Backend.Api.Tests.TestHost;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Backend.Infrastructure.Persistence;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Tests.Questions;

public sealed class QuestionsTaxonomyContractTests : IClassFixture<SynaptixApiFactory>
{
    private readonly SynaptixApiFactory _factory;
    private readonly HttpClient _http;

    public QuestionsTaxonomyContractTests(SynaptixApiFactory factory)
    {
        _factory = factory;
        _http = factory.CreateClient();
    }

    [Fact]
    public async Task QuestionSet_CategoryMatching_IsAliasAware_AndCaseInsensitive()
    {
        await SeedQuestionAsync("Taxonomy science alias question?", "Science", QuestionDifficulty.Easy,
            canonicalCategory: "science", subject: "stem", topic: "physics", sourceDataset: "taxonomy-test/science", tags: ["physics"]);

        var response = await _http.GetAsync("/api/v1/questions/set?category=natural_science&count=5");
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<QuestionSetDto>(TestJson.Default);
        payload!.Questions.Should().Contain(q => q.Text == "Taxonomy science alias question?");
        payload.Questions.Select(q => q.Taxonomy).Should().OnlyContain(t => t != null);
    }

    [Fact]
    public async Task QuestionSet_FiltersByTaxonomyFields()
    {
        await SeedQuestionAsync("Taxonomy grade-band question?", "Kids", QuestionDifficulty.Medium,
            canonicalCategory: "kids", subject: "k12", topic: "math", gradeBand: "k_2", ageGroup: "early_elementary",
            audience: "kids", sourceDataset: "taxonomy-test/class_2", tags: ["addition"]);

        var response = await _http.GetAsync("/api/v1/questions/set?gradeBand=k_2&ageGroup=early_elementary&subject=k12&topic=math&dataset=taxonomy-test/class_2&tags=addition&count=5");
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<QuestionSetDto>(TestJson.Default);
        payload!.Questions.Should().ContainSingle(q => q.Text == "Taxonomy grade-band question?");
    }

    [Fact]
    public async Task Mixed_ReturnsAnswerSafeBalancedQuestions()
    {
        await SeedQuestionAsync("Taxonomy mixed science?", "Science", QuestionDifficulty.Easy,
            canonicalCategory: "science", subject: "stem", sourceDataset: "taxonomy-test/mixed");
        await SeedQuestionAsync("Taxonomy mixed history?", "History", QuestionDifficulty.Hard,
            canonicalCategory: "history", subject: "humanities", sourceDataset: "taxonomy-test/mixed");

        var response = await _http.PostAsJsonAsync("/api/v1/questions/mixed", new MixedQuestionSetRequest(
            Count: 4,
            Categories: new[] { "science", "history" },
            Subjects: null,
            Topics: null,
            GradeBands: null,
            AgeGroups: null,
            Audiences: null,
            Datasets: null,
            Difficulties: null,
            Tags: null,
            BalanceCategories: true,
            BalanceDifficulties: true));
        response.EnsureSuccessStatusCode();

        var jsonText = await response.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(jsonText);
        var questions = json.RootElement.GetProperty("questions").EnumerateArray().ToArray();
        questions.Should().NotBeEmpty();
        questions.Select(q => q.TryGetProperty("correctOptionId", out var _)).Should().OnlyContain(exposed => !exposed);
        questions.Select(q => q.GetProperty("category").GetString()).Should().Contain(["Science", "History"]);
    }

    [Fact]
    public async Task Metadata_ReturnsTaxonomyFacetsAliasesDatasetsAndCounts()
    {
        await SeedQuestionAsync("Taxonomy metadata science?", "Science", QuestionDifficulty.Easy,
            canonicalCategory: "science", subject: "stem", topic: "biology", gradeBand: "middle_school",
            ageGroup: "teen", audience: "teen", sourceDataset: "taxonomy-test/metadata", tags: ["cells"]);

        var response = await _http.GetAsync("/api/v1/questions/metadata");
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<QuestionMetadataResponseDto>(TestJson.Default);
        payload!.TaxonomyCategories.Should().Contain(c => c.Key == "science" && c.Count > 0);
        payload.Subjects.Should().Contain(s => s.Key == "stem" && s.Count > 0);
        payload.Topics.Should().Contain(t => t.Key == "biology" && t.Count > 0);
        payload.GradeBands.Should().Contain(g => g.Key == "middle_school" && g.Count > 0);
        payload.AgeGroups.Should().Contain(a => a.Key == "teen" && a.Count > 0);
        payload.Datasets.Should().Contain(d => d.Key == "taxonomy-test/metadata" && d.Count > 0);
        payload.Aliases.Should().NotBeNull();
        var aliases = payload.Aliases!;
        aliases.Should().ContainKey("science");
        aliases.TryGetValue("science", out var scienceAliases).Should().BeTrue();
        scienceAliases.Should().Contain("natural_science");
    }

    private async Task SeedQuestionAsync(
        string text,
        string category,
        QuestionDifficulty difficulty,
        string canonicalCategory,
        string subject,
        string? topic = null,
        string? gradeBand = null,
        string? ageGroup = null,
        string? audience = null,
        string? sourceDataset = null,
        string[]? tags = null)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();

        var question = new Question(text, category, difficulty, "A", null);
        question.ReplaceOptions(new[]
        {
            new QuestionOption(question.Id, "A", "Correct"),
            new QuestionOption(question.Id, "B", "Incorrect")
        });
        question.ReplaceTags(tags ?? []);
        question.SetTaxonomy(
            canonicalCategory,
            category,
            subject,
            topic,
            null,
            gradeBand,
            ageGroup,
            audience,
            sourceDataset,
            text.Replace(' ', '_').ToLowerInvariant(),
            "multiple_choice",
            "text",
            "[]");
        question.SetStatus("Approved");

        db.Questions.Add(question);
        await db.SaveChangesAsync();
    }
}
