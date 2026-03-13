using FluentAssertions;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Application.Tests.Domain;

/// <summary>
/// Pure domain unit tests for MissionClaim.
/// No database or infrastructure dependencies.
/// </summary>
public sealed class MissionClaimTests
{
    [Fact]
    public void AddProgress_Increases_Progress()
    {
        var claim = new MissionClaim(Guid.NewGuid(), Guid.NewGuid());

        claim.AddProgress(2, goal: 5);

        claim.Progress.Should().Be(2);
        claim.Completed.Should().BeFalse();
    }

    [Fact]
    public void AddProgress_Clamps_AtGoal()
    {
        var claim = new MissionClaim(Guid.NewGuid(), Guid.NewGuid());

        claim.AddProgress(10, goal: 3);

        claim.Progress.Should().Be(3, "progress should be clamped to the mission goal");
    }

    [Fact]
    public void AddProgress_Marks_Completed_WhenGoalReached()
    {
        var claim = new MissionClaim(Guid.NewGuid(), Guid.NewGuid());

        claim.AddProgress(3, goal: 3);

        claim.Completed.Should().BeTrue();
        claim.CompletedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void AddProgress_DoesNotOvercomplete_WhenCalledAgainAfterGoal()
    {
        var claim = new MissionClaim(Guid.NewGuid(), Guid.NewGuid());
        claim.AddProgress(3, goal: 3);

        // Call again — progress is at cap, Completed stays true
        claim.AddProgress(1, goal: 3);

        claim.Progress.Should().Be(3);
        claim.Completed.Should().BeTrue();
    }

    [Fact]
    public void AddProgress_IsNoOp_ForZeroAmount()
    {
        var claim = new MissionClaim(Guid.NewGuid(), Guid.NewGuid());

        claim.AddProgress(0, goal: 3);

        claim.Progress.Should().Be(0);
        claim.Completed.Should().BeFalse();
    }

    [Fact]
    public void AddProgress_IsNoOp_ForNegativeAmount()
    {
        var claim = new MissionClaim(Guid.NewGuid(), Guid.NewGuid());

        claim.AddProgress(-5, goal: 3);

        claim.Progress.Should().Be(0);
    }

    [Fact]
    public void MarkClaimed_Sets_ClaimedTrue_AndClaimedAt()
    {
        var claim = new MissionClaim(Guid.NewGuid(), Guid.NewGuid());
        claim.AddProgress(3, goal: 3); // complete first

        claim.MarkClaimed();

        claim.Claimed.Should().BeTrue();
        claim.ClaimedAtUtc.Should().NotBeNull();
    }

    [Fact]
    public void MarkClaimed_Throws_WhenNotCompleted()
    {
        var claim = new MissionClaim(Guid.NewGuid(), Guid.NewGuid());
        claim.AddProgress(1, goal: 3); // incomplete

        var act = () => claim.MarkClaimed();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*completed*");
    }

    [Fact]
    public void MarkClaimed_IsIdempotent_WhenCalledTwice()
    {
        var claim = new MissionClaim(Guid.NewGuid(), Guid.NewGuid());
        claim.AddProgress(3, goal: 3);
        claim.MarkClaimed();
        var firstClaimedAt = claim.ClaimedAtUtc;

        // Second call should be silently ignored
        claim.MarkClaimed();

        claim.Claimed.Should().BeTrue();
        claim.ClaimedAtUtc.Should().Be(firstClaimedAt);
    }

    [Fact]
    public void Reset_Clears_AllProgress()
    {
        var claim = new MissionClaim(Guid.NewGuid(), Guid.NewGuid());
        claim.AddProgress(3, goal: 3);
        claim.MarkClaimed();

        var resetTime = DateTime.UtcNow;
        claim.Reset(resetTime);

        claim.Progress.Should().Be(0);
        claim.Completed.Should().BeFalse();
        claim.CompletedAtUtc.Should().BeNull();
        claim.Claimed.Should().BeFalse();
        claim.ClaimedAtUtc.Should().BeNull();
        claim.LastResetAtUtc.Should().Be(resetTime);
    }

    [Fact]
    public void AddProgress_IncrementalSteps_EventuallyCompleteMission()
    {
        var claim = new MissionClaim(Guid.NewGuid(), Guid.NewGuid());

        claim.AddProgress(1, goal: 3);
        claim.Completed.Should().BeFalse();

        claim.AddProgress(1, goal: 3);
        claim.Completed.Should().BeFalse();

        claim.AddProgress(1, goal: 3);
        claim.Completed.Should().BeTrue();
        claim.Progress.Should().Be(3);
    }
}