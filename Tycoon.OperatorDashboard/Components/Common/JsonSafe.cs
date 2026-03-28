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

        return GetText(value, fallback);
    }

    public static string GetText(JsonElement value, string fallback = "—")
    {
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

    public static int GetInt(JsonElement source, string propertyName, int fallback = 0)
    {
        if (!source.TryGetProperty(propertyName, out var value))
            return fallback;

        return value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetInt32(out var n) => n,
            JsonValueKind.String when int.TryParse(value.GetString(), out var n) => n,
            _ => fallback
        };
    }

    public static Guid GetGuid(JsonElement source, string propertyName, Guid fallback = default)
    {
        if (!source.TryGetProperty(propertyName, out var value))
            return fallback;

        if (value.ValueKind == JsonValueKind.String && Guid.TryParse(value.GetString(), out var parsed))
            return parsed;

        return fallback;
    }
}
