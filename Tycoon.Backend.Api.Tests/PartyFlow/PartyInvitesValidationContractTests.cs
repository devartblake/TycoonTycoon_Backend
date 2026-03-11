using System.Net;
using FluentAssertions;
using Tycoon.Backend.Api.Tests.TestHost;
using Xunit;

namespace Tycoon.Backend.Api.Tests.Party;

public sealed class PartyInvitesValidationContractTests : IClassFixture<TycoonApiFactory>
{
    private readonly HttpClient _http;

    public PartyInvitesValidationContractTests(TycoonApiFactory factory)
    {
        _http = factory.CreateClient();
    }

    [Fact]
    public async Task PartyInvites_WithEmptyPlayerId_ReturnsValidationEnvelope()
    {
        var resp = await _http.GetAsync("/party/invites?playerId=00000000-0000-0000-0000-000000000000&box=incoming&page=1&pageSize=25");

        resp.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        await resp.HasErrorCodeAsync("VALIDATION_ERROR");
    }
}
