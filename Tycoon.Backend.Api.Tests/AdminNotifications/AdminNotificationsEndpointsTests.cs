using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Shared.Contracts.Dtos;
using Xunit;

namespace Tycoon.Backend.Api.Tests.AdminNotifications;

public sealed class AdminNotificationsEndpointsTests : IClassFixture<TycoonApiFactory>
{
    private readonly HttpClient _http;
    private readonly TycoonApiFactory _factory;

    public AdminNotificationsEndpointsTests(TycoonApiFactory factory)
    {
        _factory = factory;
        _http = factory.CreateClient().WithAdminOpsKey();
    }

    [Fact]
    public async Task Channels_Send_Schedule_Template_History_Flow_Works()
    {
        await EnsureAdminAuthAsync();
        var channelsResp = await _http.GetAsync("/admin/notifications/channels");
        channelsResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var channels = await channelsResp.Content.ReadFromJsonAsync<List<AdminNotificationChannelDto>>();
        channels.Should().NotBeNull();
        channels!.Should().Contain(c => c.Key == "admin_basic");

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
        await EnsureAdminAuthAsync();
        var schResp = await _http.PostAsJsonAsync("/admin/notifications/schedule",
            new AdminNotificationScheduleRequest("Weekend promo", "Body", "missing_channel", DateTimeOffset.UtcNow.AddDays(1), new Dictionary<string, object>{{"type", "none"}}, new Dictionary<string, object>{{"segment", "active_7d"}}));

        schResp.StatusCode.Should().Be(HttpStatusCode.NotFound);

        await schResp.HasErrorCodeAsync("NOT_FOUND");
    }
    [Fact]
    public async Task DeadLetter_List_And_Replay_Work_For_Failed_Schedule()
    {
        await EnsureAdminAuthAsync();
        string scheduleId;
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IAppDb>();
            var schedule = new Tycoon.Backend.Domain.Entities.AdminNotificationSchedule(
                $"sch_{Guid.NewGuid():N}", "Failed", "Body", "missing", DateTimeOffset.UtcNow.AddMinutes(-1));
            schedule.MarkRetryOrFail("missing", DateTimeOffset.UtcNow.AddMinutes(-1));
            schedule.MarkRetryOrFail("missing", DateTimeOffset.UtcNow.AddMinutes(-1));
            schedule.MarkRetryOrFail("missing", DateTimeOffset.UtcNow.AddMinutes(-1));
            db.AdminNotificationSchedules.Add(schedule);
            await db.SaveChangesAsync();
            scheduleId = schedule.ScheduleId;
        }

        var deadResp = await _http.GetAsync("/admin/notifications/dead-letter?page=1&pageSize=25");
        deadResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var dead = await deadResp.Content.ReadFromJsonAsync<AdminNotificationScheduledListResponse>();
        dead.Should().NotBeNull();
        dead!.Items.Should().Contain(x => x.ScheduleId == scheduleId && x.Status == "failed");

        var replayResp = await _http.PostAsync($"/admin/notifications/dead-letter/{scheduleId}/replay", content: null);
        replayResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var scheduledResp = await _http.GetAsync("/admin/notifications/scheduled?page=1&pageSize=50");
        scheduledResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var scheduled = await scheduledResp.Content.ReadFromJsonAsync<AdminNotificationScheduledListResponse>();
        scheduled.Should().NotBeNull();
        scheduled!.Items.Should().Contain(x => x.ScheduleId == scheduleId && x.Status == "scheduled");
    }


    [Fact]
    public async Task DeadLetter_Replay_NonFailed_Schedule_Returns_Conflict()
    {
        await EnsureAdminAuthAsync();
        var channelsResp = await _http.GetAsync("/admin/notifications/channels");
        channelsResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var schResp = await _http.PostAsJsonAsync("/admin/notifications/schedule",
            new AdminNotificationScheduleRequest("Soon", "Body", "admin_basic", DateTimeOffset.UtcNow.AddMinutes(10),
                new Dictionary<string, object>{{"type", "none"}}, new Dictionary<string, object>{{"segment", "all"}}));
        schResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var sch = await schResp.Content.ReadFromJsonAsync<AdminNotificationScheduleResponse>();
        sch.Should().NotBeNull();

        var replayResp = await _http.PostAsync($"/admin/notifications/dead-letter/{sch!.ScheduleId}/replay", content: null);
        replayResp.StatusCode.Should().Be(HttpStatusCode.Conflict);

        await replayResp.HasErrorCodeAsync("CONFLICT");
    }

    [Fact]
    public async Task DeadLetter_Replay_Missing_Schedule_Returns_NotFound_Envelope()
    {
        await EnsureAdminAuthAsync();

        var replayResp = await _http.PostAsync($"/admin/notifications/dead-letter/sch_missing/replay", content: null);
        replayResp.StatusCode.Should().Be(HttpStatusCode.NotFound);

        await replayResp.HasErrorCodeAsync("NOT_FOUND");
    }

    private async Task EnsureAdminAuthAsync()
    {
        if (_http.DefaultRequestHeaders.Authorization is not null)
        {
            return;
        }

        var email = $"admin_{Guid.NewGuid():N}@example.com";
        var password = "Passw0rd!123";
        var deviceId = $"dev-{Guid.NewGuid():N}";

        var signupResp = await _http.PostAsJsonAsync("/auth/signup",
            new SignupRequest(email, password, deviceId, Username: $"adm_{Guid.NewGuid():N}"));
        signupResp.EnsureSuccessStatusCode();

        var loginResp = await _http.PostAsJsonAsync("/admin/auth/login", new AdminLoginRequest(email, password));
        loginResp.EnsureSuccessStatusCode();

        var login = await loginResp.Content.ReadFromJsonAsync<AdminLoginResponse>();
        login.Should().NotBeNull();

        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login!.AccessToken);
    }

}
