using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Synaptix.Security.Kms.Api.Features.Internal;
using Synaptix.Security.Kms.Tests.Helpers;

namespace Synaptix.Security.Kms.Tests.Internal;

public sealed class InternalSessionEndpointTests
{
    [Fact]
    public async Task HandleStartSession_CreatesSessionInStore()
    {
        var store = new InMemorySessionStore();

        var result = await InternalEndpoints.HandleStartSession(
            new InternalStartSessionRequest(
                "operator-dashboard-django",
                "django-admin-auth",
                ["X25519-HKDF-SHA256-AES256GCM"]),
            store,
            default);

        var http = new DefaultHttpContext
        {
            RequestServices = new ServiceCollection().AddLogging().BuildServiceProvider(),
            Response = { Body = new MemoryStream() }
        };
        await result.ExecuteAsync(http);
        http.Response.Body.Position = 0;

        var body = await JsonDocument.ParseAsync(http.Response.Body);
        var sessionId = body.RootElement.GetProperty("sessionId").GetGuid();

        var session = await store.GetAsync(sessionId, default);
        session.Should().NotBeNull();
        session!.SubjectId.Should().Be("operator-dashboard-django");
        session.DeviceId.Should().Be("django-admin-auth");
    }
}
