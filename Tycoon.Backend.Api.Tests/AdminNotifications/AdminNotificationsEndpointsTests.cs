using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Shared.Contracts.Dtos;
using Xunit;

namespace Tycoon.Backend.Api.Tests.AdminNotifications;

public sealed class AdminNotificationsEndpointsTests : IClassFixture<TycoonApiFactory>
{
    private readonly HttpClient _http;

    public AdminNotificationsEndpointsTests(TycoonApiFactory factory)
    {
        _http = factory.CreateClient().WithAdminOpsKey();
    }

    [Fact]
    public async Task Channels_Send_Schedule_Template_History_Flow_Works()
    {
        var channelsResp = await _http.GetAsync("/admin/notifications/channels");
        channelsResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var putResp = await _http.PutAsJsonAsync("/admin/notifications/channels/admin_promos",
            new UpsertAdminNotificationChannelRequest("Promos", "Promo channel", "high", true));
        putResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var sendResp = await _http.PostAsJsonAsync("/admin/notifications/send",
            new AdminNotificationSendRequest("Maintenance", "Window", "admin_promos", new Dictionary<string, object>{{"segment", "all_users"}}, new Dictionary<string, object>{{"type","maintenance"}}));
        sendResp.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var schResp = await _http.PostAsJsonAsync("/admin/notifications/schedule",
            new AdminNotificationScheduleRequest("Weekend promo", "Body", "admin_promos", DateTimeOffset.UtcNow.AddDays(1), new Dictionary<string, object>{{"type", "none"}}, new Dictionary<string, object>{{"segment", "active_7d"}}));
        schResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var sch = await schResp.Content.ReadFromJsonAsync<AdminNotificationScheduleResponse>();
        sch.Should().NotBeNull();

        var listResp = await _http.GetAsync("/admin/notifications/scheduled?page=1&pageSize=25");
        listResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var tplCreate = await _http.PostAsJsonAsync("/admin/notifications/templates",
            new AdminNotificationTemplateRequest("promo_default", "{{campaignName}}", "{{body}}", "admin_promos", new[] { "campaignName", "body" }));
        tplCreate.StatusCode.Should().Be(HttpStatusCode.Created);

        var createdTpl = await tplCreate.Content.ReadFromJsonAsync<AdminNotificationTemplateDto>();
        createdTpl.Should().NotBeNull();

        var tplPatch = await _http.PatchAsJsonAsync($"/admin/notifications/templates/{createdTpl!.TemplateId}",
            new AdminNotificationTemplateRequest("promo_default", "Updated", "Updated body", "admin_promos", new[] { "campaignName", "body" }));
        tplPatch.StatusCode.Should().Be(HttpStatusCode.OK);

        var from = DateTimeOffset.UtcNow.AddMinutes(-5).ToString("O");
        var to = DateTimeOffset.UtcNow.AddMinutes(5).ToString("O");
        var historyResp = await _http.GetAsync($"/admin/notifications/history?from={Uri.EscapeDataString(from)}&to={Uri.EscapeDataString(to)}&status=queued&page=1&pageSize=25");
        historyResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var cancelResp = await _http.DeleteAsync($"/admin/notifications/scheduled/{sch!.ScheduleId}");
        cancelResp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Schedule_Unknown_Channel_Returns_NotFound()
    {
        var schResp = await _http.PostAsJsonAsync("/admin/notifications/schedule",
            new AdminNotificationScheduleRequest("Weekend promo", "Body", "missing_channel", DateTimeOffset.UtcNow.AddDays(1), new Dictionary<string, object>{{"type", "none"}}, new Dictionary<string, object>{{"segment", "active_7d"}}));

        schResp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
