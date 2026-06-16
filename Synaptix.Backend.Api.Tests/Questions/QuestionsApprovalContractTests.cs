using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Synaptix.Backend.Api.Tests.TestHost;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Backend.Infrastructure.Persistence;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Tests.Questions;

public sealed class QuestionsApprovalContractTests : IClassFixture<TycoonApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly TycoonApiFactory _factory;
    private readonly HttpClient _http;

    public QuestionsApprovalContractTests(TycoonApiFactory factory)
    {
        _factory = factory;
        _http = factory.CreateClient();
    }

    [Fact]
    public async Task QuestionSet_ReturnsOnlyApprovedQuestions()
    {
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();

            var approved = new Question(
                text: "Approved question?",
                category: "General",
                difficulty: QuestionDifficulty.Easy,
                correctOptionId: "A",
                mediaKey: null);
            approved.ReplaceOptions(new[]
            {
                new QuestionOption(approved.Id, "A", "Yes"),
                new QuestionOption(approved.Id, "B", "No")
            });
            approved.SetStatus("Approved");

            var draft = new Question(
                text: "Draft question?",
                category: "General",
                difficulty: QuestionDifficulty.Easy,
                correctOptionId: "A",
                mediaKey: null);
            draft.ReplaceOptions(new[]
            {
                new QuestionOption(draft.Id, "A", "Yes"),
                new QuestionOption(draft.Id, "B", "No")
            });
            draft.SetStatus("Draft");

            db.Questions.Add(approved);
            db.Questions.Add(draft);
            await db.SaveChangesAsync();
        }

        var response = await _http.GetFromJsonAsync<QuestionSetDto>("/api/v1/questions/set?count=20", JsonOptions);
        response.Should().NotBeNull();
        response!.Questions.Should().Contain(q => q.Text.Contains("Approved question"));
        response.Questions.Should().NotContain(q => q.Text.Contains("Draft question"));
    }
}
