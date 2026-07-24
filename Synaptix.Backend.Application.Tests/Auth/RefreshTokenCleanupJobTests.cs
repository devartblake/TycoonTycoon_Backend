using System.Reflection;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Synaptix.Backend.Application.Auth;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Backend.Infrastructure.Persistence;

namespace Synaptix.Backend.Application.Tests.Auth;

public sealed class RefreshTokenCleanupJobTests
{
    private static AppDb NewDb()
    {
        var opts = new DbContextOptionsBuilder<AppDb>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDb(opts, dispatcher: null);
    }

    private static RefreshToken Token(
        Guid userId, string token, DateTimeOffset expiresAt,
        bool revoked = false, TimeSpan? revokedAgo = null)
    {
        var rt = new RefreshToken(userId, token, "device-1", expiresAt, "user");
        if (revoked)
        {
            rt.Revoke();
            if (revokedAgo != null)
            {
                typeof(RefreshToken)
                    .GetProperty(nameof(RefreshToken.RevokedAt))!
                    .SetValue(rt, DateTimeOffset.UtcNow - revokedAgo.Value);
            }
        }
        return rt;
    }

    [Fact]
    public async Task Deletes_Expired_And_OldRevoked_KeepsActiveAndRecent()
    {
        await using var db = NewDb();
        var user = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        // Should be deleted:
        db.RefreshTokens.Add(Token(user, "long-expired", now.AddDays(-30)));
        db.RefreshTokens.Add(Token(user, "old-revoked", now.AddDays(10),
            revoked: true, revokedAgo: TimeSpan.FromDays(30)));

        // Should be kept:
        db.RefreshTokens.Add(Token(user, "active", now.AddDays(20)));
        db.RefreshTokens.Add(Token(user, "recently-revoked", now.AddDays(10),
            revoked: true, revokedAgo: TimeSpan.FromHours(1)));
        await db.SaveChangesAsync();

        var job = new RefreshTokenCleanupJob(
            db, NullLogger<RefreshTokenCleanupJob>.Instance, TimeSpan.FromDays(7));

        var deleted = await job.RunAsync(CancellationToken.None);

        deleted.Should().Be(2);
        var remaining = await db.RefreshTokens.Select(t => t.Token).ToListAsync();
        remaining.Should().BeEquivalentTo(new[] { "active", "recently-revoked" });
    }

    [Fact]
    public async Task Deletes_Across_MultipleBatches()
    {
        await using var db = NewDb();
        var user = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        // More than one batch (BatchSize = 500) of expired tokens.
        for (var i = 0; i < 1100; i++)
            db.RefreshTokens.Add(Token(user, $"expired-{i}", now.AddDays(-30)));
        await db.SaveChangesAsync();

        var job = new RefreshTokenCleanupJob(
            db, NullLogger<RefreshTokenCleanupJob>.Instance, TimeSpan.FromDays(7));

        var deleted = await job.RunAsync(CancellationToken.None);

        deleted.Should().Be(1100);
        (await db.RefreshTokens.CountAsync()).Should().Be(0);
    }
}
