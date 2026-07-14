using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Synaptix.Backend.Api.Tests.TestHost;
using Synaptix.Backend.Application.Auth;
using Synaptix.Backend.Domain.Entities;
using Xunit;

namespace Synaptix.Backend.Api.Tests.AdminStorage;

public sealed class AdminStorageEndpointsTests : IClassFixture<SynaptixApiFactory>
{
    private readonly HttpClient _http;

    public AdminStorageEndpointsTests(SynaptixApiFactory factory)
    {
        _http = factory.CreateClient().WithAdminOpsKey();
    }

    [Fact]
    public async Task Prefixes_Returns_Approved_Destinations()
    {
        var resp = await _http.GetAsync("/admin/storage/prefixes");

        resp.IsSuccessStatusCode.Should().BeTrue();
        var result = await resp.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("prefixes").EnumerateArray()
            .Select(x => x.GetProperty("prefix").GetString())
            .Should().Contain(["seeds/", "models/", "frontend/assets/"]);
    }

    [Theory]
    [InlineData("../secret.json")]
    [InlineData("/models/asset.glb")]
    [InlineData("private/asset.png")]
    public async Task UploadProxy_Rejects_Unsafe_Keys(string key)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(key), "key");
        form.Add(new StringContent("false"), "overwrite");
        using var content = new ByteArrayContent([1, 2, 3]);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        form.Add(content, "file", "asset.json");

        var resp = await _http.PostAsync("/admin/storage/upload-proxy", form);

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadProxy_Requires_Overwrite_For_Existing_Key()
    {
        var key = $"images/tests/{Guid.NewGuid():N}.png";
        var first = await UploadAsync(key, overwrite: false);
        first.IsSuccessStatusCode.Should().BeTrue();

        var second = await UploadAsync(key, overwrite: false);
        second.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var third = await UploadAsync(key, overwrite: true);
        third.IsSuccessStatusCode.Should().BeTrue();
    }

    [Fact]
    public void PermissionProfiles_Give_Storage_Write_To_Admins_Only()
    {
        AdminPermissionProfiles.ForRole(AdminRole.Admin).Permissions.Should().Contain(["storage:read", "storage:write"]);
        AdminPermissionProfiles.ForRole(AdminRole.SuperAdmin).Permissions.Should().Contain(["storage:read", "storage:write"]);
        AdminPermissionProfiles.ForRole(AdminRole.Moderator).Permissions.Should().NotContain("storage:write");
        AdminPermissionProfiles.ForRole(AdminRole.Viewer).Permissions.Should().NotContain("storage:write");
    }

    [Theory]
    [InlineData("../secret.json")]
    [InlineData("private/file.json")]
    public async Task Objects_Metadata_Rejects_Invalid_Key(string key)
    {
        var resp = await _http.GetAsync($"/admin/storage/objects/metadata?key={Uri.EscapeDataString(key)}");
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Objects_Metadata_Returns_NotFound_For_Absent_Object()
    {
        var resp = await _http.GetAsync($"/admin/storage/objects/metadata?key=images/tests/{Guid.NewGuid():N}.png");
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("code").GetString().Should().Be("NOT_FOUND");
    }

    [Fact]
    public async Task Objects_List_Returns_Correct_Shape_For_Valid_Prefix()
    {
        var resp = await _http.GetAsync("/admin/storage/objects?prefix=seeds/&pageSize=2");
        resp.IsSuccessStatusCode.Should().BeTrue();
        var result = await resp.Content.ReadFromJsonAsync<JsonElement>();
        result.GetProperty("prefix").GetString().Should().Be("seeds/");
        result.GetProperty("items").GetArrayLength().Should().BeGreaterThanOrEqualTo(0);
        result.TryGetProperty("nextCursor", out _).Should().BeTrue();
    }

    [Fact]
    public async Task Objects_List_Returns_400_For_Disallowed_Prefix()
    {
        var resp = await _http.GetAsync("/admin/storage/objects?prefix=private/forbidden/");
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UploadIntent_Rejects_Disallowed_Prefix()
    {
        var resp = await _http.PostAsJsonAsync("/admin/storage/upload-intent", new
        {
            key = "private/not-allowed.json",
            contentType = "application/json",
            sizeBytes = 1024,
            overwrite = false,
        });
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("code").GetString().Should().Be("PREFIX_NOT_ALLOWED");
    }

    [Fact]
    public async Task UploadIntent_With_Valid_Key_Returns_Url_Or_PresignUnavailable()
    {
        var resp = await _http.PostAsJsonAsync("/admin/storage/upload-intent", new
        {
            key = $"images/tests/{Guid.NewGuid():N}.png",
            contentType = "image/png",
            sizeBytes = 1024,
            overwrite = false,
        });
        resp.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
        if (resp.StatusCode == HttpStatusCode.BadRequest)
        {
            var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
            body.GetProperty("code").GetString().Should().Be("PRESIGN_UNAVAILABLE");
        }
    }

    private async Task<HttpResponseMessage> UploadAsync(string key, bool overwrite)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(key), "key");
        form.Add(new StringContent(overwrite ? "true" : "false"), "overwrite");
        var content = new ByteArrayContent([0x89, 0x50, 0x4E, 0x47]);
        content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(content, "file", "asset.png");
        return await _http.PostAsync("/admin/storage/upload-proxy", form);
    }
}
