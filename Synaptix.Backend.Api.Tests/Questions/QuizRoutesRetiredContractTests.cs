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
    [InlineData("/quiz/play")]
    [InlineData("/quiz/daily")]
    [InlineData("/quiz/mixed")]
    [InlineData("/quiz/categories")]
    [InlineData("/quiz/stats")]
    public async Task LegacyQuizRoutes_AreNotMapped(string path)
    {
        var response = await _http.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
