using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Synaptix.Backend.Api.Tests.TestHost;
using Synaptix.Backend.Application.Questions;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Backend.Infrastructure.Persistence;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Tests.AdminQuestions;

public sealed class AdminQuestionTaxonomySuggestionTests : IClassFixture<AdminQuestionTaxonomySuggestionTests.Factory>
{
    private readonly Factory _factory;
    private readonly HttpClient _http;

    public AdminQuestionTaxonomySuggestionTests(Factory factory)
    {
        _factory = factory;
        _http = factory.CreateClient().WithAdminOpsKey();
    }

    [Fact]
    public async Task Suggest_StoresPendingSuggestion_AndApplyUpdatesQuestionTaxonomy()
    {
        _factory.Sidecar.Response = Suggestion("science", 0.96m, []);
        var questionId = await SeedQuestionAsync("Which force keeps planets in orbit?", "General");

        var suggest = await _http.PostAsync($"/admin/questions/{questionId}/taxonomy/suggest", null);
        suggest.EnsureSuccessStatusCode();
        var stored = await suggest.Content.ReadFromJsonAsync<QuestionTaxonomyStoredSuggestionDto>(TestJson.Default);

        stored!.Status.Should().Be("Pending");
        stored.QuestionId.Should().Be(questionId);
        stored.Suggestion.CanonicalCategory.Should().Be("science");

        var list = await _http.GetFromJsonAsync<IReadOnlyList<QuestionTaxonomyStoredSuggestionDto>>(
            $"/admin/questions/{questionId}/taxonomy/suggestions",
            TestJson.Default);
        list.Should().ContainSingle(s => s.Id == stored.Id);

        var apply = await _http.PostAsJsonAsync(
            $"/admin/questions/{questionId}/taxonomy/apply",
            new ApplyQuestionTaxonomySuggestionRequest(stored.Id, "admin-test", "accepted"),
            TestJson.Default);
        apply.EnsureSuccessStatusCode();

        var dto = await apply.Content.ReadFromJsonAsync<QuestionDto>(TestJson.Default);
        dto!.Taxonomy!.CanonicalCategory.Should().Be("science");
        dto.Taxonomy.Subject.Should().Be("stem");

        var after = await _http.GetFromJsonAsync<IReadOnlyList<QuestionTaxonomyStoredSuggestionDto>>(
            $"/admin/questions/{questionId}/taxonomy/suggestions",
            TestJson.Default);
        after!.Single(s => s.Id == stored.Id).Status.Should().Be("Applied");
    }

    [Fact]
    public async Task Import_WithOptionalSidecarOutage_FallsBackWithoutFailing()
    {
        _factory.Sidecar.Response = null;

        var response = await _http.PostAsJsonAsync("/admin/questions/import-taxonomy", ImportRequest(
            "sidecar-outage-001",
            "Ambiguous fallback import?",
            enrich: true,
            autoApply: false));

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<ImportQuestionsResultDto>(TestJson.Default);
        payload!.Received.Should().Be(1);
        payload.Created.Should().Be(1);
        payload.Failed.Should().Be(0);
    }

    [Fact]
    public async Task Import_WithLowConfidenceSuggestion_StoresPendingSuggestion()
    {
        _factory.Sidecar.Response = Suggestion("science", 0.60m, ["low confidence"]);

        var response = await _http.PostAsJsonAsync("/admin/questions/import-taxonomy", ImportRequest(
            "low-confidence-001",
            "What force keeps planets in orbit?",
            enrich: true,
            autoApply: false));
        response.EnsureSuccessStatusCode();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();
        db.QuestionTaxonomySuggestions.Should().ContainSingle(s => s.SourceQuestionId == "low-confidence-001" && s.Status == "Pending");
    }

    [Fact]
    public async Task Import_WithHighConfidenceAutoApply_UpdatesTaxonomyWithoutPendingSuggestion()
    {
        _factory.Sidecar.Response = Suggestion("science", 0.96m, []);

        var response = await _http.PostAsJsonAsync("/admin/questions/import-taxonomy", ImportRequest(
            "high-confidence-001",
            "What force keeps planets in orbit?",
            enrich: true,
            autoApply: true));
        response.EnsureSuccessStatusCode();

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();
        db.Questions.Should().ContainSingle(q => q.SourceQuestionId == "high-confidence-001" && q.CanonicalCategory == "science");
        db.QuestionTaxonomySuggestions.Should().NotContain(s => s.SourceQuestionId == "high-confidence-001");
    }

    [Fact]
    public async Task PublicQuestionEndpoints_DoNotCallSidecar()
    {
        _factory.Sidecar.Response = Suggestion("science", 0.96m, []);
        _factory.Sidecar.CallCount = 0;
        await SeedQuestionAsync("Gameplay no sidecar taxonomy question?", "Science", taxonomy: "science");

        (await _http.GetAsync("/questions/set?count=1")).EnsureSuccessStatusCode();
        (await _http.PostAsJsonAsync("/questions/mixed", new MixedQuestionSetRequest(Count: 1), TestJson.Default)).EnsureSuccessStatusCode();

        _factory.Sidecar.CallCount.Should().Be(0);
    }

    private async Task<Guid> SeedQuestionAsync(string text, string category, string? taxonomy = null)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();
        var question = new Question(text, category, QuestionDifficulty.Easy, "A", null);
        question.ReplaceOptions([
            new QuestionOption(question.Id, "A", "Gravity"),
            new QuestionOption(question.Id, "B", "Evaporation")
        ]);
        question.SetStatus("Approved");
        if (taxonomy is not null)
            question.SetTaxonomy(taxonomy, category, "stem", null, null, null, null, "general", null, null, "multiple_choice", "text");
        db.Questions.Add(question);
        await db.SaveChangesAsync();
        return question.Id;
    }

    private static TaxonomyImportQuestionsRequest ImportRequest(string id, string text, bool enrich, bool autoApply) =>
        new([
            new TaxonomyQuestionImportItemDto(
                Id: id,
                Question: null,
                Text: text,
                Category: null,
                Difficulty: "Easy",
                Answers: null,
                Options: [
                    new TaxonomyQuestionOptionImportDto("A", null, "Gravity", true),
                    new TaxonomyQuestionOptionImportDto("B", null, "Evaporation", false)
                ],
                CorrectAnswer: "Gravity",
                CorrectOptionId: null,
                Tags: ["space"],
                MediaKey: null,
                ImageUrl: null,
                VideoUrl: null,
                AudioUrl: null,
                Type: "multiple_choice",
                Taxonomy: new QuestionTaxonomyInputDto(SourceDataset: "test/sidecar"))
        ], Strict: true, EnrichWithSidecar: enrich, AutoApplyHighConfidenceSuggestions: autoApply);

    private static QuestionTaxonomySuggestionResponse Suggestion(string category, decimal confidence, IReadOnlyList<string> warnings) =>
        new(
            category,
            category == "science" ? "Science" : "General",
            category == "science" ? "stem" : "general",
            "physics",
            null,
            "middle_school",
            "teen",
            "teen",
            "multiple_choice",
            "text",
            ["sidecar", category],
            new Dictionary<string, decimal> { ["canonicalCategory"] = confidence },
            confidence,
            "fake-taxonomy-v1",
            warnings);

    public sealed class Factory : TycoonApiFactory
    {
        public FakeQuestionTaxonomySidecarClient Sidecar { get; } = new();

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<IQuestionTaxonomySidecarClient>();
                services.AddSingleton<IQuestionTaxonomySidecarClient>(Sidecar);
                services.PostConfigure<QuestionTaxonomySidecarOptions>(o =>
                {
                    o.QuestionTaxonomyEnabled = true;
                    o.QuestionTaxonomyAutoApplyEnabled = true;
                    o.QuestionTaxonomyAutoApplyMinConfidence = 0.85m;
                });
            });
        }
    }

    public sealed class FakeQuestionTaxonomySidecarClient : IQuestionTaxonomySidecarClient
    {
        public QuestionTaxonomySuggestionResponse? Response { get; set; } = Suggestion("science", 0.96m, []);
        public int CallCount { get; set; }

        public Task<QuestionTaxonomySuggestionResponse?> SuggestAsync(QuestionTaxonomySuggestionRequest request, CancellationToken ct = default)
        {
            CallCount++;
            return Task.FromResult(Response);
        }

        public Task<IReadOnlyList<QuestionTaxonomySuggestionResponse?>> SuggestBatchAsync(
            IReadOnlyList<QuestionTaxonomySuggestionRequest> requests,
            CancellationToken ct = default)
        {
            CallCount += requests.Count;
            return Task.FromResult<IReadOnlyList<QuestionTaxonomySuggestionResponse?>>(
                requests.Select(_ => Response).ToList());
        }
    }
}
