using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Backend.Infrastructure.Persistence;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Tests.Questions;

public sealed class QuestionDiscoveryEndpointsTests : IClassFixture<TycoonApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly TycoonApiFactory _factory;
    private readonly HttpClient _http;

    public QuestionDiscoveryEndpointsTests(TycoonApiFactory factory)
    {
        _factory = factory;
        _http = factory.CreateClient();
    }

    [Fact]
    public async Task Categories_ReturnsApprovedCategoriesOnly_WithCounts()
    {
        await SeedQuestionAsync("Approved science", "Science", QuestionDifficulty.Easy, "Approved");
        await SeedQuestionAsync("Approved history", "History", QuestionDifficulty.Medium, "Approved");
        await SeedQuestionAsync("Draft science", "Science", QuestionDifficulty.Hard, "Draft");

        var response = await _http.GetFromJsonAsync<QuestionCategoriesResponseDto>("/questions/categories", JsonOptions);

        response.Should().NotBeNull();
        response!.Categories.Should().Contain(x => x.Key == "Science" && x.Count >= 1);
        response.Categories.Should().Contain(x => x.Key == "History" && x.Count >= 1);
    }

    [Fact]
    public async Task Metadata_ReturnsApprovedCategoryAndDifficultyCatalog()
    {
        await SeedQuestionAsync("Metadata science", "Science", QuestionDifficulty.Easy, "Approved");
        await SeedQuestionAsync("Metadata history", "History", QuestionDifficulty.Hard, "Approved");

        var response = await _http.GetFromJsonAsync<QuestionMetadataResponseDto>("/questions/metadata", JsonOptions);

        response.Should().NotBeNull();
        response!.Categories.Should().Contain(x => x.Key == "Science");
        response.Categories.Should().Contain(x => x.Key == "History");
        response.Difficulties.Should().Contain(QuestionDifficulty.Easy);
        response.Difficulties.Should().Contain(QuestionDifficulty.Hard);
        response.DefaultCount.Should().Be(10);
        response.MaxCount.Should().Be(50);
    }

    [Fact]
    public async Task PreviewSet_AppliesFilters_AndDoesNotExposeCorrectAnswers()
    {
        await SeedQuestionAsync("Science preview", "Science", QuestionDifficulty.Easy, "Approved");
        await SeedQuestionAsync("History preview", "History", QuestionDifficulty.Hard, "Approved");

        var response = await _http.PostAsJsonAsync("/questions/preview-set", new PreviewQuestionSetRequest(
            Categories: new[] { "Science" },
            Difficulties: new[] { QuestionDifficulty.Easy },
            Count: 10));
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<QuestionSetDto>(JsonOptions);
        payload.Should().NotBeNull();
        payload!.Questions.Should().NotBeEmpty();
        payload.Questions.Should().OnlyContain(q => q.Category == "Science" && q.Difficulty == QuestionDifficulty.Easy);
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
