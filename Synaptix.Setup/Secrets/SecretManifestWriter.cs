using Serilog;

namespace Synaptix.Setup.Secrets;

public sealed class SecretManifestWriter
{
    private const string PlaceholderMarker = "<generated-by-synaptix-setup>";

    // Keys that get generated secrets mapped to generator functions.
    private static readonly Dictionary<string, Func<string>> SecretGenerators = new()
    {
        ["POSTGRES_PASSWORD"]          = () => SecretGenerator.GeneratePassword(32),
        ["MONGO_INITDB_ROOT_PASSWORD"] = () => SecretGenerator.GeneratePassword(32),
        ["MONGO_APP_PASSWORD"]         = () => SecretGenerator.GeneratePassword(32),
        ["REDIS_PASSWORD"]             = () => SecretGenerator.GeneratePassword(32),
        ["ELASTIC_PASSWORD"]           = () => SecretGenerator.GeneratePassword(24, includeSpecial: false),
        ["RABBITMQ_PASSWORD"]          = () => SecretGenerator.GeneratePassword(32),
        ["MINIO_ROOT_PASSWORD"]        = () => SecretGenerator.GeneratePassword(32),
        ["JWT_SECRET_KEY"]             = () => SecretGenerator.GenerateJwtKey(64),
        ["ADMIN_OPS_KEY"]              = () => SecretGenerator.GenerateApiKey(48),
        ["KMS_SERVICE_TOKEN"]          = () => SecretGenerator.GenerateApiKey(40),
        ["SUPER_ADMIN_PASSWORD"]       = () => SecretGenerator.GeneratePassword(20),
        ["GRAFANA_PASSWORD"]           = () => SecretGenerator.GeneratePassword(20),
        ["PGADMIN_DEFAULT_PASSWORD"]   = () => SecretGenerator.GeneratePassword(20),
        ["MONGO_EXPRESS_PASSWORD"]     = () => SecretGenerator.GeneratePassword(20),
    };

    public async Task<GeneratedSecrets> WriteEnvAsync(string templatePath, string outputPath)
    {
        if (!File.Exists(templatePath))
            throw new FileNotFoundException($"Template not found: {templatePath}");

        var templateLines = await File.ReadAllLinesAsync(templatePath);
        var generated = new Dictionary<string, string>();
        var outputLines = new List<string>();

        foreach (var line in templateLines)
        {
            if (line.TrimStart().StartsWith('#') || string.IsNullOrWhiteSpace(line))
            {
                outputLines.Add(line);
                continue;
            }

            var eqIdx = line.IndexOf('=');
            if (eqIdx < 0) { outputLines.Add(line); continue; }

            var key   = line[..eqIdx].Trim();
            var value = line[(eqIdx + 1)..].Trim();

            if (value.Contains(PlaceholderMarker, StringComparison.OrdinalIgnoreCase) &&
                SecretGenerators.TryGetValue(key, out var gen))
            {
                var secret = gen();
                generated[key] = secret;
                outputLines.Add($"{key}={secret}");
                Log.Information("Generated secret for {Key}.", key);
            }
            else
            {
                outputLines.Add(line);
            }
        }

        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        await File.WriteAllLinesAsync(outputPath, outputLines);
        Log.Information("Wrote {OutputPath} with {Count} generated secret(s).", outputPath, generated.Count);

        return new GeneratedSecrets(outputPath, generated);
    }
}

public sealed record GeneratedSecrets(string OutputPath, IReadOnlyDictionary<string, string> Values);
