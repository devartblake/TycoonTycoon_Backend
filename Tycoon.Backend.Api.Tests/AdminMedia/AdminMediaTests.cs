using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Shared.Contracts.Dtos;
using Xunit;

namespace Tycoon.Backend.Api.Tests.AdminMedia
{
    public sealed class AdminMediaTests : IClassFixture<TycoonApiFactory>
    {
        private readonly TycoonApiFactory _factory;
    private readonly HttpClient _http;

        public AdminMediaTests(TycoonApiFactory factory)
        {
            _factory = factory;
            _http = factory.CreateClient().WithAdminOpsKey();
        }



        [Fact]
        public async Task Media_Intent_Rejects_Wrong_OpsKey()
        {
            using var wrongKey = _factory.CreateClient().WithAdminOpsKey("wrong-key");

            var resp = await wrongKey.PostAsJsonAsync("/admin/media/intent", new CreateUploadIntentRequest("image.png", "image/png", 12345));
            resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            await resp.HasErrorCodeAsync("FORBIDDEN");
        }

        [Fact]
        public async Task Media_Intent_Requires_OpsKey()
        {
            using var noKey = _factory.CreateClient();

            var resp = await noKey.PostAsJsonAsync("/admin/media/intent", new CreateUploadIntentRequest("image.png", "image/png", 12345));
            resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            await resp.HasErrorCodeAsync("UNAUTHORIZED");
        }

        [Fact]
        public async Task CreateUploadIntent_Returns_AssetKey_And_UploadUrl()
        {
            var req = new CreateUploadIntentRequest("image.png", "image/png", 12345);

            var resp = await _http.PostAsJsonAsync("/admin/media/intent", req);
            resp.IsSuccessStatusCode.Should().BeTrue();

            var dto = await resp.Content.ReadFromJsonAsync<UploadIntentDto>();
            dto.Should().NotBeNull();
            dto!.AssetKey.Should().Contain("uploads/");
            dto.UploadUrl.Should().Contain("/admin/media/upload/");
        }

        [Fact]
        public async Task Upload_Stores_File_And_Returns_AssetKey_And_Url()
        {
            // First get a valid assetKey from the intent endpoint.
            var intentResp = await _http.PostAsJsonAsync("/admin/media/intent", new CreateUploadIntentRequest("test.png", "image/png", 4));
            intentResp.IsSuccessStatusCode.Should().BeTrue();
            var intent = await intentResp.Content.ReadFromJsonAsync<UploadIntentDto>();

            // Build a minimal multipart upload using the assetKey from the intent.
            using var form = new MultipartFormDataContent();
            using var fileBytes = new ByteArrayContent([0x89, 0x50, 0x4E, 0x47]); // PNG magic bytes
            fileBytes.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
            form.Add(fileBytes, "file", "test.png");

            var uploadUrl = intent!.UploadUrl;
            var resp = await _http.PostAsync(uploadUrl, form);

            resp.IsSuccessStatusCode.Should().BeTrue();

            var result = await resp.Content.ReadFromJsonAsync<JsonElement>();
            result.GetProperty("assetKey").GetString().Should().Be(intent.AssetKey);
            result.GetProperty("url").GetString().Should().NotBeNullOrWhiteSpace();
        }
    }
}
