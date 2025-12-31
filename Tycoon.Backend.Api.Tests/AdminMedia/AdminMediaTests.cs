using System.Net.Http.Json;
using FluentAssertions;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Shared.Contracts.Dtos;
using Xunit;

namespace Tycoon.Backend.Api.Tests.AdminMedia
{
    public sealed class AdminMediaTests : IClassFixture<TycoonApiFactory>
    {
        private readonly HttpClient _http;

        public AdminMediaTests(TycoonApiFactory factory)
        {
            _http = factory.CreateClient().WithAdminOpsKey();
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
    }
}
