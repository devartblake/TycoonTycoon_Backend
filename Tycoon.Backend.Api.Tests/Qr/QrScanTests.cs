using System.Net.Http.Json;
using FluentAssertions;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Shared.Contracts.Dtos;
using Xunit;

namespace Tycoon.Backend.Api.Tests.Qr
{
    public sealed class QrScanTests : IClassFixture<TycoonApiFactory>
    {
        private readonly HttpClient _http;

        public QrScanTests(TycoonApiFactory factory)
        {
            _http = factory.CreateClient();
        }

        [Fact]
        public async Task TrackScan_Then_Duplicate_Is_Idempotent()
        {
            var playerId = Guid.NewGuid();
            var eventId = Guid.NewGuid();

            var req = new TrackScanRequest(
                EventId: eventId,
                PlayerId: playerId,
                Value: "REF:ABCDEFGH",
                OccurredAtUtc: DateTimeOffset.UtcNow,
                Type: QrScanType.Referral);

            var a = await _http.PostAsJsonAsync("/qr/track-scan", req);
            a.IsSuccessStatusCode.Should().BeTrue();

            var b = await _http.PostAsJsonAsync("/qr/track-scan", req);
            b.IsSuccessStatusCode.Should().BeTrue();

            var r2 = await b.Content.ReadFromJsonAsync<TrackScanResultDto>();
            r2!.Status.Should().Be("Duplicate");
        }
    }
}
