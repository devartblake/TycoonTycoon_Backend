using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Backend.Infrastructure.Persistence;
using Tycoon.Shared.Contracts.Dtos;
using Xunit;

namespace Tycoon.Backend.Api.Tests.Referrals
{
    public sealed class ReferralFlowTests : IClassFixture<TycoonApiFactory>
    {
        private readonly TycoonApiFactory _factory;
        private readonly HttpClient _http;

        public ReferralFlowTests(TycoonApiFactory factory)
        {
            _factory = factory;
            _http = factory.CreateClient();
        }

        [Fact]
        public async Task Create_Then_Get_Then_Redeem_Is_Idempotent()
        {
            var ownerId = Guid.NewGuid();
            var redeemerId = Guid.NewGuid();

            // Seed player rows — RedeemReferralCode requires both to exist in Players table
            await using (var scope = _factory.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDb>();
                var owner = new Player($"refowner_{ownerId:N}");
                typeof(Player).GetProperty("Id")!.SetValue(owner, ownerId);
                var redeemer = new Player($"refredeemer_{redeemerId:N}");
                typeof(Player).GetProperty("Id")!.SetValue(redeemer, redeemerId);
                db.Players.AddRange(owner, redeemer);
                await db.SaveChangesAsync();
            }

            // Create code
            var create = await _http.PostAsJsonAsync("/referrals", new CreateReferralCodeRequest(ownerId));
            create.IsSuccessStatusCode.Should().BeTrue();

            var rc = await create.Content.ReadFromJsonAsync<ReferralCodeDto>();
            rc.Should().NotBeNull();
            rc!.Code.Should().NotBeNullOrWhiteSpace();

            // Get status
            var get = await _http.GetAsync($"/referrals/{rc.Code}");
            get.IsSuccessStatusCode.Should().BeTrue();

            // Redeem once
            var eventId = Guid.NewGuid();
            var redeem1 = await _http.PostAsJsonAsync($"/referrals/{rc.Code}/redeem", new RedeemReferralRequest(eventId, redeemerId));
            redeem1.IsSuccessStatusCode.Should().BeTrue();

            var r1 = await redeem1.Content.ReadFromJsonAsync<RedeemReferralResultDto>();
            r1!.Status.Should().Be("Redeemed");

            // Redeem duplicate (same eventId)
            var redeem2 = await _http.PostAsJsonAsync($"/referrals/{rc.Code}/redeem", new RedeemReferralRequest(eventId, redeemerId));
            redeem2.IsSuccessStatusCode.Should().BeTrue();

            var r2 = await redeem2.Content.ReadFromJsonAsync<RedeemReferralResultDto>();
            r2!.Status.Should().Be("Duplicate");
        }
    }
}
