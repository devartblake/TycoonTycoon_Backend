using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Backend.Application.Analytics.Models;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Backend.Infrastructure.Persistence;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Tests.Study;

public sealed class StudySetsEndpointsContractTests : IClassFixture<TycoonApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly TycoonApiFactory _factory;
    private readonly HttpClient _http;

    public StudySetsEndpointsContractTests(TycoonApiFactory factory)
    {
        _factory = factory;
        _http = factory.CreateClient();
    }

    [Fact]
    public async Task ListStudySets_ReturnsCategoryGeneratedSets_FromApprovedQuestions()
    {
        await SeedQuestionAsync("Science 1", "Science", QuestionDifficulty.Easy, "Approved");
        await SeedQuestionAsync("History 1", "History", QuestionDifficulty.Medium, "Approved");
        await SeedQuestionAsync("Hidden Draft", "Science", QuestionDifficulty.Hard, "Draft");

        var response = await _http.GetFromJsonAsync<StudySetsResponseDto>("/study-sets", JsonOptions);

        response.Should().NotBeNull();
        response!.Items.Should().Contain(x => x.Kind == StudySetKinds.Category && x.Category == "Science");
        response.Items.Should().Contain(x => x.Kind == StudySetKinds.Category && x.Category == "History");
    }

    [Fact]
    public async Task GetStudySet_ReturnsQuestionsWithCorrectAnswers_ForStudyContext()
    {
        await SeedQuestionAsync("Science 1", "Science", QuestionDifficulty.Easy, "Approved");
        await SeedQuestionAsync("Science 2", "Science", QuestionDifficulty.Medium, "Approved");

        var list = await _http.GetFromJsonAsync<StudySetsResponseDto>("/study-sets", JsonOptions);
        var scienceSet = list!.Items.Single(x => x.Category == "Science" && x.Kind == StudySetKinds.Category);

        var response = await _http.GetFromJsonAsync<StudySetDetailDto>(
            $"/study-sets/{Uri.EscapeDataString(scienceSet.Id)}?count=10", JsonOptions);

        response.Should().NotBeNull();
        response!.Kind.Should().Be(StudySetKinds.Category);
        response.Category.Should().Be("Science");
        response.Questions.Should().NotBeEmpty();
        response.Questions.Should().OnlyContain(q => q.Category == "Science");
        response.Questions.Should().OnlyContain(q => !string.IsNullOrWhiteSpace(q.CorrectOptionId));
    }

    [Fact]
    public async Task RecommendedStudySets_IncludesWeakArea_WhenPlayerAnalyticsExist()
    {
        var playerId = Guid.NewGuid();
        await SeedQuestionAsync("Science 1", "Science", QuestionDifficulty.Easy, "Approved");
        await SeedQuestionAsync("History 1", "History", QuestionDifficulty.Easy, "Approved");

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();
            db.QuestionAnsweredPlayerDailyRollups.Add(new QuestionAnsweredPlayerDailyRollup
            {
                Id = "rollup-1",
                Day = DateOnly.FromDateTime(DateTime.UtcNow.Date),
                PlayerId = playerId,
                Mode = "study",
                Category = "History",
                Difficulty = (int)QuestionDifficulty.Easy,
                TotalAnswers = 10,
                CorrectAnswers = 2,
                WrongAnswers = 8
            });
            await db.SaveChangesAsync();
        }

        var response = await _http.GetFromJsonAsync<RecommendedStudySetsResponseDto>(
            $"/study-sets/recommended?playerId={playerId}&count=5", JsonOptions);

        response.Should().NotBeNull();
        response!.Items.Should().NotBeEmpty();
        response.Items[0].Kind.Should().Be(StudySetKinds.WeakArea);
        response.Items[0].Category.Should().Be("History");
    }

    [Fact]
    public async Task GetStudySet_ReturnsNotFound_ForUnknownId()
    {
        var response = await _http.GetAsync("/study-sets/unknown");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await response.HasErrorCodeAsync("NOT_FOUND");
    }

    private async Task SeedQuestionAsync(string text, string category, QuestionDifficulty difficulty, string status)
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
        question.SetStatus(status);

        db.Questions.Add(question);
        await db.SaveChangesAsync();
    }
}
