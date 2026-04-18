using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Backend.Infrastructure.Persistence;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Tests.Questions;

public sealed class QuestionsGameplayContractTests : IClassFixture<TycoonApiFactory>
{
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
}
