using Xunit;
using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.Leaderboards;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Backend.Infrastructure.Persistence;

namespace Synaptix.Backend.Application.Tests.Leaderboards;

public class GetArcadeLeaderboardHandlerTests
{
    private readonly AppDb _db;
    private readonly GetArcadeLeaderboardHandler _handler;

    public GetArcadeLeaderboardHandlerTests()
    {
        var options = new DbContextOptionsBuilder<AppDb>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new AppDb(options);
        _handler = new GetArcadeLeaderboardHandler(_db);
    }

    [Fact]
    public async Task Handle_EmptyLeaderboard_ReturnsEmptyList()
    {
        // Arrange
        var query = new GetArcadeLeaderboard(
            GameId: "patternSprint",
            Difficulty: "normal",
            Page: 1,
            PageSize: 50
        );

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Empty(result.Items);
        Assert.Equal(0, result.Total);
        Assert.Equal(1, result.Page);
        Assert.Equal(50, result.PageSize);
    }

    [Fact]
    public async Task Handle_WithScores_ReturnsSortedByScoreThenDuration()
    {
        // Arrange
        await SetupTestScores();

        var query = new GetArcadeLeaderboard(
            GameId: "patternSprint",
            Difficulty: "normal",
            Page: 1,
            PageSize: 50
        );

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(3, result.Total);
        Assert.Equal(3, result.Items.Count);

        // Should be sorted by score descending, then duration ascending
        Assert.Equal(1500, result.Items[0].Score);
        Assert.Equal(25000, result.Items[0].DurationMs);
        Assert.Equal(1000, result.Items[1].Score);
        Assert.Equal(30000, result.Items[1].DurationMs);
        Assert.Equal(1000, result.Items[2].Score);
        Assert.Equal(40000, result.Items[2].DurationMs);
    }

    [Fact]
    public async Task Handle_WithScores_ReturnsCorrectRanks()
    {
        // Arrange
        await SetupTestScores();

        var query = new GetArcadeLeaderboard(
            GameId: "patternSprint",
            Difficulty: "normal",
            Page: 1,
            PageSize: 50
        );

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.Items[0].Rank);
        Assert.Equal(2, result.Items[1].Rank);
        Assert.Equal(3, result.Items[2].Rank);
    }

    [Fact]
    public async Task Handle_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        var playerIds = new List<Guid>();
        for (int i = 0; i < 25; i++)
        {
            var playerId = Guid.NewGuid();
            playerIds.Add(playerId);
            var player = new Player($"player_{i}", "US");
            player.GetType().GetProperty("Id")!.SetValue(player, playerId);
            _db.Players.Add(player);

            var entry = new ArcadeScoreEntry(
                playerId,
                "quickMathRush",
                "easy",
                5000 - (i * 100),
                25000,
                DateTimeOffset.UtcNow
            );
            _db.ArcadeScores.Add(entry);
        }
        await _db.SaveChangesAsync();

        // Act - Page 1
        var result1 = await _handler.Handle(
            new GetArcadeLeaderboard("quickMathRush", "easy", Page: 1, PageSize: 10),
            CancellationToken.None
        );

        // Act - Page 2
        var result2 = await _handler.Handle(
            new GetArcadeLeaderboard("quickMathRush", "easy", Page: 2, PageSize: 10),
            CancellationToken.None
        );

        // Act - Page 3
        var result3 = await _handler.Handle(
            new GetArcadeLeaderboard("quickMathRush", "easy", Page: 3, PageSize: 10),
            CancellationToken.None
        );

        // Assert
        Assert.Equal(25, result1.Total);
        Assert.Equal(10, result1.Items.Count);
        Assert.Equal(10, result2.Items.Count);
        Assert.Equal(5, result3.Items.Count);

        // Verify no overlap between pages
        Assert.Equal(1, result1.Items[0].Rank);
        Assert.Equal(10, result1.Items[9].Rank);
        Assert.Equal(11, result2.Items[0].Rank);
        Assert.Equal(20, result2.Items[9].Rank);
        Assert.Equal(21, result3.Items[0].Rank);
        Assert.Equal(25, result3.Items[4].Rank);
    }

    [Fact]
    public async Task Handle_WithAuthenticatedPlayer_ReturnsPlayerRank()
    {
        // Arrange
        var targetPlayerId = Guid.NewGuid();
        await SetupTestScoresWithTargetPlayer(targetPlayerId);

        var query = new GetArcadeLeaderboard(
            GameId: "patternSprint",
            Difficulty: "normal",
            Page: 1,
            PageSize: 50,
            PlayerId: targetPlayerId
        );

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result.MyRank);
        Assert.NotNull(result.MyScore);
        Assert.Equal(2, result.MyRank); // Second place
        Assert.Equal(1000, result.MyScore);
    }

    [Fact]
    public async Task Handle_WithAuthenticatedPlayerNotOnPage_ComputesRank()
    {
        // Arrange
        var targetPlayerId = Guid.NewGuid();
        var targetPlayer = new Player("target_player", "US");
        targetPlayer.GetType().GetProperty("Id")!.SetValue(targetPlayer, targetPlayerId);
        _db.Players.Add(targetPlayer);

        // Add 15 higher-scoring players
        for (int i = 0; i < 15; i++)
        {
            var playerId = Guid.NewGuid();
            var player = new Player($"high_player_{i}", "US");
            player.GetType().GetProperty("Id")!.SetValue(player, playerId);
            _db.Players.Add(player);

            var entry = new ArcadeScoreEntry(
                playerId,
                "memoryFlip",
                "hard",
                5000 - (i * 100),
                30000,
                DateTimeOffset.UtcNow
            );
            _db.ArcadeScores.Add(entry);
        }

        // Add target player with lower score
        var targetEntry = new ArcadeScoreEntry(
            targetPlayerId,
            "memoryFlip",
            "hard",
            1000,
            30000,
            DateTimeOffset.UtcNow
        );
        _db.ArcadeScores.Add(targetEntry);

        await _db.SaveChangesAsync();

        var query = new GetArcadeLeaderboard(
            GameId: "memoryFlip",
            Difficulty: "hard",
            Page: 1,
            PageSize: 10,
            PlayerId: targetPlayerId
        );

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result.MyRank);
        Assert.Equal(16, result.MyRank); // Rank 16 (after 15 higher scores)
        Assert.Equal(1000, result.MyScore);
        // Player should not be in the first 10 items
        Assert.DoesNotContain(result.Items, item => item.PlayerId == targetPlayerId);
    }

    [Fact]
    public async Task Handle_DifferentGamesAndDifficulties_ReturnsSeparateLeaderboards()
    {
        // Arrange
        var playerId = Guid.NewGuid();
        var player = new Player("multi_game_player", "US");
        player.GetType().GetProperty("Id")!.SetValue(player, playerId);
        _db.Players.Add(player);

        var entry1 = new ArcadeScoreEntry(playerId, "patternSprint", "easy", 1000, 40000, DateTimeOffset.UtcNow);
        var entry2 = new ArcadeScoreEntry(playerId, "patternSprint", "normal", 1500, 35000, DateTimeOffset.UtcNow);
        var entry3 = new ArcadeScoreEntry(playerId, "memoryFlip", "easy", 2000, 30000, DateTimeOffset.UtcNow);

        _db.ArcadeScores.AddRange(entry1, entry2, entry3);
        await _db.SaveChangesAsync();

        // Act
        var result1 = await _handler.Handle(
            new GetArcadeLeaderboard("patternSprint", "easy"),
            CancellationToken.None
        );
        var result2 = await _handler.Handle(
            new GetArcadeLeaderboard("patternSprint", "normal"),
            CancellationToken.None
        );
        var result3 = await _handler.Handle(
            new GetArcadeLeaderboard("memoryFlip", "easy"),
            CancellationToken.None
        );

        // Assert
        Assert.Single(result1.Items);
        Assert.Single(result2.Items);
        Assert.Single(result3.Items);

        Assert.Equal(1000, result1.Items[0].Score);
        Assert.Equal(1500, result2.Items[0].Score);
        Assert.Equal(2000, result3.Items[0].Score);
    }

    [Fact]
    public async Task Handle_PageSizeExceedsMax_ClampsToMax()
    {
        // Arrange
        await SetupTestScores();

        var query = new GetArcadeLeaderboard(
            GameId: "patternSprint",
            Difficulty: "normal",
            Page: 1,
            PageSize: 1000 // Exceeds max
        );

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.PageSize <= 100); // Should be clamped to max (100)
    }

    private async Task SetupTestScores()
    {
        var playerIds = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };

        foreach (var playerId in playerIds)
        {
            var player = new Player($"player_{playerId}", "US");
            player.GetType().GetProperty("Id")!.SetValue(player, playerId);
            _db.Players.Add(player);
        }

        var entries = new[]
        {
            new ArcadeScoreEntry(playerIds[0], "patternSprint", "normal", 1500, 25000, DateTimeOffset.UtcNow),
            new ArcadeScoreEntry(playerIds[1], "patternSprint", "normal", 1000, 30000, DateTimeOffset.UtcNow),
            new ArcadeScoreEntry(playerIds[2], "patternSprint", "normal", 1000, 40000, DateTimeOffset.UtcNow),
        };

        _db.ArcadeScores.AddRange(entries);
        await _db.SaveChangesAsync();
    }

    private async Task SetupTestScoresWithTargetPlayer(Guid targetPlayerId)
    {
        var targetPlayer = new Player("target_player", "US");
        targetPlayer.GetType().GetProperty("Id")!.SetValue(targetPlayer, targetPlayerId);
        _db.Players.Add(targetPlayer);

        var player1 = new Player("player_1", "US");
        var player1Id = Guid.NewGuid();
        player1.GetType().GetProperty("Id")!.SetValue(player1, player1Id);
        _db.Players.Add(player1);

        var player2 = new Player("player_2", "US");
        var player2Id = Guid.NewGuid();
        player2.GetType().GetProperty("Id")!.SetValue(player2, player2Id);
        _db.Players.Add(player2);

        var entries = new[]
        {
            new ArcadeScoreEntry(player1Id, "patternSprint", "normal", 1500, 25000, DateTimeOffset.UtcNow),
            new ArcadeScoreEntry(targetPlayerId, "patternSprint", "normal", 1000, 30000, DateTimeOffset.UtcNow),
            new ArcadeScoreEntry(player2Id, "patternSprint", "normal", 800, 35000, DateTimeOffset.UtcNow),
        };

        _db.ArcadeScores.AddRange(entries);
        await _db.SaveChangesAsync();
    }
}
