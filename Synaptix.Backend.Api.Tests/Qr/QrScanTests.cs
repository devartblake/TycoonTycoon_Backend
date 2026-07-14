using System.Net.Http.Json;
using FluentAssertions;
using Synaptix.Backend.Api.Tests.TestHost;
using Synaptix.Shared.Contracts.Dtos;
using Xunit;

namespace Synaptix.Backend.Api.Tests.Qr
{
    public sealed class QrScanTests : IClassFixture<SynaptixApiFactory>
    {
        private readonly HttpClient _http;

        public QrScanTests(SynaptixApiFactory factory)
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

            var a = await _http.PostAsJsonAsync("/api/v1/qr/track-scan", req);
            a.IsSuccessStatusCode.Should().BeTrue();

            var b = await _http.PostAsJsonAsync("/api/v1/qr/track-scan", req);
            b.IsSuccessStatusCode.Should().BeTrue();

            var r2 = await b.Content.ReadFromJsonAsync<TrackScanResultDto>();
            r2!.Status.Should().Be("Duplicate");
        }
    }
}
