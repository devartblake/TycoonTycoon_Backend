using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tycoon.Backend.Api.Tests.TestHost;

internal static class TestJson
{
    internal static readonly JsonSerializerOptions Default = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };
}
