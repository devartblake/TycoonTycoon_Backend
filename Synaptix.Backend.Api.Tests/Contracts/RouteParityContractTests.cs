using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Synaptix.Backend.Api.Tests.TestHost;

namespace Synaptix.Backend.Api.Tests.Contracts;

public sealed class RouteParityContractTests : IClassFixture<TycoonApiFactory>
{
    private readonly TycoonApiFactory _factory;

    public RouteParityContractTests(TycoonApiFactory factory)
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
        { "POST", "/api/v1/security/sessions/start" },
        { "POST", "/api/v1/security/sessions/renew" },
        { "POST", "/api/v1/security/sessions/revoke" },
        { "GET", "/api/v1/questions/set" },
        { "POST", "/api/v1/questions/check" },
        { "POST", "/api/v1/questions/check-batch" },
        { "POST", "/api/v1/quiz/complete" },
        { "GET", "/api/v1/assets/manifest" },
        { "GET", "/api/v1/assets/avatars/{avatarId}" },
    };

    private static RouteEndpoint? FindEndpoint(WebApplicationFactory<Program> factory, string method, string routePattern)
    {
        var endpoints = factory.Services.GetRequiredService<EndpointDataSource>().Endpoints;
        return endpoints
            .OfType<RouteEndpoint>()
            .FirstOrDefault(endpoint =>
                string.Equals(endpoint.RoutePattern.RawText, routePattern, StringComparison.OrdinalIgnoreCase) &&
                endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods
                    .Contains(method, StringComparer.OrdinalIgnoreCase) == true);
    }
}
