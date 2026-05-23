using System.Net.Http;
using System.Text.Json;
using FluentAssertions;

namespace Synaptix.Backend.Api.Tests.TestHost;

internal static class HttpErrorEnvelopeAssert
{
    public static async Task HasErrorCodeAsync(this HttpResponseMessage response, string expectedCode)
    {
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("error").GetProperty("code").GetString().Should().Be(expectedCode);
    }

    public static async Task HasErrorDetailAsync(this HttpResponseMessage response, string detailKey, string expectedValue)
    {
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("error").GetProperty("details").GetProperty(detailKey).GetString().Should().Be(expectedValue);
    }

    public static async Task HasErrorMessageContainingAsync(this HttpResponseMessage response, string expectedSubstring)
    {
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var msg = json.RootElement.GetProperty("error").GetProperty("message").GetString();
        msg.Should().NotBeNull();
        msg!.Should().Contain(expectedSubstring);
    }

    public static async Task HasErrorDetailArrayContainingAsync(this HttpResponseMessage response, string detailKey, string expectedValue)
    {
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var arr = json.RootElement.GetProperty("error").GetProperty("details").GetProperty(detailKey);
        arr.ValueKind.Should().Be(JsonValueKind.Array);
        arr.EnumerateArray().Select(x => x.GetString()).Should().Contain(expectedValue);
    }
}
