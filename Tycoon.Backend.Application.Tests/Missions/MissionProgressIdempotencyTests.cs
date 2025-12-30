using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Missions;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Backend.Infrastructure.Persistence;
using Tycoon.Shared.Contracts.Dtos;
using Xunit;

namespace Tycoon.Backend.Application.Tests.Missions
{
    public sealed class MissionProgressIdempotencyTests
    {
        private static AppDb NewDb()
        {
            var opts = new DbContextOptionsBuilder<AppDb>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .Options;

            return new AppDb(opts, dispatcher: null);
        }

        [Fact]
        public async Task ApplyMatchCompletedProgress_IsIdempotent()
        {
            await using var db = NewDb();

            // Seed: player + mission
            var player = new Player(username: "devart", countryCode: "US");
            player.AddScore(1000);

            var mission = new Mission(
                type: "Daily",
                key: "daily_play_3",
                title: "Play 3 matches",
                description: "Complete 3 matches today.",
                goal: 3,
                rewardXp: 50,
                rewardCoins: 10,
                rewardDiamonds: 0,
                active: true);

            db.Players.Add(player);
            db.Missions.Add(mission);
            await db.SaveChangesAsync();

            var progressService = new MissionProgressService(db);
            var handler = new ApplyMatchCompletedProgressHandler(db, progressService);

            var eventId = Guid.NewGuid();

            var dto = new MatchCompletedProgressDto(
                EventId: eventId,
                PlayerId: player.Id,
                IsWin: false,
                CorrectAnswers: 5,
                TotalQuestions: 10,
                DurationSeconds: 60);

            // First apply
            var r1 = await handler.Handle(new ApplyMatchCompletedProgress(dto), CancellationToken.None);
            r1.Status.Should().Be("Applied");

            // Duplicate apply
            var r2 = await handler.Handle(new ApplyMatchCompletedProgress(dto), CancellationToken.None);
            r2.Status.Should().Be("Duplicate");

            // Verify mission claim progress is only +1, not +2
            var claim = await db.MissionClaims.SingleAsync(x => x.PlayerId == player.Id && x.MissionId == mission.Id);
            claim.Progress.Should().Be(1);
        }
    }
}
