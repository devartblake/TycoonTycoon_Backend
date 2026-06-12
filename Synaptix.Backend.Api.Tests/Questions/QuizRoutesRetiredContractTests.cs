using System.Net;
using FluentAssertions;
using Synaptix.Backend.Api.Tests.TestHost;

namespace Synaptix.Backend.Api.Tests.Questions;

public sealed class QuizRoutesRetiredContractTests : IClassFixture<TycoonApiFactory>
{
    private readonly HttpClient _http;

    public QuizRoutesRetiredContractTests(TycoonApiFactory factory)
    {
        _http = factory.CreateClient();
    }

    [Theory]
    [InlineData("/api/v1/quiz/play")]
    [InlineData("/api/v1/quiz/daily")]
    [InlineData("/api/v1/quiz/mixed")]
    [InlineData("/api/v1/quiz/categories")]
    [InlineData("/api/v1/quiz/stats")]
    public async Task LegacyQuizRoutes_AreNotMapped(string path)
    {
        var response = await _http.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
