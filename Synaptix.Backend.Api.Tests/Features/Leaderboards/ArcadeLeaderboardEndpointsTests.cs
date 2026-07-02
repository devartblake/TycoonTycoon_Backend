using Xunit;
using Microsoft.AspNetCore.WebUtilities;
using System.Net;
using System.Text;
using System.Text.Json;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Backend.Infrastructure.Persistence;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Tests.Features.Leaderboards;

public class ArcadeLeaderboardEndpointsTests : IClassFixture<TestApiFactory>
{
    private readonly TestApiFactory _factory;
    private readonly HttpClient _client;

    public ArcadeLeaderboardEndpointsTests(TestApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SubmitScore_WithValidRequest_ReturnsSuccess()
    {
        // Arrange
        var player = await _factory.CreateTestPlayer("test_player_1");
        var token = _factory.GenerateToken(player.Id);
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var request = new ArcadeScoreSubmitRequest(
            GameId: "patternSprint",
            Difficulty: "normal",
            Score: 1500,
            DurationMs: 35000
        );

        // Act
        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json"
        );
        var response = await _client.PostAsync("/api/v1/leaderboards/arcade/submit", content);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.GetProperty("success").GetBoolean());
    }

    [Fact]
    public async Task SubmitScore_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var request = new ArcadeScoreSubmitRequest(
            GameId: "patternSprint",
            Difficulty: "normal",
            Score: 1500,
            DurationMs: 35000
        );

        // Act
        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json"
        );
        var response = await _client.PostAsync("/api/v1/leaderboards/arcade/submit", content);

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SubmitScore_WithInvalidGameId_ReturnsBadRequest()
    {
        // Arrange
        var player = await _factory.CreateTestPlayer("test_player_2");
        var token = _factory.GenerateToken(player.Id);
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var request = new ArcadeScoreSubmitRequest(
            GameId: "",
            Difficulty: "normal",
            Score: 1500,
            DurationMs: 35000
        );

        // Act
        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json"
        );
        var response = await _client.PostAsync("/api/v1/leaderboards/arcade/submit", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SubmitScore_WithNegativeScore_ReturnsBadRequest()
    {
        // Arrange
        var player = await _factory.CreateTestPlayer("test_player_3");
        var token = _factory.GenerateToken(player.Id);
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var request = new ArcadeScoreSubmitRequest(
            GameId: "patternSprint",
            Difficulty: "normal",
            Score: -100,
            DurationMs: 35000
        );

        // Act
        var content = new StringContent(
            JsonSerializer.Serialize(request),
            Encoding.UTF8,
            "application/json"
        );
        var response = await _client.PostAsync("/api/v1/leaderboards/arcade/submit", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SubmitScore_HigherScoreThanPrevious_UpdatesEntry()
    {
        // Arrange
        var player = await _factory.CreateTestPlayer("test_player_4");
        var token = _factory.GenerateToken(player.Id);
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // First submission
        var firstRequest = new ArcadeScoreSubmitRequest(
            GameId: "patternSprint",
            Difficulty: "hard",
            Score: 1000,
            DurationMs: 40000
        );
        var firstContent = new StringContent(
            JsonSerializer.Serialize(firstRequest),
            Encoding.UTF8,
            "application/json"
        );
        await _client.PostAsync("/api/v1/leaderboards/arcade/submit", firstContent);

        // Second submission with higher score
        var secondRequest = new ArcadeScoreSubmitRequest(
            GameId: "patternSprint",
            Difficulty: "hard",
            Score: 1500,
            DurationMs: 35000
        );
        var secondContent = new StringContent(
            JsonSerializer.Serialize(secondRequest),
            Encoding.UTF8,
            "application/json"
        );

        // Act
        var response = await _client.PostAsync("/api/v1/leaderboards/arcade/submit", secondContent);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(json.RootElement.GetProperty("success").GetBoolean());

        // Verify via database
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();
            var entry = await db.ArcadeScores
                .FirstOrDefaultAsync(e =>
                    e.PlayerId == player.Id &&
                    e.GameId == "patternSprint" &&
                    e.Difficulty == "hard");

            Assert.NotNull(entry);
            Assert.Equal(1500, entry.Score);
            Assert.Equal(35000, entry.DurationMs);
        }
    }

    [Fact]
    public async Task SubmitScore_LowerScoreThanPrevious_DoesNotUpdate()
    {
        // Arrange
        var player = await _factory.CreateTestPlayer("test_player_5");
        var token = _factory.GenerateToken(player.Id);
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // First submission
        var firstRequest = new ArcadeScoreSubmitRequest(
            GameId: "memoryFlip",
            Difficulty: "normal",
            Score: 2000,
            DurationMs: 30000
        );
        var firstContent = new StringContent(
            JsonSerializer.Serialize(firstRequest),
            Encoding.UTF8,
            "application/json"
        );
        await _client.PostAsync("/api/v1/leaderboards/arcade/submit", firstContent);

        // Second submission with lower score
        var secondRequest = new ArcadeScoreSubmitRequest(
            GameId: "memoryFlip",
            Difficulty: "normal",
            Score: 1500,
            DurationMs: 35000
        );
        var secondContent = new StringContent(
            JsonSerializer.Serialize(secondRequest),
            Encoding.UTF8,
            "application/json"
        );

        // Act
        var response = await _client.PostAsync("/api/v1/leaderboards/arcade/submit", secondContent);

        // Assert - should return success: false
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.False(json.RootElement.GetProperty("success").GetBoolean());

        // Verify score wasn't updated in database
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();
            var entry = await db.ArcadeScores
                .FirstOrDefaultAsync(e =>
                    e.PlayerId == player.Id &&
                    e.GameId == "memoryFlip" &&
                    e.Difficulty == "normal");

            Assert.NotNull(entry);
            Assert.Equal(2000, entry.Score); // Still the original score
        }
    }

    [Fact]
    public async Task GetLeaderboard_ReturnsTopScores()
    {
        // Arrange
        var players = new List<Player>();
        for (int i = 0; i < 3; i++)
        {
            players.Add(await _factory.CreateTestPlayer($"leaderboard_player_{i}"));
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();

            // Insert test scores
            var scores = new[]
            {
                new ArcadeScoreEntry(players[0].Id, "quickMathRush", "easy", 2500, 25000, DateTimeOffset.UtcNow),
                new ArcadeScoreEntry(players[1].Id, "quickMathRush", "easy", 2000, 30000, DateTimeOffset.UtcNow),
                new ArcadeScoreEntry(players[2].Id, "quickMathRush", "easy", 1500, 35000, DateTimeOffset.UtcNow),
            };

            foreach (var score in scores)
            {
                db.ArcadeScores.Add(score);
            }
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync(
            "/api/v1/leaderboards/arcade/quickMathRush/easy"
        );

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;

        Assert.Equal("quickMathRush", root.GetProperty("gameId").GetString());
        Assert.Equal("easy", root.GetProperty("difficulty").GetString());

        var items = root.GetProperty("items").EnumerateArray().ToList();
        Assert.Equal(3, items.Count);

        // Verify scores are in descending order
        Assert.Equal(2500, items[0].GetProperty("score").GetInt32());
        Assert.Equal(2000, items[1].GetProperty("score").GetInt32());
        Assert.Equal(1500, items[2].GetProperty("score").GetInt32());

        // Verify ranks
        Assert.Equal(1, items[0].GetProperty("rank").GetInt32());
        Assert.Equal(2, items[1].GetProperty("rank").GetInt32());
        Assert.Equal(3, items[2].GetProperty("rank").GetInt32());
    }

    [Fact]
    public async Task GetLeaderboard_WithAuthentication_ReturnsPlayerRank()
    {
        // Arrange
        var targetPlayer = await _factory.CreateTestPlayer("rank_test_player");
        var otherPlayers = new List<Player>();
        for (int i = 0; i < 4; i++)
        {
            otherPlayers.Add(await _factory.CreateTestPlayer($"rank_other_player_{i}"));
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();

            var scores = new[]
            {
                new ArcadeScoreEntry(otherPlayers[0].Id, "patternSprint", "insane", 3000, 20000, DateTimeOffset.UtcNow),
                new ArcadeScoreEntry(otherPlayers[1].Id, "patternSprint", "insane", 2500, 25000, DateTimeOffset.UtcNow),
                new ArcadeScoreEntry(targetPlayer.Id, "patternSprint", "insane", 2200, 28000, DateTimeOffset.UtcNow),
                new ArcadeScoreEntry(otherPlayers[2].Id, "patternSprint", "insane", 2000, 30000, DateTimeOffset.UtcNow),
            };

            foreach (var score in scores)
            {
                db.ArcadeScores.Add(score);
            }
            await db.SaveChangesAsync();
        }

        var token = _factory.GenerateToken(targetPlayer.Id);
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        // Act
        var response = await _client.GetAsync(
            "/api/v1/leaderboards/arcade/patternSprint/insane"
        );

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;

        var myRank = root.GetProperty("myRank").GetInt32();
        var myScore = root.GetProperty("myScore").GetInt32();

        Assert.Equal(3, myRank);
        Assert.Equal(2200, myScore);
    }

    [Fact]
    public async Task GetLeaderboard_WithPagination_ReturnsCorrectPage()
    {
        // Arrange
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();

            // Insert 25 test scores
            for (int i = 0; i < 25; i++)
            {
                var player = await _factory.CreateTestPlayer($"pagination_player_{i}");
                var score = new ArcadeScoreEntry(
                    player.Id,
                    "quickMathRush",
                    "normal",
                    5000 - (i * 100),
                    25000 + (i * 1000),
                    DateTimeOffset.UtcNow
                );
                db.ArcadeScores.Add(score);
            }
            await db.SaveChangesAsync();
        }

        // Act - Get page 1 with pageSize=10
        var response1 = await _client.GetAsync(
            "/api/v1/leaderboards/arcade/quickMathRush/normal?page=1&pageSize=10"
        );

        // Act - Get page 2 with pageSize=10
        var response2 = await _client.GetAsync(
            "/api/v1/leaderboards/arcade/quickMathRush/normal?page=2&pageSize=10"
        );

        // Assert
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);

        var json1 = JsonDocument.Parse(await response1.Content.ReadAsStringAsync());
        var json2 = JsonDocument.Parse(await response2.Content.ReadAsStringAsync());

        var root1 = json1.RootElement;
        var root2 = json2.RootElement;

        Assert.Equal(1, root1.GetProperty("page").GetInt32());
        Assert.Equal(2, root2.GetProperty("page").GetInt32());
        Assert.Equal(10, root1.GetProperty("pageSize").GetInt32());
        Assert.Equal(10, root2.GetProperty("pageSize").GetInt32());
        Assert.Equal(25, root1.GetProperty("total").GetInt32());
        Assert.Equal(25, root2.GetProperty("total").GetInt32());

        // Verify different entries on each page
        var items1 = root1.GetProperty("items").EnumerateArray().ToList();
        var items2 = root2.GetProperty("items").EnumerateArray().ToList();

        Assert.Equal(10, items1.Count);
        Assert.Equal(10, items2.Count);

        // First page should have rank 1-10, second page 11-20
        Assert.Equal(1, items1[0].GetProperty("rank").GetInt32());
        Assert.Equal(10, items1[9].GetProperty("rank").GetInt32());
        Assert.Equal(11, items2[0].GetProperty("rank").GetInt32());
        Assert.Equal(20, items2[9].GetProperty("rank").GetInt32());
    }

    [Fact]
    public async Task GetLeaderboard_EmptyLeaderboard_ReturnsEmptyList()
    {
        // Act
        var response = await _client.GetAsync(
            "/api/v1/leaderboards/arcade/unknownGame/expert"
        );

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var root = json.RootElement;

        var items = root.GetProperty("items").EnumerateArray().ToList();
        Assert.Empty(items);
        Assert.Equal(0, root.GetProperty("total").GetInt32());
    }

    [Fact]
    public async Task GetLeaderboard_SameSortsCorrectlyByDuration()
    {
        // Arrange
        var players = new List<Player>();
        for (int i = 0; i < 3; i++)
        {
            players.Add(await _factory.CreateTestPlayer($"same_score_player_{i}"));
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();

            // Insert entries with same score but different durations
            var scores = new[]
            {
                new ArcadeScoreEntry(players[0].Id, "patternSprint", "normal", 1000, 40000, DateTimeOffset.UtcNow),
                new ArcadeScoreEntry(players[1].Id, "patternSprint", "normal", 1000, 25000, DateTimeOffset.UtcNow),
                new ArcadeScoreEntry(players[2].Id, "patternSprint", "normal", 1000, 30000, DateTimeOffset.UtcNow),
            };

            foreach (var score in scores)
            {
                db.ArcadeScores.Add(score);
            }
            await db.SaveChangesAsync();
        }

        // Act
        var response = await _client.GetAsync(
            "/api/v1/leaderboards/arcade/patternSprint/normal"
        );

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var items = json.RootElement.GetProperty("items").EnumerateArray().ToList();

        // Should be sorted by duration ascending (faster times rank higher)
        Assert.Equal(25000, items[0].GetProperty("durationMs").GetInt32());
        Assert.Equal(30000, items[1].GetProperty("durationMs").GetInt32());
        Assert.Equal(40000, items[2].GetProperty("durationMs").GetInt32());
    }
}
