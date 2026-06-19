using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Application.Avatars;
using Synaptix.Wallet.Services;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Backend.Infrastructure.Persistence;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.Tests.Avatars;

// ─── Shared helpers ───────────────────────────────────────────────────────────

file static class Helpers
{
    public static AppDb NewDb() =>
        new(new DbContextOptionsBuilder<AppDb>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options, dispatcher: null);

    public static PlayerTransactionService TxnService(AppDb db) =>
        new(db, new EconomyService(db));

    public static StoreItem AvatarItem(string sku = "hero-v1", int priceCoins = 500) => new()
    {
        Sku = sku,
        Name = "Hero Avatar",
        ItemType = "avatar",
        PriceCoins = priceCoins,
        IsActive = true,
        MediaKey = "avatars/hero-v1",
        ThumbnailUrl = "https://cdn.example.com/hero.png",
        IsFeatured = true,
        Version = "1.0.0"
    };

    // Seeds a wallet by applying a coin credit transaction through EconomyService.
    public static async Task SeedWallet(AppDb db, Guid playerId, int coins)
    {
        var svc = new EconomyService(db);
        await svc.ApplyAsync(
            new CreateEconomyTxnRequest(Guid.NewGuid(), playerId, "test-seed",
                new[] { new EconomyLineDto(CurrencyType.Coins, coins) }),
            CancellationToken.None);
    }

    // Seeds a completed avatar ownership record via PlayerTransaction.
    public static async Task SeedOwnership(AppDb db, Guid playerId, string sku)
    {
        var ptxn = new PlayerTransaction(Guid.NewGuid(), "store-purchase");
        ptxn.AddActor(playerId, PlayerTransactionActorRole.Buyer);
        ptxn.AddItemChange(sku, 1, ItemOperation.Grant);
        ptxn.MarkApplied();
        db.PlayerTransactions.Add(ptxn);
        await db.SaveChangesAsync();
    }
}

// ─── Fake presigned storage ───────────────────────────────────────────────────

file sealed class FakePresignedStorage : IObjectStorage, IPresignedStorage
{
    public string LastGetKey { get; private set; } = string.Empty;

    public Task<string> GetPresignedGetUrlAsync(string key, TimeSpan expiry, CancellationToken ct = default)
    {
        LastGetKey = key;
        return Task.FromResult($"https://fake-storage.example.com/{key}?token=test");
    }

    public Task<string> GetPresignedPutUrlAsync(string key, string contentType, TimeSpan expiry, CancellationToken ct = default)
        => Task.FromResult($"https://fake-storage.example.com/put/{key}");

    public Task PutAsync(string key, Stream content, string contentType, long size = -1, CancellationToken ct = default)
        => Task.CompletedTask;

    public string GetPublicUrl(string key) => $"https://fake-storage.example.com/{key}";

    public Task<Stream?> GetAsync(string key, CancellationToken ct = default)
        => Task.FromResult<Stream?>(null);
}

// ─── GetAvatarCatalog ─────────────────────────────────────────────────────────

public sealed class GetAvatarCatalogHandlerTests
{
    [Fact]
    public async Task Returns_Empty_WhenNoCatalogItems()
    {
        await using var db = Helpers.NewDb();
        var handler = new GetAvatarCatalogHandler(db);

        var result = await handler.Handle(new GetAvatarCatalog(null), CancellationToken.None);

        result.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Returns_OnlyActive_AvatarItems()
    {
        await using var db = Helpers.NewDb();
        db.StoreItems.AddRange(
            Helpers.AvatarItem("hero-v1"),
            new StoreItem { Sku = "hero-v2", ItemType = "avatar", IsActive = false, Name = "Inactive" },
            new StoreItem { Sku = "powerup:skip", ItemType = "powerup", IsActive = true, Name = "Skip" }
        );
        await db.SaveChangesAsync();
        var handler = new GetAvatarCatalogHandler(db);

        var result = await handler.Handle(new GetAvatarCatalog(null), CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items[0].Sku.Should().Be("hero-v1");
    }

    [Fact]
    public async Task Sets_Owned_False_WhenNoPlayerId()
    {
        await using var db = Helpers.NewDb();
        db.StoreItems.Add(Helpers.AvatarItem("hero-v1"));
        await db.SaveChangesAsync();
        var handler = new GetAvatarCatalogHandler(db);

        var result = await handler.Handle(new GetAvatarCatalog(null), CancellationToken.None);

        result.Items[0].Owned.Should().BeFalse();
    }

    [Fact]
    public async Task Sets_Owned_True_WhenPlayerHasPurchased()
    {
        await using var db = Helpers.NewDb();
        var playerId = Guid.NewGuid();
        db.StoreItems.Add(Helpers.AvatarItem("hero-v1"));
        await db.SaveChangesAsync();
        await Helpers.SeedOwnership(db, playerId, "hero-v1");
        var handler = new GetAvatarCatalogHandler(db);

        var result = await handler.Handle(new GetAvatarCatalog(playerId), CancellationToken.None);

        result.Items[0].Owned.Should().BeTrue();
    }

    [Fact]
    public async Task Sets_Owned_False_ForOtherPlayer()
    {
        await using var db = Helpers.NewDb();
        var ownerPlayerId = Guid.NewGuid();
        var otherPlayerId = Guid.NewGuid();
        db.StoreItems.Add(Helpers.AvatarItem("hero-v1"));
        await db.SaveChangesAsync();
        await Helpers.SeedOwnership(db, ownerPlayerId, "hero-v1");
        var handler = new GetAvatarCatalogHandler(db);

        var result = await handler.Handle(new GetAvatarCatalog(otherPlayerId), CancellationToken.None);

        result.Items[0].Owned.Should().BeFalse();
    }

    [Fact]
    public async Task Maps_Fields_Correctly()
    {
        await using var db = Helpers.NewDb();
        db.StoreItems.Add(Helpers.AvatarItem("hero-v1", priceCoins: 750));
        await db.SaveChangesAsync();
        var handler = new GetAvatarCatalogHandler(db);

        var result = await handler.Handle(new GetAvatarCatalog(null), CancellationToken.None);

        var item = result.Items[0];
        item.Sku.Should().Be("hero-v1");
        item.Price.Should().Be(750);
        item.Currency.Should().Be("coins");
        item.Category.Should().Be("avatar");
        item.Type.Should().Be("cosmetic");
        item.ThumbnailUrl.Should().Be("https://cdn.example.com/hero.png");
        item.IsFeatured.Should().BeTrue();
        item.Version.Should().Be("1.0.0");
    }

    [Fact]
    public async Task Version_Defaults_To_1_0_0_WhenNull()
    {
        await using var db = Helpers.NewDb();
        db.StoreItems.Add(new StoreItem { Sku = "bare", ItemType = "avatar", IsActive = true, Name = "Bare", Version = null });
        await db.SaveChangesAsync();
        var handler = new GetAvatarCatalogHandler(db);

        var result = await handler.Handle(new GetAvatarCatalog(null), CancellationToken.None);

        result.Items[0].Version.Should().Be("1.0.0");
    }
}

// ─── PurchaseAvatar ───────────────────────────────────────────────────────────

public sealed class PurchaseAvatarHandlerTests
{
    [Fact]
    public async Task Returns_AvatarNotFound_WhenSkuMissing()
    {
        await using var db = Helpers.NewDb();
        var handler = new PurchaseAvatarHandler(db, Helpers.TxnService(db));

        var result = await handler.Handle(new PurchaseAvatar(Guid.NewGuid(), "missing-sku"), CancellationToken.None);

        result.ErrorCode.Should().Be("avatar_not_found");
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Returns_AvatarNotFound_WhenItemInactive()
    {
        await using var db = Helpers.NewDb();
        db.StoreItems.Add(new StoreItem { Sku = "hero-v1", ItemType = "avatar", IsActive = false, Name = "Inactive" });
        await db.SaveChangesAsync();
        var handler = new PurchaseAvatarHandler(db, Helpers.TxnService(db));

        var result = await handler.Handle(new PurchaseAvatar(Guid.NewGuid(), "hero-v1"), CancellationToken.None);

        result.ErrorCode.Should().Be("avatar_not_found");
    }

    [Fact]
    public async Task Returns_AlreadyOwned_WhenPlayerHasPurchased()
    {
        await using var db = Helpers.NewDb();
        var playerId = Guid.NewGuid();
        db.StoreItems.Add(Helpers.AvatarItem("hero-v1"));
        await db.SaveChangesAsync();
        await Helpers.SeedOwnership(db, playerId, "hero-v1");
        var handler = new PurchaseAvatarHandler(db, Helpers.TxnService(db));

        var result = await handler.Handle(new PurchaseAvatar(playerId, "hero-v1"), CancellationToken.None);

        result.ErrorCode.Should().Be("already_owned");
    }

    [Fact]
    public async Task Returns_InsufficientFunds_WhenWalletTooLow()
    {
        await using var db = Helpers.NewDb();
        var playerId = Guid.NewGuid();
        db.StoreItems.Add(Helpers.AvatarItem("hero-v1", priceCoins: 500));
        await db.SaveChangesAsync();
        await Helpers.SeedWallet(db, playerId, coins: 100); // 100 < 500
        var handler = new PurchaseAvatarHandler(db, Helpers.TxnService(db));

        var result = await handler.Handle(new PurchaseAvatar(playerId, "hero-v1"), CancellationToken.None);

        result.ErrorCode.Should().Be("insufficient_funds");
        result.ErrorDetails.Should().NotBeNull();
    }

    [Fact]
    public async Task Returns_Success_WithCorrectDto_WhenSufficientFunds()
    {
        await using var db = Helpers.NewDb();
        var playerId = Guid.NewGuid();
        db.StoreItems.Add(Helpers.AvatarItem("hero-v1", priceCoins: 200));
        await db.SaveChangesAsync();
        await Helpers.SeedWallet(db, playerId, coins: 1000);
        var handler = new PurchaseAvatarHandler(db, Helpers.TxnService(db));

        var result = await handler.Handle(new PurchaseAvatar(playerId, "hero-v1"), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.ErrorCode.Should().BeNull();
        result.Dto.Should().NotBeNull();
        result.Dto!.AvatarId.Should().Be("hero-v1");
        result.Dto.CoinsDeducted.Should().Be(200);
        result.Dto.NewBalance.Should().Be(800);
    }

    [Fact]
    public async Task Purchase_Reduces_Wallet_Balance()
    {
        await using var db = Helpers.NewDb();
        var playerId = Guid.NewGuid();
        db.StoreItems.Add(Helpers.AvatarItem("hero-v1", priceCoins: 300));
        await db.SaveChangesAsync();
        await Helpers.SeedWallet(db, playerId, coins: 500);
        var handler = new PurchaseAvatarHandler(db, Helpers.TxnService(db));

        await handler.Handle(new PurchaseAvatar(playerId, "hero-v1"), CancellationToken.None);

        var wallet = await db.PlayerWallets.SingleAsync(w => w.PlayerId == playerId);
        wallet.Coins.Should().Be(200);
    }
}

// ─── GetAvatarAsset ───────────────────────────────────────────────────────────

public sealed class GetAvatarAssetHandlerTests
{
    [Fact]
    public async Task Returns_NotFound_WhenSkuMissing()
    {
        await using var db = Helpers.NewDb();
        var handler = new GetAvatarAssetHandler(db, new FakePresignedStorage());

        var result = await handler.Handle(new GetAvatarAsset(Guid.NewGuid(), "missing"), CancellationToken.None);

        result.Found.Should().BeFalse();
        result.Owned.Should().BeFalse();
        result.Dto.Should().BeNull();
    }

    [Fact]
    public async Task Returns_NotOwned_WhenPlayerHasNotPurchased()
    {
        await using var db = Helpers.NewDb();
        db.StoreItems.Add(Helpers.AvatarItem("hero-v1"));
        await db.SaveChangesAsync();
        var handler = new GetAvatarAssetHandler(db, new FakePresignedStorage());

        var result = await handler.Handle(new GetAvatarAsset(Guid.NewGuid(), "hero-v1"), CancellationToken.None);

        result.Found.Should().BeTrue();
        result.Owned.Should().BeFalse();
        result.Dto.Should().BeNull();
    }

    [Fact]
    public async Task Returns_PresignedUrl_WhenOwned()
    {
        await using var db = Helpers.NewDb();
        var playerId = Guid.NewGuid();
        db.StoreItems.Add(Helpers.AvatarItem("hero-v1"));
        await db.SaveChangesAsync();
        await Helpers.SeedOwnership(db, playerId, "hero-v1");
        var storage = new FakePresignedStorage();
        var handler = new GetAvatarAssetHandler(db, storage);

        var result = await handler.Handle(new GetAvatarAsset(playerId, "hero-v1"), CancellationToken.None);

        result.Found.Should().BeTrue();
        result.Owned.Should().BeTrue();
        result.Dto.Should().NotBeNull();
        result.Dto!.PresignedUrl.Should().Contain("fake-storage.example.com");
        result.Dto.ArchiveFormat.Should().Be("zip");
        result.Dto.ContentType.Should().Be("application/zip");
    }

    [Fact]
    public async Task Uses_MediaKey_For_ArchivePath()
    {
        await using var db = Helpers.NewDb();
        var playerId = Guid.NewGuid();
        db.StoreItems.Add(new StoreItem
        {
            Sku = "hero-v1", ItemType = "avatar", IsActive = true,
            Name = "Hero", MediaKey = "avatars/hero-v1"
        });
        await db.SaveChangesAsync();
        await Helpers.SeedOwnership(db, playerId, "hero-v1");
        var storage = new FakePresignedStorage();
        var handler = new GetAvatarAssetHandler(db, storage);

        await handler.Handle(new GetAvatarAsset(playerId, "hero-v1"), CancellationToken.None);

        storage.LastGetKey.Should().Be("avatars/hero-v1.zip");
    }

    [Fact]
    public async Task Falls_Back_To_Sku_WhenMediaKeyNull()
    {
        await using var db = Helpers.NewDb();
        var playerId = Guid.NewGuid();
        db.StoreItems.Add(new StoreItem
        {
            Sku = "hero-v1", ItemType = "avatar", IsActive = true,
            Name = "Hero", MediaKey = null
        });
        await db.SaveChangesAsync();
        await Helpers.SeedOwnership(db, playerId, "hero-v1");
        var storage = new FakePresignedStorage();
        var handler = new GetAvatarAssetHandler(db, storage);

        await handler.Handle(new GetAvatarAsset(playerId, "hero-v1"), CancellationToken.None);

        storage.LastGetKey.Should().Be("avatars/hero-v1.zip");
    }

    [Fact]
    public async Task ExpiresAt_Is_15_Minutes_From_Now()
    {
        await using var db = Helpers.NewDb();
        var playerId = Guid.NewGuid();
        db.StoreItems.Add(Helpers.AvatarItem("hero-v1"));
        await db.SaveChangesAsync();
        await Helpers.SeedOwnership(db, playerId, "hero-v1");
        var handler = new GetAvatarAssetHandler(db, new FakePresignedStorage());

        var before = DateTimeOffset.UtcNow;
        var result = await handler.Handle(new GetAvatarAsset(playerId, "hero-v1"), CancellationToken.None);

        result.Dto!.ExpiresAt.Should().BeCloseTo(before.AddMinutes(15), TimeSpan.FromSeconds(5));
    }
}
