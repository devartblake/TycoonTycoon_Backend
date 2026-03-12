using FluentAssertions;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Application.Tests.Domain;

/// <summary>
/// Pure domain unit tests for the Player aggregate.
/// No database or infrastructure dependencies.
/// </summary>
public sealed class PlayerTests
{
    [Fact]
    public void AddXp_Increases_Xp()
    {
        var player = new Player("testuser");

        player.AddXp(50);

        player.Xp.Should().Be(50);
    }

    [Fact]
    public void AddXp_DoesNotAccept_NegativeAmount()
    {
        var player = new Player("testuser");

        player.AddXp(-10);

        player.Xp.Should().Be(0);
    }

    [Fact]
    public void AddXp_LevelsUp_WhenThresholdReached()
    {
        var player = new Player("testuser");
        // Level 1 threshold = 1 * 100 = 100 XP

        player.AddXp(100);

        player.Level.Should().Be(2, "player should level up when XP reaches Level * 100");
        player.Xp.Should().Be(0, "XP should be consumed on level-up");
    }

    [Fact]
    public void AddXp_LevelsUp_MultipleTimesAtOnce()
    {
        var player = new Player("testuser");
        // Level 1: 100 XP, Level 2: 200 XP → total 300 XP to reach level 3

        player.AddXp(300);

        player.Level.Should().Be(3);
    }

    [Fact]
    public void AddXp_Carries_Remainder_AfterLevelUp()
    {
        var player = new Player("testuser");

        player.AddXp(150); // Level 1 threshold is 100; 50 XP remains

        player.Level.Should().Be(2);
        player.Xp.Should().Be(50);
    }

    [Fact]
    public void AddScore_Increases_Score()
    {
        var player = new Player("testuser");

        player.AddScore(500);

        player.Score.Should().Be(500);
    }

    [Fact]
    public void AddScore_DoesNotAccept_NegativeAmount()
    {
        var player = new Player("testuser");
        player.AddScore(100);

        player.AddScore(-50);

        player.Score.Should().Be(100, "AddScore should ignore negative amounts");
    }

    [Fact]
    public void ApplyMatchResult_ScoreDelta_ClampsToZero_WhenNegative()
    {
        var player = new Player("testuser");
        player.AddScore(10);

        player.ApplyMatchResult(scoreDelta: -100, xpEarned: 0);

        player.Score.Should().Be(0, "Score should not go below 0");
    }

    [Fact]
    public void ApplyMatchResult_Applies_Both_Score_And_Xp()
    {
        var player = new Player("testuser");

        player.ApplyMatchResult(scoreDelta: 10, xpEarned: 50);

        player.Score.Should().Be(10);
        player.Xp.Should().Be(50);
    }

    [Fact]
    public void SetTier_UpdatesTierId()
    {
        var player = new Player("testuser");
        var tierId = Guid.NewGuid();

        player.SetTier(tierId);

        player.TierId.Should().Be(tierId);
    }

    [Fact]
    public void SetTier_IsIdempotent_WhenSameTier()
    {
        var player = new Player("testuser");
        var tierId = Guid.NewGuid();
        player.SetTier(tierId);

        // Setting the same tier again should not cause issues
        player.SetTier(tierId);

        player.TierId.Should().Be(tierId);
    }

    [Fact]
    public void AddCoins_Increases_Coins()
    {
        var player = new Player("testuser");

        player.AddCoins(100);

        player.Coins.Should().Be(100);
    }

    [Fact]
    public void AddCoins_DoesNotAccept_NegativeAmount()
    {
        var player = new Player("testuser");
        player.AddCoins(50);

        player.AddCoins(-10);

        player.Coins.Should().Be(50);
    }

    [Fact]
    public void AddDiamonds_Increases_Diamonds()
    {
        var player = new Player("testuser");

        player.AddDiamonds(5);

        player.Diamonds.Should().Be(5);
    }

    [Fact]
    public void Player_InitialisesAt_Level1_WithZeroStats()
    {
        var player = new Player("newuser", "GB");

        player.Level.Should().Be(1);
        player.Xp.Should().Be(0);
        player.Score.Should().Be(0);
        player.Coins.Should().Be(0);
        player.Diamonds.Should().Be(0);
        player.CountryCode.Should().Be("GB");
    }
}
