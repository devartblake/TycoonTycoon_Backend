using System.Net.Http;
using System.Text.Json;
using FluentAssertions;

namespace Tycoon.Backend.Api.Tests.TestHost;

internal static class HttpErrorEnvelopeAssert
{
    public static async Task HasErrorCodeAsync(this HttpResponseMessage response, string expectedCode)
    {
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("error").GetProperty("code").GetString().Should().Be(expectedCode);
    }
}
