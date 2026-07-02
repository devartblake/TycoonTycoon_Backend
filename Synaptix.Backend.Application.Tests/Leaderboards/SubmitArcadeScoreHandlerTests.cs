using Xunit;
using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.Leaderboards;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Backend.Infrastructure.Persistence;

namespace Synaptix.Backend.Application.Tests.Leaderboards;

public class SubmitArcadeScoreHandlerTests
{
    private readonly AppDb _db;
    private readonly SubmitArcadeScoreHandler _handler;

    public SubmitArcadeScoreHandlerTests()
    {
        var options = new DbContextOptionsBuilder<AppDb>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new AppDb(options);
        _handler = new SubmitArcadeScoreHandler(_db);
    }

    [Fact]
    public async Task Handle_WithNonexistentPlayer_ReturnsFalse()
    {
        // Arrange
        var command = new SubmitArcadeScore(
            PlayerId: Guid.NewGuid(),
            GameId: "patternSprint",
            Difficulty: "normal",
            Score: 1000,
            DurationMs: 30000
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task Handle_WithExistingPlayer_CreatesNewEntry()
    {
        // Arrange
        var playerId = Guid.NewGuid();
        var player = new Player("test_player", "US");
        player.GetType().GetProperty("Id")!.SetValue(player, playerId);
        _db.Players.Add(player);
        await _db.SaveChangesAsync();

        var command = new SubmitArcadeScore(
            PlayerId: playerId,
            GameId: "patternSprint",
            Difficulty: "normal",
            Score: 1500,
            DurationMs: 30000
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result);

        var entry = await _db.ArcadeScores
            .FirstOrDefaultAsync(e =>
                e.PlayerId == playerId &&
                e.GameId == "patternSprint" &&
                e.Difficulty == "normal");

        Assert.NotNull(entry);
        Assert.Equal(1500, entry.Score);
        Assert.Equal(30000, entry.DurationMs);
    }

    [Fact]
    public async Task Handle_WithHigherScore_UpdatesEntry()
    {
        // Arrange
        var playerId = Guid.NewGuid();
        var player = new Player("test_player_2", "US");
        player.GetType().GetProperty("Id")!.SetValue(player, playerId);
        _db.Players.Add(player);

        var existingEntry = new ArcadeScoreEntry(
            playerId,
            "memoryFlip",
            "hard",
            1000,
            40000,
            DateTimeOffset.UtcNow.AddDays(-1)
        );
        _db.ArcadeScores.Add(existingEntry);
        await _db.SaveChangesAsync();

        var command = new SubmitArcadeScore(
            PlayerId: playerId,
            GameId: "memoryFlip",
            Difficulty: "hard",
            Score: 1500,
            DurationMs: 35000
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result);

        var updatedEntry = await _db.ArcadeScores
            .FirstOrDefaultAsync(e =>
                e.PlayerId == playerId &&
                e.GameId == "memoryFlip" &&
                e.Difficulty == "hard");

        Assert.NotNull(updatedEntry);
        Assert.Equal(1500, updatedEntry.Score);
        Assert.Equal(35000, updatedEntry.DurationMs);
    }

    [Fact]
    public async Task Handle_WithLowerScore_DoesNotUpdate()
    {
        // Arrange
        var playerId = Guid.NewGuid();
        var player = new Player("test_player_3", "US");
        player.GetType().GetProperty("Id")!.SetValue(player, playerId);
        _db.Players.Add(player);

        var existingEntry = new ArcadeScoreEntry(
            playerId,
            "quickMathRush",
            "easy",
            2000,
            25000,
            DateTimeOffset.UtcNow
        );
        _db.ArcadeScores.Add(existingEntry);
        await _db.SaveChangesAsync();

        var command = new SubmitArcadeScore(
            PlayerId: playerId,
            GameId: "quickMathRush",
            Difficulty: "easy",
            Score: 1500,
            DurationMs: 30000
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result);

        var entry = await _db.ArcadeScores
            .FirstOrDefaultAsync(e =>
                e.PlayerId == playerId &&
                e.GameId == "quickMathRush" &&
                e.Difficulty == "easy");

        Assert.NotNull(entry);
        Assert.Equal(2000, entry.Score); // Unchanged
        Assert.Equal(25000, entry.DurationMs); // Unchanged
    }

    [Fact]
    public async Task Handle_SameScoreDifferentDuration_UpdatesIfFaster()
    {
        // Arrange
        var playerId = Guid.NewGuid();
        var player = new Player("test_player_4", "US");
        player.GetType().GetProperty("Id")!.SetValue(player, playerId);
        _db.Players.Add(player);

        var existingEntry = new ArcadeScoreEntry(
            playerId,
            "patternSprint",
            "normal",
            1000,
            40000,
            DateTimeOffset.UtcNow
        );
        _db.ArcadeScores.Add(existingEntry);
        await _db.SaveChangesAsync();

        var command = new SubmitArcadeScore(
            PlayerId: playerId,
            GameId: "patternSprint",
            Difficulty: "normal",
            Score: 1000, // Same score
            DurationMs: 30000 // Faster time
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result); // Should update because duration is lower

        var updatedEntry = await _db.ArcadeScores
            .FirstOrDefaultAsync(e =>
                e.PlayerId == playerId &&
                e.GameId == "patternSprint" &&
                e.Difficulty == "normal");

        Assert.NotNull(updatedEntry);
        Assert.Equal(1000, updatedEntry.Score);
        Assert.Equal(30000, updatedEntry.DurationMs); // Updated to faster time
    }

    [Fact]
    public async Task Handle_MultipleGamesAndDifficulties()
    {
        // Arrange
        var playerId = Guid.NewGuid();
        var player = new Player("test_player_5", "US");
        player.GetType().GetProperty("Id")!.SetValue(player, playerId);
        _db.Players.Add(player);
        await _db.SaveChangesAsync();

        // Submit scores for different game/difficulty combinations
        var commands = new[]
        {
            new SubmitArcadeScore(playerId, "patternSprint", "easy", 500, 45000),
            new SubmitArcadeScore(playerId, "patternSprint", "normal", 1000, 35000),
            new SubmitArcadeScore(playerId, "memoryFlip", "hard", 1500, 30000),
        };

        // Act
        foreach (var command in commands)
        {
            await _handler.Handle(command, CancellationToken.None);
        }

        // Assert
        var entries = await _db.ArcadeScores
            .Where(e => e.PlayerId == playerId)
            .ToListAsync();

        Assert.Equal(3, entries.Count);

        var psEasy = entries.FirstOrDefault(e => e.GameId == "patternSprint" && e.Difficulty == "easy");
        var psNormal = entries.FirstOrDefault(e => e.GameId == "patternSprint" && e.Difficulty == "normal");
        var mfHard = entries.FirstOrDefault(e => e.GameId == "memoryFlip" && e.Difficulty == "hard");

        Assert.NotNull(psEasy);
        Assert.Equal(500, psEasy.Score);
        Assert.NotNull(psNormal);
        Assert.Equal(1000, psNormal.Score);
        Assert.NotNull(mfHard);
        Assert.Equal(1500, mfHard.Score);
    }
}
