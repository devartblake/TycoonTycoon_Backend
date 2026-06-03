using Microsoft.Extensions.Configuration;

namespace Synaptix.Setup;

// Minimal .env file reader — loads KEY=VALUE pairs into IConfiguration.
public static class DotEnvConfigurationExtensions
{
    public static IConfigurationBuilder AddDotNetEnv(this IConfigurationBuilder builder, string filePath)
    {
        if (!File.Exists(filePath)) return builder;

        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawLine in File.ReadAllLines(filePath))
        {
            var line = rawLine.Trim();
            if (line.StartsWith('#') || string.IsNullOrWhiteSpace(line)) continue;

            var eqIdx = line.IndexOf('=');
            if (eqIdx < 0) continue;

            var key   = line[..eqIdx].Trim();
            var value = line[(eqIdx + 1)..].Trim().Trim('"').Trim('\'');

            if (!string.IsNullOrWhiteSpace(key))
            {
                // Normalize double-underscore to colon so .env file keys behave
                // identically to AddEnvironmentVariables() — both map MinIO__Endpoint
                // to cfg["MinIO:Endpoint"].
                var normalizedKey = key.Replace("__", ":");
                data[normalizedKey] = value;
            }
        }

        return builder.AddInMemoryCollection(data);
    }
}
