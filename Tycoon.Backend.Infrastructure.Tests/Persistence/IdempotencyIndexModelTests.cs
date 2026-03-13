using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Backend.Infrastructure.Persistence;

namespace Tycoon.Backend.Infrastructure.Tests.Persistence;

/// <summary>
/// Verifies that the EF Core model has unique indexes configured on EventId columns
/// that are critical for idempotency across the system.
/// Note: InMemory does not enforce unique constraints at runtime; these tests
/// validate the model metadata so the constraints exist in real migrations.
/// </summary>
public sealed class IdempotencyIndexModelTests
{
    private static AppDb NewDb()
    {
        var opts = new DbContextOptionsBuilder<AppDb>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDb(opts, dispatcher: null);
    }

    [Fact]
    public void ProcessedGameplayEvent_Has_UniqueIndex_On_EventId()
    {
        using var db = NewDb();
        var entityType = db.Model.FindEntityType(typeof(ProcessedGameplayEvent));

        entityType.Should().NotBeNull();

        var uniqueOnEventId = entityType!.GetIndexes()
            .Any(ix => ix.IsUnique && ix.Properties.Any(p => p.Name == nameof(ProcessedGameplayEvent.EventId)));

        uniqueOnEventId.Should().BeTrue("ProcessedGameplayEvent requires a unique index on EventId for idempotency");
    }

    [Fact]
    public void EconomyTransaction_Has_UniqueIndex_On_EventId()
    {
        using var db = NewDb();
        var entityType = db.Model.FindEntityType(typeof(EconomyTransaction));

        entityType.Should().NotBeNull();

        var uniqueOnEventId = entityType!.GetIndexes()
            .Any(ix => ix.IsUnique && ix.Properties.Any(p => p.Name == nameof(EconomyTransaction.EventId)));

        uniqueOnEventId.Should().BeTrue("EconomyTransaction requires a unique index on EventId for idempotency");
    }

    [Fact]
    public void SeasonPointTransaction_Has_UniqueIndex_On_EventId()
    {
        using var db = NewDb();
        var entityType = db.Model.FindEntityType(typeof(SeasonPointTransaction));

        entityType.Should().NotBeNull();

        var uniqueOnEventId = entityType!.GetIndexes()
            .Any(ix => ix.IsUnique && ix.Properties.Any(p => p.Name == nameof(SeasonPointTransaction.EventId)));

        uniqueOnEventId.Should().BeTrue("SeasonPointTransaction requires a unique index on EventId for idempotency");
    }

    [Fact]
    public void ProcessedGameplayEvent_Has_PrimaryKey()
    {
        using var db = NewDb();
        var entityType = db.Model.FindEntityType(typeof(ProcessedGameplayEvent));

        entityType!.FindPrimaryKey().Should().NotBeNull();
        entityType.FindPrimaryKey()!.Properties.Should().ContainSingle(p => p.Name == nameof(ProcessedGameplayEvent.Id));
    }

    [Fact]
    public void EconomyTransaction_Has_Index_On_PlayerId()
    {
        using var db = NewDb();
        var entityType = db.Model.FindEntityType(typeof(EconomyTransaction));

        var indexOnPlayerId = entityType!.GetIndexes()
            .Any(ix => ix.Properties.Any(p => p.Name == nameof(EconomyTransaction.PlayerId)));

        indexOnPlayerId.Should().BeTrue("EconomyTransaction should be queryable by PlayerId for history lookups");
    }

    [Fact]
    public void SeasonPointTransaction_Has_CompositeIndex_On_SeasonId_And_PlayerId()
    {
        using var db = NewDb();
        var entityType = db.Model.FindEntityType(typeof(SeasonPointTransaction));

        var compositeIndex = entityType!.GetIndexes().Any(ix =>
            ix.Properties.Any(p => p.Name == nameof(SeasonPointTransaction.SeasonId)) &&
            ix.Properties.Any(p => p.Name == nameof(SeasonPointTransaction.PlayerId)));

        compositeIndex.Should().BeTrue("SeasonPointTransaction should have a composite index on (SeasonId, PlayerId) for leaderboard queries");
    }
}