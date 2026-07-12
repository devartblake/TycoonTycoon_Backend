using System.Security.Claims;
using FluentAssertions;
using Xunit;

namespace Synaptix.Backend.Api.Tests.Security;

// #414: the Hangfire dashboard must be admin-gated, not merely "authenticated".
// HangfireAuthorizationFilter is a top-level (global namespace) type in Program.cs.
public sealed class HangfireAuthorizationFilterTests
{
    private static ClaimsPrincipal Principal(bool authenticated, params (string Type, string Value)[] claims)
    {
        var identity = authenticated
            ? new ClaimsIdentity(claims.Select(c => new Claim(c.Type, c.Value)), "TestAuth")
            : new ClaimsIdentity(claims.Select(c => new Claim(c.Type, c.Value))); // no auth type => not authenticated
        return new ClaimsPrincipal(identity);
    }

    [Fact]
    public void NullUser_IsNotAuthorized()
        => HangfireAuthorizationFilter.IsAuthorized(null).Should().BeFalse();

    [Fact]
    public void Unauthenticated_IsNotAuthorized()
        => HangfireAuthorizationFilter.IsAuthorized(Principal(false, ("role", "admin"))).Should().BeFalse();

    [Fact]
    public void AuthenticatedNonAdmin_IsNotAuthorized()
        => HangfireAuthorizationFilter.IsAuthorized(Principal(true, ("role", "user"))).Should().BeFalse();

    [Fact]
    public void AuthenticatedWithNoRole_IsNotAuthorized()
        => HangfireAuthorizationFilter.IsAuthorized(Principal(true, ("email", "a@b.com"))).Should().BeFalse();

    [Fact]
    public void AdminViaRoleClaim_IsAuthorized()
        => HangfireAuthorizationFilter.IsAuthorized(Principal(true, ("role", "admin"))).Should().BeTrue();

    [Fact]
    public void AdminViaClaimTypesRole_IsAuthorized()
        => HangfireAuthorizationFilter.IsAuthorized(Principal(true, (ClaimTypes.Role, "Admin"))).Should().BeTrue();
}
