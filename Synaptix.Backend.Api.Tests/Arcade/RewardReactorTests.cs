using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Synaptix.Backend.Application.Rewards;
using Synaptix.Backend.Api.Features.Arcade;
using Synaptix.Backend.Api.Tests.TestHost;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Backend.Infrastructure.Persistence;
using Synaptix.Shared.Contracts.Dtos;
using Xunit;

namespace Synaptix.Backend.Api.Tests.Arcade;

public sealed class RewardReactorTests : IClassFixture<TycoonApiFactory>
{
    private readonly TycoonApiFactory _factory;

    public RewardReactorTests(TycoonApiFactory factory)
    {
        _factory = factory;
    }

    // ── Route mapping smoke tests ──────────────────────────────────────────

    [Theory]
    [InlineData("/api/v1/arcade/reactor/spin")]
    [InlineData("/api/v1/arcade/reactor/claim")]
    [InlineData("/api/v1/arcade/reactor/chain")]
    [InlineData("/api/v1/arcade/spin/start")]
    public async Task PostRoutes_WithoutAuth_Return401(string route)
    {
        using var client = _factory.CreateClient();
        var resp = await client.PostAsJsonAsync(route, new { });
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetMyRewards_WithoutAuth_Returns401()
    {
        using var client = _factory.CreateClient();
        var resp = await client.GetAsync("/api/v1/users/me/rewards");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetReactorConfig_WithoutAuth_Returns401()
    {
        using var client = _factory.CreateClient();
        var resp = await client.GetAsync("/api/v1/arcade/reactor/config");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Full reactor spin → claim flow ─────────────────────────────────────

    [Fact]
    public async Task Spin_Returns_PendingClaim_With_ClaimToken()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var spinResp = await client.PostAsJsonAsync("/api/v1/arcade/reactor/spin",
            new ReactorSpinRequest(
                IdempotencyKey: $"test-spin-{Guid.NewGuid():N}",
                ReactorId: "daily-xp-reactor",
                Context: new ReactorSpinContext("daily_login", null, null)));

        spinResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var spin = await spinResp.Content.ReadFromJsonAsync<ReactorSpinResponse>(TestJson.Default);
        spin.Should().NotBeNull();
        spin!.SpinId.Should().StartWith("rr_");
        spin.Status.Should().Be("PendingClaim");
        spin.ClaimToken.Should().NotBeNullOrEmpty();
        spin.Animation.Should().NotBeNull();
        spin.RewardPreview.Lines.Should().NotBeEmpty();
        spin.ExpiresAtUtc.Should().BeAfter(DateTimeOffset.UtcNow);
        spin.EventId.Should().Be("double_coins_weekend_2026_05");
        spin.EventMultiplier.Should().Be(2.0);
        spin.SeasonKey.Should().Be("halloween_2026");
    }

    [Fact]
    public async Task Spin_IsDuplicate_SameIdempotencyKey_ReturnsSameSpinId()
    {
        var (client, _) = await CreateAuthenticatedClient();
        var key = $"idem-{Guid.NewGuid():N}";

        var r1 = await client.PostAsJsonAsync("/api/v1/arcade/reactor/spin",
            new ReactorSpinRequest(key, "daily-xp-reactor", null));
        r1.StatusCode.Should().Be(HttpStatusCode.OK);
        var s1 = await r1.Content.ReadFromJsonAsync<ReactorSpinResponse>(TestJson.Default);

        var r2 = await client.PostAsJsonAsync("/api/v1/arcade/reactor/spin",
            new ReactorSpinRequest(key, "daily-xp-reactor", null));
        r2.StatusCode.Should().Be(HttpStatusCode.OK);
        var s2 = await r2.Content.ReadFromJsonAsync<ReactorSpinResponse>(TestJson.Default);

        s1!.SpinId.Should().Be(s2!.SpinId);
    }

    [Fact]
    public async Task Spin_Then_Claim_AppliesRewardToWallet()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var spinResp = await client.PostAsJsonAsync("/api/v1/arcade/reactor/spin",
            new ReactorSpinRequest($"spin-{Guid.NewGuid():N}", "daily-xp-reactor", null));
        spinResp.EnsureSuccessStatusCode();
        var spin = await spinResp.Content.ReadFromJsonAsync<ReactorSpinResponse>(TestJson.Default);

        var claimResp = await client.PostAsJsonAsync("/api/v1/arcade/reactor/claim",
            new ReactorClaimRequest(
                SpinId: spin!.SpinId,
                IdempotencyKey: $"claim-{spin.SpinId}",
                ClaimToken: spin.ClaimToken));

        claimResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var claim = await claimResp.Content.ReadFromJsonAsync<ReactorClaimResponse>(TestJson.Default);

        claim.Should().NotBeNull();
        claim!.SpinId.Should().Be(spin.SpinId);
        claim.Status.Should().Be("Applied");
        claim.Duplicate.Should().BeFalse();
        claim.Lines.Should().NotBeEmpty();
        claim.Wallet.Should().NotBeNull();
        claim.Lines.Select(l => l.Type)
            .Should().BeEquivalentTo(spin.RewardPreview.Lines.Select(l => l.Type));
        if (claim.ChainedSpinId is not null)
            claim.ChainedSpinId.Should().StartWith("chain_");
    }

    [Fact]
    public async Task GetActiveEvents_ReturnsOk_WithExpectedShape()
    {
        using var client = _factory.CreateClient();

        var resp = await client.GetAsync("/api/v1/events/active");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await resp.Content.ReadAsStringAsync();
        body.Should().Contain("events");
    }

    [Fact]
    public async Task GetReactorConfig_WithAuth_ReturnsAssetSwitchingConfig()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var resp = await client.GetAsync("/api/v1/arcade/reactor/config");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var config = await resp.Content.ReadFromJsonAsync<ReactorConfigResponse>(TestJson.Default);
        config.Should().NotBeNull();
        config!.SeasonKey.Should().Be("halloween_2026");
        config.SymbolSet.Should().Be("halloween");
        config.AssetBaseUrl.Should().Be("https://cdn.example.com/reactor/halloween_2026/");
    }

    [Fact]
    public async Task ReactorChain_ActivatesTicket_And_IsIdempotent()
    {
        var (client, playerId) = await CreateAuthenticatedClient();
        var chainId = await SeedChainTicketAsync(Guid.Parse(playerId), expiresIn: TimeSpan.FromMinutes(3));

        var r1 = await client.PostAsJsonAsync("/api/v1/arcade/reactor/chain", new ReactorChainRequest(chainId));
        r1.StatusCode.Should().Be(HttpStatusCode.OK);
        var s1 = await r1.Content.ReadFromJsonAsync<ReactorSpinResponse>(TestJson.Default);

        var r2 = await client.PostAsJsonAsync("/api/v1/arcade/reactor/chain", new ReactorChainRequest(chainId));
        r2.StatusCode.Should().Be(HttpStatusCode.OK);
        var s2 = await r2.Content.ReadFromJsonAsync<ReactorSpinResponse>(TestJson.Default);

        s1.Should().NotBeNull();
        s2.Should().NotBeNull();
        s1!.SpinId.Should().Be(s2!.SpinId);
        s1.ClaimToken.Should().NotBeNullOrEmpty();
        s2.ClaimToken.Should().NotBeNullOrEmpty();
        s1.CooldownUntilUtc.Should().BeNull();
        s2.CooldownUntilUtc.Should().BeNull();
    }

    [Fact]
    public async Task ReactorChain_ExpiredTicket_Returns409()
    {
        var (client, playerId) = await CreateAuthenticatedClient();
        var chainId = await SeedChainTicketAsync(Guid.Parse(playerId), expiresIn: TimeSpan.FromMinutes(-1));

        var resp = await client.PostAsJsonAsync("/api/v1/arcade/reactor/chain", new ReactorChainRequest(chainId));
        resp.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var body = await resp.Content.ReadAsStringAsync();
        body.Should().Contain("REWARD_CHAIN_EXPIRED");
    }

    [Fact]
    public async Task ReactorChain_MissingTicket_Returns404()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var resp = await client.PostAsJsonAsync("/api/v1/arcade/reactor/chain",
            new ReactorChainRequest($"chain_missing_{Guid.NewGuid():N}"));

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await resp.Content.ReadAsStringAsync();
        body.Should().Contain("REWARD_CHAIN_NOT_FOUND");
    }

    [Fact]
    public async Task Claim_Duplicate_ReturnsDuplicate_True()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var spin = await SpinAsync(client);
        var claimKey = $"claim-{spin.SpinId}";

        var c1 = await client.PostAsJsonAsync("/api/v1/arcade/reactor/claim",
            new ReactorClaimRequest(spin.SpinId, claimKey, spin.ClaimToken));
        c1.EnsureSuccessStatusCode();

        var c2 = await client.PostAsJsonAsync("/api/v1/arcade/reactor/claim",
            new ReactorClaimRequest(spin.SpinId, claimKey, spin.ClaimToken));
        c2.StatusCode.Should().Be(HttpStatusCode.OK);
        var dup = await c2.Content.ReadFromJsonAsync<ReactorClaimResponse>(TestJson.Default);
        dup!.Duplicate.Should().BeTrue();
    }

    [Fact]
    public async Task Second_Spin_Without_Claim_Returns_CooldownActive()
    {
        var (client, _) = await CreateAuthenticatedClient();

        // First spin and claim (required to start cooldown via RewardPolicyService)
        var spin1 = await SpinAsync(client);
        var claim1 = await client.PostAsJsonAsync("/api/v1/arcade/reactor/claim",
            new ReactorClaimRequest(spin1.SpinId, $"claim-{spin1.SpinId}", spin1.ClaimToken));
        claim1.EnsureSuccessStatusCode();

        // Immediately try a second spin — should hit daily cap (1/day for reactor)
        var r2 = await client.PostAsJsonAsync("/api/v1/arcade/reactor/spin",
            new ReactorSpinRequest($"spin2-{Guid.NewGuid():N}", "daily-xp-reactor", null));

        r2.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await r2.Content.ReadAsStringAsync();
        (body.Contains("REWARD_DAILY_LIMIT_REACHED") || body.Contains("REWARD_COOLDOWN_ACTIVE"))
            .Should().BeTrue("second spin should be rejected by policy");
    }

    [Fact]
    public async Task Claim_WithInvalidToken_Returns403()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var spin = await SpinAsync(client);
        var claimResp = await client.PostAsJsonAsync("/api/v1/arcade/reactor/claim",
            new ReactorClaimRequest(spin.SpinId, $"claim-{spin.SpinId}", "invalid-token-value"));

        claimResp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Claim_WhenPendingExpired_Returns409()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var spin = await SpinAsync(client);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();
            var session = db.RewardSessions.First(s => s.SpinId == spin.SpinId);
            session.MarkExpired();
            await db.SaveChangesAsync();
        }

        var claimResp = await client.PostAsJsonAsync("/api/v1/arcade/reactor/claim",
            new ReactorClaimRequest(spin.SpinId, $"claim-expired-{spin.SpinId}", spin.ClaimToken));

        claimResp.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await claimResp.Content.ReadAsStringAsync();
        body.Should().Contain("REWARD_PENDING_EXPIRED");
    }

    [Fact]
    public async Task Claim_WithWrongSpinId_Returns404()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var claimResp = await client.PostAsJsonAsync("/api/v1/arcade/reactor/claim",
            new ReactorClaimRequest("rr_nonexistent", "claim-nonexistent", "some-token"));

        claimResp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetMyRewards_WithAuth_ReturnsEmptyOrPending()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var r = await client.GetAsync("/api/v1/users/me/rewards");
        r.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await r.Content.ReadAsStringAsync();
        body.Should().Contain("pending");
        body.Should().Contain("recentClaims");
    }

    [Fact]
    public async Task GetMyRewards_ShowsPendingAfterSpin()
    {
        var (client, _) = await CreateAuthenticatedClient();

        await SpinAsync(client);

        var r = await client.GetAsync("/api/v1/users/me/rewards");
        r.EnsureSuccessStatusCode();
        var body = await r.Content.ReadAsStringAsync();
        body.Should().Contain("rr_");
    }

    // ── Arcade Spin /start endpoint ────────────────────────────────────────

    [Fact]
    public async Task ArcadeSpinStart_Returns_PendingClaim()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var resp = await client.PostAsJsonAsync("/api/v1/arcade/spin/start",
            new ArcadeSpinStartRequest($"arcade-{Guid.NewGuid():N}"));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var spin = await resp.Content.ReadFromJsonAsync<ArcadeSpinStartResponse>(TestJson.Default);
        spin.Should().NotBeNull();
        spin!.SpinId.Should().StartWith("spin_");
        spin.Status.Should().Be("PendingClaim");
        spin.ClaimToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ArcadeSpinClaim_NewContract_UsesSpinIdClaimTokenAndIdempotency()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var start = await client.PostAsJsonAsync("/api/v1/arcade/spin/start",
            new ArcadeSpinStartRequest($"arcade-{Guid.NewGuid():N}"));
        start.EnsureSuccessStatusCode();
        var started = await start.Content.ReadFromJsonAsync<ArcadeSpinStartResponse>(TestJson.Default);
        started.Should().NotBeNull();

        var claim = await client.PostAsJsonAsync("/api/v1/arcade/spin/claim",
            new SpinClaimRequest(
                PlayerId: null,
                SegmentId: null,
                SpinId: started!.SpinId,
                ClaimToken: started.ClaimToken,
                IdempotencyKey: $"claim-{started.SpinId}"));

        claim.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await claim.Content.ReadFromJsonAsync<SpinClaimResponse>(TestJson.Default);
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.NewBalance.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task ArcadeSpinClaim_LegacyContract_RemainsSupported()
    {
        var (client, _) = await CreateAuthenticatedClient();

        var spinId = $"legacy-spin-{Guid.NewGuid():N}";
        var legacyClaim = await client.PostAsJsonAsync("/api/v1/arcade/spin/claim",
            new SpinClaimRequest(
                PlayerId: null,
                SegmentId: "gold_chest",
                SpinId: spinId));

        legacyClaim.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await legacyClaim.Content.ReadFromJsonAsync<SpinClaimResponse>(TestJson.Default);
        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.CoinsGranted.Should().Be(250);
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private async Task<(HttpClient Client, string PlayerId)> CreateAuthenticatedClient()
    {
        var client = _factory.CreateClient();
        var uniqueId = Guid.NewGuid().ToString("N")[..12];

        var signupResp = await client.PostAsJsonAsync("/api/v1/auth/signup", new SignupRequest(
            Email: $"reactor-{uniqueId}@example.com",
            Password: "Passw0rd!",
            DeviceId: "test-device",
            Username: $"reactor_{uniqueId}"));

        signupResp.EnsureSuccessStatusCode();
        var signup = await signupResp.Content.ReadFromJsonAsync<SignupResponse>();
        signup.Should().NotBeNull();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", signup!.AccessToken);

        return (client, signup.UserId.ToString());
    }

    private async Task<ReactorSpinResponse> SpinAsync(HttpClient client)
    {
        var spinResp = await client.PostAsJsonAsync("/api/v1/arcade/reactor/spin",
            new ReactorSpinRequest($"spin-{Guid.NewGuid():N}", "daily-xp-reactor", null));
        spinResp.EnsureSuccessStatusCode();
        var spin = await spinResp.Content.ReadFromJsonAsync<ReactorSpinResponse>(TestJson.Default);
        spin.Should().NotBeNull();
        return spin!;
    }

    private async Task<string> SeedChainTicketAsync(Guid playerId, TimeSpan expiresIn)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();

        var linesJson = JsonSerializer.Serialize(new List<RewardLine>
        {
            new("coins", 100)
        });

        var animationJson = JsonSerializer.Serialize(
            new RewardAnimationHint("three_reel_reactor", ["syncoins", "syncoins", "syncoins"], [0, 1, 2], "rare", "high"));

        var ticket = RewardChainTicket.Create(
            playerId,
            sourceSpinId: $"rr_src_{Guid.NewGuid():N}",
            rewardId: "chain_bonus",
            rewardLinesJson: linesJson,
            animationJson: animationJson,
            expiresAtUtc: DateTimeOffset.UtcNow + expiresIn);

        db.RewardChainTickets.Add(ticket);
        await db.SaveChangesAsync();

        return ticket.ChainedSpinId;
    }
}
