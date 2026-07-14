using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Synaptix.Backend.Api.Tests.TestHost;
using Synaptix.Shared.Contracts.Abstractions;
using Synaptix.Shared.Contracts.Dtos;
using Xunit;

namespace Synaptix.Backend.Api.Tests.AdminEconomy;

public sealed class AdminEconomyRollbackTests : IClassFixture<SynaptixApiFactory>
{
    private readonly SynaptixApiFactory _factory;
    private readonly HttpClient _admin;

    public AdminEconomyRollbackTests(SynaptixApiFactory factory)
    {
        _factory = factory;
        _admin = factory.CreateClient().WithAdminOpsKey();
    }

    [Fact]
    public async Task Rollback_ByEventId_Succeeds_AndSecondRollbackIsConflict()
    {
        var originalEventId = Guid.NewGuid();
        var playerId = Guid.NewGuid();

        // Arrange: create a normal economy transaction using the service directly
        using (var scope = _factory.Services.CreateScope())
        {
            var econ = scope.ServiceProvider.GetRequiredService<IEconomyService>();

            var created = await econ.ApplyAsync(new CreateEconomyTxnRequest(
                EventId: originalEventId,
                PlayerId: playerId,
                Kind: "award",
                Note: "test award",
                Lines: new[]
                {
                    new EconomyLineDto(CurrencyType.Coins, 50),
                }
            ), CancellationToken.None);

            created.EventId.Should().Be(originalEventId);
        }

        // Act 1: rollback succeeds
        var resp1 = await _admin.PostAsJsonAsync("/admin/economy/rollback",
            new AdminRollbackEconomyRequest(originalEventId, "confirmed fraud"));

        resp1.StatusCode.Should().Be(HttpStatusCode.OK);

        var rollback1 = await resp1.Content.ReadFromJsonAsync<EconomyTxnResultDto>(TestJson.Default);
        rollback1.Should().NotBeNull();
        rollback1!.EventId.Should().NotBe(Guid.Empty);

        // Act 2: rollback same original again => 409
        var resp2 = await _admin.PostAsJsonAsync("/admin/economy/rollback",
            new AdminRollbackEconomyRequest(originalEventId, "duplicate"));

        resp2.StatusCode.Should().Be(HttpStatusCode.Conflict);

        await resp2.HasErrorCodeAsync("CONFLICT");
    }

    [Fact]
    public async Task Rollback_UnknownEventId_Returns404()
    {
        var resp = await _admin.PostAsJsonAsync("/admin/economy/rollback",
            new AdminRollbackEconomyRequest(Guid.NewGuid(), "no such txn"));

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);

        await resp.HasErrorCodeAsync("NOT_FOUND");
    }
}
