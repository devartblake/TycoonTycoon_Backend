using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Synaptix.Backend.Api.Tests.TestHost;
using Xunit;

namespace Synaptix.Backend.Api.Tests.Party;

public sealed class PartyEnqueueValidationContractTests : IClassFixture<TycoonApiFactory>
{
    private readonly HttpClient _http;

    public PartyEnqueueValidationContractTests(TycoonApiFactory factory)
    {
        _http = factory.CreateClient();
    }

    [Fact]
    public async Task PartyEnqueue_WithEmptyLeaderId_ReturnsValidationEnvelope()
    {
        var create = await _http.PostAsJsonAsync("/api/v1/party", new { LeaderPlayerId = Guid.NewGuid() });
        create.EnsureSuccessStatusCode();

        var roster = await create.Content.ReadFromJsonAsync<Synaptix.Shared.Contracts.Dtos.PartyRosterDto>();
        roster.Should().NotBeNull();

        var enqueue = await _http.PostAsJsonAsync($"/api/v1/party/{roster!.PartyId}/enqueue", new
        {
            LeaderPlayerId = Guid.Empty,
            Mode = "ranked",
            Tier = 1
        });

        enqueue.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        await enqueue.HasErrorCodeAsync("VALIDATION_ERROR");
    }
}
