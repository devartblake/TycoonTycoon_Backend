using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Backend.Infrastructure.Persistence;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Shared.Contracts.Dtos;
using Xunit;
using PartyEntity = Tycoon.Backend.Domain.Entities.Party;
using MatchEntity = Tycoon.Backend.Domain.Entities.Match;

namespace Tycoon.Backend.Api.Tests.PartyFlow;

public sealed class PartyIntegrityAdminFlagsTests : IClassFixture<TycoonApiFactory>
{
    private readonly TycoonApiFactory _factory;
    private readonly HttpClient _http;

    // Adjust if your match submit route differs
    private const string SubmitRoute = "/matches/submit";

    public PartyIntegrityAdminFlagsTests(TycoonApiFactory factory)
    {
        _factory = factory;
        _http = factory.CreateClient();
    }

    [Fact]
    public async Task SubmitMatch_MissingPartyMember_WritesPartyMemberMissingFlag()
    {
        // Arrange: Create party + match + party link + membership snapshot
        var partyId = Guid.NewGuid();
        var leaderId = Guid.NewGuid();
        var mateId = Guid.NewGuid();
        var matchId = Guid.NewGuid();

        await SeedPartyMatchAsync(partyId, leaderId, mateId, matchId);

        var req = new SubmitMatchRequest(
            EventId: Guid.NewGuid(),
            MatchId: matchId,
            Mode: "ranked",
            Category: "general",
            QuestionCount: 10,
            StartedAtUtc: DateTimeOffset.UtcNow.AddMinutes(-2),
            EndedAtUtc: DateTimeOffset.UtcNow,
            Status: MatchStatus.Completed,
            Participants: new[]
            {
                // Only leader submits; mate is missing
                new MatchParticipantResultDto(
                    PlayerId: leaderId,
                    Score: 100,
                    Correct: 10,
                    Wrong: 0,
                    AvgAnswerTimeMs: 1200
                )
            }
        );

        // Act
        var resp = await _http.PostAsJsonAsync(SubmitRoute, req);
        resp.EnsureSuccessStatusCode();

        // Assert: flag exists in DB
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();

        var flags = db.AntiCheatFlags
            .Where(f => f.MatchId == matchId && f.RuleKey == "party-member-missing-from-submit")
            .ToList();

        flags.Should().NotBeEmpty();

        flags[0].EvidenceJson.Should().NotBeNullOrWhiteSpace();
        flags[0].EvidenceJson!.Should().Contain(partyId.ToString());
        flags[0].Message.Should().Contain("missing");
    }

    [Fact]
    public async Task AdminPartyFlags_ReturnsPartyIdParsedFromEvidence()
    {
        // Arrange: seed a flag that contains evidenceJson.partyId
        var matchId = Guid.NewGuid();
        var partyId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();

            db.AntiCheatFlags.Add(new AntiCheatFlag(
                matchId: matchId,
                playerId: null,
                ruleKey: "party-member-missing-from-submit",
                severity: AntiCheatSeverity.Warning,
                action: AntiCheatAction.Warn,
                message: "Match submit missing party member(s).",
                evidenceJson: System.Text.Json.JsonSerializer.Serialize(new { partyId }),
                createdAtUtc: DateTimeOffset.UtcNow
            ));

            await db.SaveChangesAsync();
        }

        // Act: call admin endpoint
        var admin = _factory.CreateClient();
        admin.DefaultRequestHeaders.Add("X-Admin-Ops-Key", "test-admin-ops-key"); // adjust to your factory config

        var r = await admin.GetAsync("/admin/anti-cheat/party/flags?page=1&pageSize=50&sinceUtc=" +
                                     Uri.EscapeDataString(DateTimeOffset.UtcNow.AddDays(-1).ToString("O")));
        r.EnsureSuccessStatusCode();

        var payload = await r.Content.ReadFromJsonAsync<PartyAntiCheatFlagsResponseDto>();
        payload.Should().NotBeNull();
        payload!.Items.Should().NotBeEmpty();

        var item = payload.Items.First(x => x.MatchId == matchId);
        item.PartyId.Should().Be(partyId);
        item.RuleKey.Should().StartWith("party-");
    }

    private async Task SeedPartyMatchAsync(Guid partyId, Guid leaderId, Guid mateId, Guid matchId)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();

        // Party
        var party = new PartyEntity(leaderId);
        typeof(PartyEntity).GetProperty(nameof(PartyEntity.Id))!.SetValue(party, partyId);
        db.Parties.Add(party);

        // Members
        db.PartyMembers.Add(new PartyMember(partyId, leaderId, PartyRole.Leader));
        db.PartyMembers.Add(new PartyMember(partyId, mateId, PartyRole.Member));

        // Match (minimal)
        var match = new Match(hostPlayerId: leaderId, mode: "ranked");
        typeof(Match).GetProperty(nameof(Match.Id))!.SetValue(match, matchId);
        db.Matches.Add(match);

        // Link
        db.PartyMatchLinks.Add(new PartyMatchLink(partyId, matchId));

        // Snapshot (6J strict correctness)
        db.PartyMatchMembers.Add(new PartyMatchMember(partyId, matchId, leaderId, "Leader"));
        db.PartyMatchMembers.Add(new PartyMatchMember(partyId, matchId, mateId, "Member"));

        await db.SaveChangesAsync();
    }
}
