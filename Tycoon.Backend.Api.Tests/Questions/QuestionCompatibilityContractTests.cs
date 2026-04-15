using System.Net;
using System.Net.Http.Json;
using System.Linq;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Backend.Infrastructure.Persistence;

namespace Tycoon.Backend.Api.Tests.Questions;

public sealed class QuestionCompatibilityContractTests : IClassFixture<TycoonApiFactory>
{
    private readonly TycoonApiFactory _factory;
    private readonly HttpClient _http;

    public QuestionCompatibilityContractTests(TycoonApiFactory factory)
    {
        _factory = factory;
        _http = factory.CreateClient();
    }

    [Fact]
    public async Task QuizDaily_ReturnsCompatibilityEnvelope()
    {
        await SeedApprovedQuestionAsync("Daily question", "Science", Tycoon.Shared.Contracts.Dtos.QuestionDifficulty.Easy);

        var response = await _http.GetAsync("/quiz/daily?count=1");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        body.Should().NotBeNull();
        body!.Should().ContainKey("items");
        body.Should().ContainKey("questions");
        body.Should().ContainKey("data");
        body.Should().ContainKey("meta");
    }

    [Fact]
    public async Task QuestionsMixed_FiltersByCategoriesAndDifficulties()
    {
        await SeedApprovedQuestionAsync("Science easy", "Science", Tycoon.Shared.Contracts.Dtos.QuestionDifficulty.Easy);
        await SeedApprovedQuestionAsync("History hard", "History", Tycoon.Shared.Contracts.Dtos.QuestionDifficulty.Hard);

        var response = await _http.GetAsync("/questions/mixed?categories=Science&difficulties=Easy&count=5");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var json = await System.Text.Json.JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var root = json.RootElement;
        root.TryGetProperty("items", out var items).Should().BeTrue();
        items.GetArrayLength().Should().BeGreaterThan(0);
        items.EnumerateArray().All(x => x.GetProperty("category").GetString() == "Science").Should().BeTrue();
    }

    [Fact]
    public async Task CheckAnswer_AcceptsSelectedAnswerText()
    {
        var questionId = await SeedApprovedQuestionAsync("Capital?", "Geography", Tycoon.Shared.Contracts.Dtos.QuestionDifficulty.Easy);

        var response = await _http.PostAsJsonAsync("/questions/check", new
        {
            QuestionId = questionId,
            SelectedAnswer = "Paris"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var json = await System.Text.Json.JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var root = json.RootElement;
        root.GetProperty("isCorrect").GetBoolean().Should().BeTrue();
        root.GetProperty("selectedOptionId").GetString().Should().Be("A");
        root.GetProperty("correctAnswer").GetString().Should().Be("Paris");
        root.GetProperty("source").GetString().Should().Be("backend");
    }

    [Fact]
    public async Task CheckBatch_AcceptsAnswerAliasAndReturnsCompatibilityCollections()
    {
        var questionId = await SeedApprovedQuestionAsync("Capital batch?", "Geography", Tycoon.Shared.Contracts.Dtos.QuestionDifficulty.Easy);

        var response = await _http.PostAsJsonAsync("/questions/check-batch", new
        {
            Answers = new object[]
            {
                new
                {
                    QuestionId = questionId,
                    Answer = "Paris"
                }
            }
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var json = await System.Text.Json.JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var root = json.RootElement;
        root.TryGetProperty("results", out var results).Should().BeTrue();
        root.TryGetProperty("items", out var items).Should().BeTrue();
        root.TryGetProperty("answers", out var answers).Should().BeTrue();
        results.GetArrayLength().Should().Be(1);
        items.GetArrayLength().Should().Be(1);
        answers.GetArrayLength().Should().Be(1);
        results[0].GetProperty("isCorrect").GetBoolean().Should().BeTrue();
        results[0].GetProperty("correctAnswer").GetString().Should().Be("Paris");
    }

    private async Task<Guid> SeedApprovedQuestionAsync(string text, string category, Tycoon.Shared.Contracts.Dtos.QuestionDifficulty difficulty)
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
            new QuestionOption(question.Id, "A", "Paris"),
            new QuestionOption(question.Id, "B", "London")
        });
        question.SetStatus("Approved");

        db.Questions.Add(question);
        await db.SaveChangesAsync();
        return question.Id;
    }
}
