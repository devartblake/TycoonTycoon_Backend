using System.Text.Json;

namespace Tycoon.OperatorDashboard.Components.Common;

public static class JsonSafe
{
    public static IEnumerable<JsonElement> EnumerateArrayProperty(JsonDocument? doc, string propertyName)
    {
        if (doc is null)
            yield break;

        if (!doc.RootElement.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.Array)
            yield break;

        foreach (var item in property.EnumerateArray())
            yield return item;
    }

    public static string GetText(JsonElement source, string propertyName, string fallback = "—")
    {
        if (!source.TryGetProperty(propertyName, out var value))
            return fallback;

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString() ?? fallback,
            JsonValueKind.Number => value.ToString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => fallback,
            JsonValueKind.Undefined => fallback,
            _ => value.ToString()
        };
    }

    public static bool GetBool(JsonElement source, string propertyName, bool fallback = false)
    {
        if (!source.TryGetProperty(propertyName, out var value))
            return fallback;

        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String when bool.TryParse(value.GetString(), out var parsed) => parsed,
            JsonValueKind.Number when value.TryGetInt32(out var n) => n != 0,
            _ => fallback
        };
    }
}
