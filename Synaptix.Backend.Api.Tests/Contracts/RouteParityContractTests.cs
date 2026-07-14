using System.Text.RegularExpressions;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Synaptix.Backend.Api.Tests.TestHost;

namespace Synaptix.Backend.Api.Tests.Contracts;

public sealed class RouteParityContractTests : IClassFixture<SynaptixApiFactory>
{
    private readonly SynaptixApiFactory _factory;

    public RouteParityContractTests(SynaptixApiFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [MemberData(nameof(ClientRoutes))]
    public void Client_route_is_mapped_by_backend(string method, string routePattern)
    {
        var endpoint = FindEndpoint(_factory, method, routePattern);

        endpoint.Should().NotBeNull($"{method} {routePattern} is part of the mobile/local-dev client contract");
    }

    public static TheoryData<string, string> ClientRoutes() => new()
    {
        // App / auth
        { "GET", "/api/v1/app/config" },
        { "POST", "/api/v1/auth/signup" },
        { "POST", "/api/v1/auth/login" },
        { "POST", "/api/v1/auth/refresh" },
        { "POST", "/api/v1/auth/logout" },
        { "POST", "/api/v1/auth/device/bootstrap" },
        { "POST", "/api/v1/auth/account/upgrade" },
        { "POST", "/api/v1/auth/mobile-game-login" },
        { "POST", "/api/v1/auth/link-game-account" },
        { "GET", "/api/v1/auth/oauth/{provider}" },
        // Secure channel (proxied to KMS)
        { "POST", "/api/v1/security/sessions/start" },
        { "POST", "/api/v1/security/sessions/renew" },
        { "POST", "/api/v1/security/sessions/revoke" },
        // Questions / quiz
        { "GET", "/api/v1/questions/set" },
        { "POST", "/api/v1/questions/check" },
        { "POST", "/api/v1/questions/check-batch" },
        { "POST", "/api/v1/quiz/complete" },
        // Assets
        { "GET", "/api/v1/assets/manifest" },
        { "GET", "/api/v1/assets/avatars/{avatarId}" },
        // Friends / social (Alpha handoff paths)
        { "GET", "/api/v1/users/me/friends" },
        { "POST", "/api/v1/users/me/friends/request" },
        { "GET", "/api/v1/users/me/friends/requests" },
        { "GET", "/api/v1/users/me/friends/requests/sent" },
        { "POST", "/api/v1/users/me/friends/requests/{requestId}/accept" },
        { "POST", "/api/v1/users/me/friends/requests/{requestId}/decline" },
        { "DELETE", "/api/v1/users/me/friends/requests/{requestId}" },
        { "DELETE", "/api/v1/users/me/friends/{friendPlayerId}" },
        { "GET", "/api/v1/users/me/friends/suggestions" },
        { "GET", "/api/v1/users/search" },
    };

    private static RouteEndpoint? FindEndpoint(WebApplicationFactory<Program> factory, string method, string routePattern)
    {
        var want = NormalizeRoute(routePattern);
        var endpoints = factory.Services.GetRequiredService<EndpointDataSource>().Endpoints;
        return endpoints
            .OfType<RouteEndpoint>()
            .FirstOrDefault(endpoint =>
            {
                var raw = endpoint.RoutePattern.RawText;
                if (raw is null)
                    return false;
                if (!string.Equals(NormalizeRoute(raw), want, StringComparison.OrdinalIgnoreCase))
                    return false;
                return endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods
                    .Contains(method, StringComparer.OrdinalIgnoreCase) == true;
            });
    }

    /// <summary>
    /// Strip parameter constraints ({id:guid} → {id}) and trailing slashes so client
    /// path constants match ASP.NET Core RawText.
    /// </summary>
    private static string NormalizeRoute(string route)
    {
        var normalized = Regex.Replace(route, @"\{([^}:]+)[^}]*\}", "{$1}");
        return normalized.TrimEnd('/').ToLowerInvariant();
    }
}
