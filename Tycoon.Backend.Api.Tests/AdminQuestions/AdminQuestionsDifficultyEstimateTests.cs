using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Tests.AdminQuestions;

public sealed class AdminQuestionsDifficultyEstimateTests : IClassFixture<TycoonApiFactory>
{
    private readonly HttpClient _http;

    public AdminQuestionsDifficultyEstimateTests(TycoonApiFactory factory)
    {
        _http = factory.CreateClient().WithAdminOpsKey();
    }

    [Fact]
    public async Task EstimateDifficulty_UsesHeuristicWhenSidecarDisabled()
    {
        var resp = await _http.PostAsJsonAsync("/admin/questions/estimate-difficulty",
            new QuestionDifficultyEstimateRequest("What is 2 + 2?"));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var dto = await resp.Content.ReadFromJsonAsync<QuestionDifficultyEstimateResponse>();
        dto.Should().NotBeNull();
        dto!.Source.Should().Be("heuristic");
        dto.Difficulty.Should().Be(QuestionDifficulty.Easy);
    }
}
