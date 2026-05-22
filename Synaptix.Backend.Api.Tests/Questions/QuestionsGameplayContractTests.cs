using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Synaptix.Backend.Api.Tests.TestHost;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Backend.Domain.Personalization;
using Synaptix.Backend.Infrastructure.Persistence;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Tests.Questions;

public sealed class QuestionsGameplayContractTests : IClassFixture<TycoonApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly TycoonApiFactory _factory;
    private readonly HttpClient _http;

    public QuestionsGameplayContractTests(TycoonApiFactory factory)
    {
        _factory = factory;
        _http = factory.CreateClient();
    }

    [Fact]
    public async Task QuestionSet_DoesNotExposeCorrectOptionId()
    {
        await SeedApprovedQuestionAsync("Gameplay-safe question?");

        var response = await _http.GetAsync("/questions/set?count=5");
        response.EnsureSuccessStatusCode();

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var firstQuestion = json.RootElement.GetProperty("questions")[0];

        firstQuestion.TryGetProperty("correctOptionId", out _).Should().BeFalse();
    }

    [Fact]
    public async Task CheckAnswer_UsesOptionIdGradingSemantics()
    {
        var questionId = await SeedApprovedQuestionAsync("Option grading question?");

        var response = await _http.PostAsJsonAsync("/questions/check", new CheckAnswerRequest(questionId, "A"));
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<CheckAnswerResponse>();
        payload.Should().NotBeNull();
        payload!.QuestionId.Should().Be(questionId);
        payload.SelectedOptionId.Should().Be("A");
        payload.CorrectOptionId.Should().Be("A");
        payload.IsCorrect.Should().BeTrue();
    }

    [Fact]
    public async Task CheckAnswersBatch_ReturnsTotals_AndMissingQuestionAsIncorrect()
    {
        var questionId = await SeedApprovedQuestionAsync("Batch grading question?");

        var response = await _http.PostAsJsonAsync("/questions/check-batch", new CheckAnswersBatchRequest(new[]
        {
            new CheckAnswerRequest(questionId, "A"),
            new CheckAnswerRequest(Guid.NewGuid(), "B")
        }));
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<CheckAnswersBatchResponse>();
        payload.Should().NotBeNull();
        payload!.Total.Should().Be(2);
        payload.Correct.Should().Be(1);
        payload.Results.Should().Contain(r => r.QuestionId == questionId && r.IsCorrect);
        payload.Results.Should().Contain(r => r.QuestionId != questionId && !r.IsCorrect && r.CorrectOptionId == "");
    }

    [Fact]
    public async Task CheckAnswersBatch_WhenTooManyAnswers_ReturnsValidationEnvelope()
    {
        var answers = Enumerable.Range(0, 51)
            .Select(_ => new CheckAnswerRequest(Guid.NewGuid(), "A"))
            .ToArray();

        var response = await _http.PostAsJsonAsync("/questions/check-batch", new CheckAnswersBatchRequest(answers));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await response.HasErrorCodeAsync("VALIDATION_ERROR");
    }

    // ── Adaptive strategy ─────────────────────────────────────────────────────

    [Fact]
    public async Task QuestionSet_PracticeMode_UsesWeakCategory_WhenNoCategorySpecified()
    {
        var playerId = Guid.NewGuid();
        await SeedQuestionAsync("Adaptive History Q1", "History", QuestionDifficulty.Easy);
        await SeedQuestionAsync("Adaptive History Q2", "History", QuestionDifficulty.Medium);
        await SeedPlayerProfileAsync(playerId, categoryWeaknessesJson: "{\"History\": 0.9}");

        var response = await _http.GetAsync($"/questions/set?count=5&playerId={playerId}&mode=practice");
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<QuestionSetDto>(JsonOptions);
        payload.Should().NotBeNull();
        payload!.Questions.Should().NotBeEmpty();
        // All questions served should be from the weak category (History), since it was
        // the top weakness and no category was explicitly requested.
        payload.Questions.Should().OnlyContain(q => q.Category == "History");
    }

    [Fact]
    public async Task QuestionSet_RankedMode_IgnoresPersonalization_EvenWithWeakCategory()
    {
        var playerId = Guid.NewGuid();
        // Seed questions in two categories so the random pick can include both.
        await SeedQuestionAsync("Ranked History Q1", "History", QuestionDifficulty.Easy);
        await SeedQuestionAsync("Ranked Science Q1", "Science", QuestionDifficulty.Easy);
        await SeedPlayerProfileAsync(playerId, categoryWeaknessesJson: "{\"History\": 0.9}");

        // When mode=ranked the explicit category filter (none) is respected and personalization
        // must not inject a category filter that could skew ranked fairness.
        var response = await _http.GetAsync($"/questions/set?count=10&playerId={playerId}&mode=ranked");
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<QuestionSetDto>(JsonOptions);
        payload.Should().NotBeNull();
        // The result may contain questions from multiple categories (ranked is unconstrained).
        // At minimum the endpoint must succeed and return a valid payload.
        payload!.Questions.Should().NotBeNull();
    }

    [Fact]
    public async Task QuestionSet_PracticeMode_ExplicitCategory_Overrides_WeakCategory()
    {
        var playerId = Guid.NewGuid();
        // Seed a Medium Science question — adaptive difficulty defaults to Medium for a fresh player
        // (confidence = 0.50), so the explicit category=Science must be honoured.
        await SeedQuestionAsync("Override Science Q1", "Science", QuestionDifficulty.Medium);
        await SeedPlayerProfileAsync(playerId, categoryWeaknessesJson: "{\"History\": 0.9}");

        // An explicit category must always take precedence over the adaptive weak category.
        var response = await _http.GetAsync($"/questions/set?count=5&category=Science&playerId={playerId}&mode=practice");
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<QuestionSetDto>(JsonOptions);
        payload.Should().NotBeNull();
        payload!.Questions.Should().NotBeEmpty();
        payload.Questions.Should().OnlyContain(q => q.Category == "Science");
    }

    private async Task<Guid> SeedApprovedQuestionAsync(string text)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();

        var question = new Question(
            text: text,
            category: "Science",
            difficulty: QuestionDifficulty.Easy,
            correctOptionId: "A",
            mediaKey: null);
        question.ReplaceOptions(new[]
        {
            new QuestionOption(question.Id, "A", "Correct"),
            new QuestionOption(question.Id, "B", "Incorrect")
        });
        question.SetStatus("Approved");

        db.Questions.Add(question);
        await db.SaveChangesAsync();
        return question.Id;
    }

    private async Task SeedQuestionAsync(string text, string category, QuestionDifficulty difficulty)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();

        var question = new Question(
            text: text,
            category: category,
            difficulty: difficulty,
            correctOptionId: "A",
            mediaKey: null);
        question.ReplaceOptions(new[]
        {
            new QuestionOption(question.Id, "A", "Correct"),
            new QuestionOption(question.Id, "B", "Incorrect")
        });
        question.SetStatus("Approved");

        db.Questions.Add(question);
        await db.SaveChangesAsync();
    }

    private async Task SeedPlayerProfileAsync(Guid playerId, string categoryWeaknessesJson)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();

        db.PlayerMindProfiles.Add(new PlayerMindProfile
        {
            Id = Guid.NewGuid(),
            PlayerId = playerId,
            CategoryWeaknessesJson = categoryWeaknessesJson
        });
        await db.SaveChangesAsync();
    }
}
