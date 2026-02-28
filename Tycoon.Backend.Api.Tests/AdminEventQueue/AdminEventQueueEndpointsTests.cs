using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Shared.Contracts.Dtos;
using Xunit;

namespace Tycoon.Backend.Api.Tests.AdminEventQueue;

public sealed class AdminEventQueueEndpointsTests : IClassFixture<TycoonApiFactory>
{
    private readonly HttpClient _http;

    public AdminEventQueueEndpointsTests(TycoonApiFactory factory)
    {
        _http = factory.CreateClient().WithAdminOpsKey();
    }

    [Fact]
    public async Task Upload_Dedupes_And_Returns_PerEvent_Status()
    {
        var playerId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        var req = new AdminEventQueueUploadRequest(
            Source: "mobile_admin",
            ExportedAt: DateTimeOffset.UtcNow,
            PlayerId: playerId.ToString(),
            Events: new List<AdminEventQueueItemRequest>
            {
                new(eventId.ToString(), "spin_completed", DateTimeOffset.UtcNow, new Dictionary<string, object>{{"score", 100}}, 0)
            }
        );

        var first = await _http.PostAsJsonAsync("/admin/event-queue/upload", req);
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var firstBody = await first.Content.ReadFromJsonAsync<AdminEventQueueUploadResponse>();
        firstBody.Should().NotBeNull();
        firstBody!.Accepted.Should().Be(1);
        firstBody.Duplicates.Should().Be(0);
        firstBody.Results.Should().ContainSingle(r => r.EventId == eventId.ToString() && r.Status == "accepted");

        var second = await _http.PostAsJsonAsync("/admin/event-queue/upload", req);
        second.StatusCode.Should().Be(HttpStatusCode.OK);

        var secondBody = await second.Content.ReadFromJsonAsync<AdminEventQueueUploadResponse>();
        secondBody.Should().NotBeNull();
        secondBody!.Accepted.Should().Be(0);
        secondBody.Duplicates.Should().Be(1);
        secondBody.Results.Should().ContainSingle(r => r.EventId == eventId.ToString() && r.Status == "duplicate");
    }

    [Fact]
    public async Task Reprocess_Queues_Job()
    {
        var resp = await _http.PostAsJsonAsync("/admin/event-queue/reprocess", new AdminEventQueueReprocessRequest("failed_only", 1000));
        resp.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var body = await resp.Content.ReadFromJsonAsync<AdminEventQueueReprocessResponse>();
        body.Should().NotBeNull();
        body!.JobId.Should().StartWith("job_");
        body.Status.Should().Be("queued");
    }


    [Fact]
    public async Task Reprocess_InvalidLimit_ReturnsValidationEnvelope()
    {
        var resp = await _http.PostAsJsonAsync("/admin/event-queue/reprocess", new AdminEventQueueReprocessRequest("failed_only", 0));
        resp.StatusCode.Should().Be((HttpStatusCode)422);

        await resp.HasErrorCodeAsync("VALIDATION_ERROR");
    }

    [Fact]
    public async Task Upload_EmptyEvents_ReturnsValidationEnvelope()
    {
        var req = new AdminEventQueueUploadRequest(
            Source: "mobile_admin",
            ExportedAt: DateTimeOffset.UtcNow,
            PlayerId: Guid.NewGuid().ToString(),
            Events: new List<AdminEventQueueItemRequest>());

        var resp = await _http.PostAsJsonAsync("/admin/event-queue/upload", req);
        resp.StatusCode.Should().Be((HttpStatusCode)422);

        await resp.HasErrorCodeAsync("VALIDATION_ERROR");
    }

    [Fact]
    public async Task AdminRoutes_Require_OpsKey()
    {
        using var noKey = new TycoonApiFactory().CreateClient();
        var r = await noKey.PostAsJsonAsync("/admin/event-queue/reprocess", new AdminEventQueueReprocessRequest("failed_only", 10));
        r.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await r.HasErrorCodeAsync("UNAUTHORIZED");
    }
}
