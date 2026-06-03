using System.Text.Json;
using Serilog;

namespace Synaptix.Setup.Bootstrap;

public sealed class BootstrapStateWriter
{
    private static readonly JsonSerializerOptions JsonOpts = new() { WriteIndented = true };

    public async Task WriteAsync(string outputPath, BootstrapStatus status)
    {
        var dir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(status, JsonOpts);
        await File.WriteAllTextAsync(outputPath, json);
        Log.Information("Bootstrap status written to {Path}.", outputPath);
    }

    public async Task<BootstrapStatus?> ReadAsync(string path)
    {
        if (!File.Exists(path)) return null;
        try
        {
            await using var fs = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<BootstrapStatus>(fs, JsonOpts);
        }
        catch
        {
            return null;
        }
    }
}

public sealed class BootstrapStatus
{
    public DateTimeOffset GeneratedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public string Environment { get; set; } = "local";
    public bool SecretsGenerated { get; set; }
    public bool ServicesProvisioned { get; set; }
    public bool SeedsUploaded { get; set; }
    public bool SuperAdminCreated { get; set; }
    public string? EnvFilePath { get; set; }
    public List<string> CompletedTasks { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
    public List<string> Errors { get; set; } = [];
}
