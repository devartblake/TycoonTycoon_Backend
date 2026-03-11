using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Tycoon.Backend.Api.Tests.TestHost;

namespace Tycoon.Backend.Api.Tests.Party;

public sealed class PartyNotFoundContractTests : IClassFixture<TycoonApiFactory>
{
    private readonly HttpClient _http;

    public PartyNotFoundContractTests(TycoonApiFactory factory)
    {
        _http = factory.CreateClient();
    }

    [Fact]
    public async Task PartyRoster_WithUnknownPartyId_ReturnsNotFoundEnvelope()
    {
        var resp = await _http.GetAsync($"/party/{Guid.NewGuid()}");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await resp.HasErrorCodeAsync("NOT_FOUND");
    }

    [Fact]
    public async Task PartyRoster_WithEmptyPartyId_ReturnsValidationEnvelope()
    {
        var resp = await _http.GetAsync("/party/00000000-0000-0000-0000-000000000000");

        resp.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        await resp.HasErrorCodeAsync("VALIDATION_ERROR");
    }

    [Fact]
    public async Task PartyInviteAccept_WithUnknownInviteId_ReturnsNotFoundEnvelope()
    {
        var resp = await _http.PostAsJsonAsync($"/party/invites/{Guid.NewGuid()}/accept", new { PlayerId = Guid.NewGuid() });

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await resp.HasErrorCodeAsync("NOT_FOUND");
    }

    [Fact]
    public async Task PartyInviteDecline_WithUnknownInviteId_ReturnsNotFoundEnvelope()
    {
        var resp = await _http.PostAsJsonAsync($"/party/invites/{Guid.NewGuid()}/decline", new { PlayerId = Guid.NewGuid() });

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await resp.HasErrorCodeAsync("NOT_FOUND");
    }
}
