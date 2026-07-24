using System.Reflection;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Synaptix.Backend.Application.Auth;
using Synaptix.Backend.Application.Email;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Backend.Infrastructure.Persistence;

namespace Synaptix.Backend.Application.Tests.Auth;

/// <summary>
/// Covers the refresh-token hardening added for the secure-channel work:
/// OAuth2 reuse detection with family revocation (#1) and channel-subject
/// binding (#2). The failure paths short-circuit before any JWT is minted,
/// so a throwaway signing secret is sufficient.
/// </summary>
public sealed class RefreshTokenSecurityTests
{
    private static AppDb NewDb()
    {
        var opts = new DbContextOptionsBuilder<AppDb>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDb(opts, dispatcher: null);
    }

    private static AuthService NewService(AppDb db)
    {
        var jwt = Options.Create(new JwtSettings
        {
            SecretKey = "unit-test-signing-secret-key-0123456789abcdef",
            Issuer = "test",
            Audience = "test",
        });
        return new AuthService(db, jwt, NullLogger<AuthService>.Instance, new NoopEmailService());
    }

    private static async Task<User> SeedUserAsync(AppDb db)
    {
        var user = new User("player@test.local", "player", "hash");
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private static RefreshToken SeedToken(AppDb db, Guid userId, string token, string deviceId)
    {
        var rt = new RefreshToken(userId, token, deviceId, DateTimeOffset.UtcNow.AddDays(30), "user");
        db.RefreshTokens.Add(rt);
        return rt;
    }

    private static void BackdateRevocation(RefreshToken token, TimeSpan ago)
    {
        // RevokedAt has a private setter; backdate it so the reuse falls outside
        // the grace window without waiting real time.
        typeof(RefreshToken)
            .GetProperty(nameof(RefreshToken.RevokedAt))!
            .SetValue(token, DateTimeOffset.UtcNow - ago);
    }

    [Fact]
    public async Task Reuse_OutsideGraceWindow_RevokesEntireFamily()
    {
        await using var db = NewDb();
        var user = await SeedUserAsync(db);

        var rotated = SeedToken(db, user.Id, "rotated-token", "device-1");
        rotated.Revoke();
        BackdateRevocation(rotated, TimeSpan.FromMinutes(5));

        // Two other still-active tokens for the same user+device (the "family").
        SeedToken(db, user.Id, "active-a", "device-1");
        SeedToken(db, user.Id, "active-b", "device-1");
        // An unrelated device's token must survive.
        SeedToken(db, user.Id, "other-device", "device-2");
        await db.SaveChangesAsync();

        var svc = NewService(db);

        var act = () => svc.RefreshAsync("rotated-token");
        await act.Should().ThrowAsync<UnauthorizedAccessException>();

        var device1Active = await db.RefreshTokens
            .Where(t => t.DeviceId == "device-1" && !t.IsRevoked)
            .ToListAsync();
        device1Active.Should().BeEmpty("reuse detection revokes the whole active family for the device");

        var otherDevice = await db.RefreshTokens.SingleAsync(t => t.DeviceId == "device-2");
        otherDevice.IsRevoked.Should().BeFalse("a different device's family must be untouched");
    }

    [Fact]
    public async Task Reuse_WithinGraceWindow_DoesNotRevokeFamily()
    {
        await using var db = NewDb();
        var user = await SeedUserAsync(db);

        var rotated = SeedToken(db, user.Id, "rotated-token", "device-1");
        rotated.Revoke(); // RevokedAt == now → inside the grace window
        SeedToken(db, user.Id, "active-a", "device-1");
        await db.SaveChangesAsync();

        var svc = NewService(db);

        var act = () => svc.RefreshAsync("rotated-token");
        await act.Should().ThrowAsync<UnauthorizedAccessException>();

        var stillActive = await db.RefreshTokens.SingleAsync(t => t.Token == "active-a");
        stillActive.IsRevoked.Should().BeFalse("a benign retry inside the grace window must not nuke the family");
    }

    [Fact]
    public async Task Refresh_WithMismatchedChannelSubject_IsRejected()
    {
        await using var db = NewDb();
        var user = await SeedUserAsync(db);
        SeedToken(db, user.Id, "valid-token", "device-1");
        await db.SaveChangesAsync();

        var svc = NewService(db);

        var attacker = Guid.NewGuid();
        var act = () => svc.RefreshAsync("valid-token", expectedSubject: attacker);
        await act.Should().ThrowAsync<UnauthorizedAccessException>();

        // The token must remain valid — a rejected mismatch must not rotate it.
        var token = await db.RefreshTokens.SingleAsync(t => t.Token == "valid-token");
        token.IsRevoked.Should().BeFalse();
    }

    [Fact]
    public async Task Refresh_WithMatchingChannelSubject_Rotates()
    {
        await using var db = NewDb();
        var user = await SeedUserAsync(db);
        SeedToken(db, user.Id, "valid-token", "device-1");
        await db.SaveChangesAsync();

        var svc = NewService(db);

        var result = await svc.RefreshAsync("valid-token", expectedSubject: user.Id);

        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBe("valid-token", "a successful refresh rotates the token");

        var old = await db.RefreshTokens.SingleAsync(t => t.Token == "valid-token");
        old.IsRevoked.Should().BeTrue();
    }

    private sealed class NoopEmailService : IEmailService
    {
        public Task SendAsync(string to, string subject, string htmlBody, CancellationToken ct = default)
            => Task.CompletedTask;
    }
}
