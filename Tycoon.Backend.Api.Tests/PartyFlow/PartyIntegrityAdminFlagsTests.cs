using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Backend.Infrastructure.Persistence;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Shared.Contracts.Dtos;
using Xunit;
using PartyEntity = Tycoon.Backend.Domain.Entities.Party;
using MatchEntity = Tycoon.Backend.Domain.Entities.Match;

namespace Tycoon.Backend.Api.Tests.Party;

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
                new MatchParticipantResultDto(
                    PlayerId: leaderId,
                    Score: 100,
                    Correct: 10,
                    Wrong: 0,
                    AvgAnswerTimeMs: 1200
                )
            }
        );

        var resp = await _http.PostAsJsonAsync(SubmitRoute, req);
        resp.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();

        var flags = await db.AntiCheatFlags.AsNoTracking()
            .Where(f => f.MatchId == matchId && f.RuleKey == "party-member-missing-from-submit")
            .ToListAsync();

        flags.Should().NotBeEmpty();
        flags[0].EvidenceJson.Should().NotBeNullOrWhiteSpace();
        flags[0].EvidenceJson!.Should().Contain(partyId.ToString());
        flags[0].Message.Should().Contain("missing");
    }

    [Fact]
    public async Task AdminPartyFlags_ReturnsPartyIdParsedFromEvidence()
    {
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

        var admin = _factory.CreateClient();
        admin.DefaultRequestHeaders.Add("X-Admin-Ops-Key", "test-admin-ops-key"); // align to your factory config

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

        // ✅ Party (disambiguated)
        var party = new PartyEntity(leaderId);

        // Force Party.Id (private setter) via reflection
        SetEntityId(party, partyId);

        db.Parties.Add(party);

        // Members
        db.PartyMembers.Add(new PartyMember(partyId, leaderId, PartyRole.Leader));
        db.PartyMembers.Add(new PartyMember(partyId, mateId, PartyRole.Member));

        // ✅ Match ctor only supports (hostPlayerId, mode)
        var match = new MatchEntity(hostPlayerId: leaderId, mode: "ranked");
        SetEntityId(match, matchId);
        db.Matches.Add(match);

        // Link
        db.PartyMatchLinks.Add(new PartyMatchLink(partyId, matchId));

        // Snapshot
        db.PartyMatchMembers.Add(new PartyMatchMember(partyId, matchId, leaderId, "Leader"));
        db.PartyMatchMembers.Add(new PartyMatchMember(partyId, matchId, mateId, "Member"));

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Robust reflection setter for EF-style entities where Id is on a base type.
    /// </summary>
    private static void SetEntityId(object entity, Guid id)
    {
        var t = entity.GetType();

        // try Id on the concrete type
        var p = t.GetProperty("Id", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
        if (p is not null && p.PropertyType == typeof(Guid))
        {
            p.SetValue(entity, id);
            return;
        }

        // try Id on base types
        var bt = t.BaseType;
        while (bt is not null)
        {
            var bp = bt.GetProperty("Id", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (bp is not null && bp.PropertyType == typeof(Guid))
            {
                bp.SetValue(entity, id);
                return;
            }
            bt = bt.BaseType;
        }

        throw new InvalidOperationException($"Could not set Id via reflection for entity type {t.FullName}.");
    }
}
