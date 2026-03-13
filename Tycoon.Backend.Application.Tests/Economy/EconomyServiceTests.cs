using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Economy;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Backend.Infrastructure.Persistence;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Tests.Economy;

public sealed class EconomyServiceTests
{
    private static AppDb NewDb()
    {
        var opts = new DbContextOptionsBuilder<AppDb>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDb(opts, dispatcher: null);
    }

    private static CreateEconomyTxnRequest XpRequest(Guid eventId, Guid playerId, int xp) =>
        new(eventId, playerId, "match-reward",
            new[] { new EconomyLineDto(CurrencyType.Xp, xp) });

    private static CreateEconomyTxnRequest CoinsRequest(Guid eventId, Guid playerId, int coins) =>
        new(eventId, playerId, "match-reward",
            new[] { new EconomyLineDto(CurrencyType.Coins, coins) });

    // ─── ApplyAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task Apply_Returns_Applied_ForValidTransaction()
    {
        await using var db = NewDb();
        var svc = new EconomyService(db);
        var playerId = Guid.NewGuid();

        var result = await svc.ApplyAsync(XpRequest(Guid.NewGuid(), playerId, 100), CancellationToken.None);

        result.Status.Should().Be(EconomyTxnStatus.Applied);
        result.PlayerId.Should().Be(playerId);
        result.BalanceXp.Should().Be(100);
    }

    [Fact]
    public async Task Apply_Creates_Wallet_WhenNoneExists()
    {
        await using var db = NewDb();
        var svc = new EconomyService(db);
        var playerId = Guid.NewGuid();

        await svc.ApplyAsync(XpRequest(Guid.NewGuid(), playerId, 50), CancellationToken.None);

        var wallet = await db.PlayerWallets.SingleAsync(x => x.PlayerId == playerId);
        wallet.Should().NotBeNull();
        wallet.Xp.Should().Be(50);
    }

    [Fact]
    public async Task Apply_Accumulates_Balance_AcrossMultipleTransactions()
    {
        await using var db = NewDb();
        var svc = new EconomyService(db);
        var playerId = Guid.NewGuid();

        await svc.ApplyAsync(XpRequest(Guid.NewGuid(), playerId, 100), CancellationToken.None);
        await svc.ApplyAsync(XpRequest(Guid.NewGuid(), playerId, 50), CancellationToken.None);

        var wallet = await db.PlayerWallets.SingleAsync(x => x.PlayerId == playerId);
        wallet.Xp.Should().Be(150);
    }

    [Fact]
    public async Task Apply_Returns_Duplicate_OnSecondCall_WithSameEventId()
    {
        await using var db = NewDb();
        var svc = new EconomyService(db);
        var playerId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        await svc.ApplyAsync(XpRequest(eventId, playerId, 100), CancellationToken.None);
        var second = await svc.ApplyAsync(XpRequest(eventId, playerId, 100), CancellationToken.None);

        second.Status.Should().Be(EconomyTxnStatus.Duplicate);

        // Wallet should not be double-counted
        var wallet = await db.PlayerWallets.SingleAsync(x => x.PlayerId == playerId);
        wallet.Xp.Should().Be(100);
    }

    [Fact]
    public async Task Apply_Returns_Invalid_WhenNoLines()
    {
        await using var db = NewDb();
        var svc = new EconomyService(db);
        var playerId = Guid.NewGuid();

        var req = new CreateEconomyTxnRequest(Guid.NewGuid(), playerId, "test",
            Array.Empty<EconomyLineDto>());

        var result = await svc.ApplyAsync(req, CancellationToken.None);

        result.Status.Should().Be(EconomyTxnStatus.Invalid);
    }

    [Fact]
    public async Task Apply_Returns_InsufficientFunds_WhenWalletCannotCover()
    {
        await using var db = NewDb();
        var svc = new EconomyService(db);
        var playerId = Guid.NewGuid();

        // Wallet starts at 0 coins; deducting 50 should fail
        var debit = new CreateEconomyTxnRequest(Guid.NewGuid(), playerId, "purchase",
            new[] { new EconomyLineDto(CurrencyType.Coins, -50) });

        var result = await svc.ApplyAsync(debit, CancellationToken.None);

        result.Status.Should().Be(EconomyTxnStatus.InsufficientFunds);
    }

    [Fact]
    public async Task Apply_DoesNotAllowNegativeBalance_AfterSuccessfulCredit_ThenDebit()
    {
        await using var db = NewDb();
        var svc = new EconomyService(db);
        var playerId = Guid.NewGuid();

        // Credit 30 coins
        await svc.ApplyAsync(CoinsRequest(Guid.NewGuid(), playerId, 30), CancellationToken.None);

        // Debit 50 coins (more than available)
        var debit = new CreateEconomyTxnRequest(Guid.NewGuid(), playerId, "purchase",
            new[] { new EconomyLineDto(CurrencyType.Coins, -50) });

        var result = await svc.ApplyAsync(debit, CancellationToken.None);

        result.Status.Should().Be(EconomyTxnStatus.InsufficientFunds);
        result.BalanceCoins.Should().Be(30, "balance should be unchanged");
    }

    [Fact]
    public async Task Apply_Persists_EconomyTransaction_ToDatabase()
    {
        await using var db = NewDb();
        var svc = new EconomyService(db);
        var playerId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        await svc.ApplyAsync(XpRequest(eventId, playerId, 100), CancellationToken.None);

        var txn = await db.EconomyTransactions.SingleAsync(x => x.EventId == eventId);
        txn.PlayerId.Should().Be(playerId);
        txn.Kind.Should().Be("match-reward");
    }

    // ─── GetHistoryAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetHistory_Returns_TransactionsForPlayer()
    {
        await using var db = NewDb();
        var svc = new EconomyService(db);
        var playerId = Guid.NewGuid();
        var otherId = Guid.NewGuid();

        await svc.ApplyAsync(XpRequest(Guid.NewGuid(), playerId, 100), CancellationToken.None);
        await svc.ApplyAsync(XpRequest(Guid.NewGuid(), otherId, 50), CancellationToken.None);

        var history = await svc.GetHistoryAsync(playerId, page: 1, pageSize: 10, CancellationToken.None);

        history.PlayerId.Should().Be(playerId);
        history.Total.Should().Be(1);
        history.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetHistory_Clamps_PageSize_Between1_And100()
    {
        await using var db = NewDb();
        var svc = new EconomyService(db);

        var history = await svc.GetHistoryAsync(Guid.NewGuid(), page: 0, pageSize: 0, CancellationToken.None);

        history.Page.Should().Be(1, "page < 1 should clamp to 1");
        history.PageSize.Should().Be(1, "pageSize < 1 should clamp to 1");
    }

    [Fact]
    public async Task GetHistory_Clamps_MaxPageSize_To100()
    {
        await using var db = NewDb();
        var svc = new EconomyService(db);

        var history = await svc.GetHistoryAsync(Guid.NewGuid(), page: 1, pageSize: 999, CancellationToken.None);

        history.PageSize.Should().Be(100);
    }

    [Fact]
    public async Task GetHistory_Returns_EmptyList_ForPlayerWithNoTransactions()
    {
        await using var db = NewDb();
        var svc = new EconomyService(db);

        var history = await svc.GetHistoryAsync(Guid.NewGuid(), page: 1, pageSize: 10, CancellationToken.None);

        history.Total.Should().Be(0);
        history.Items.Should().BeEmpty();
    }
}